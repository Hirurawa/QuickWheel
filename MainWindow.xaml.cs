using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace QuickWheel
{
    public partial class MainWindow : Window
    {
        private GlobalKeyboardHook _globalHook;
        private const Key TRIGGER_KEY = Key.Tab;
        private const Key EXIT_KEY = Key.Escape;

        // Circular Trap Timer
        private DispatcherTimer _trapTimer;
        
        // Configuration
        private double _radius = 150; // Half of width (300/2)

        private AppSettings _settings;

        public MainWindow()
        {
            InitializeComponent();
            
            LoadSettings();

            // Warm up
            this.Left = -10000;
            this.Top = -10000;
            this.Show();
            this.Hide();

            _globalHook = new GlobalKeyboardHook();
            _globalHook.OnKeyDown += GlobalHook_OnKeyDown;
            _globalHook.OnKeyUp += GlobalHook_OnKeyUp;
            _globalHook.Hook();

            // Setup the loop to keep mouse inside circle
            _trapTimer = new DispatcherTimer();
            _trapTimer.Interval = TimeSpan.FromMilliseconds(10); // Run 100 times a second
            _trapTimer.Tick += TrapMouseInCircle;
        }

        private void LoadSettings()
        {
            try 
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    // Allow case-insensitive property names (e.g. "slices" vs "Slices")
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                    Console.WriteLine($"Loaded {_settings.Slices.Count} commands.");
                }
                else
                {
                    Console.WriteLine("settings.json not found!");
                    _settings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                _settings = new AppSettings();
            }
        }

        private void GlobalHook_OnKeyDown(object sender, GlobalKeyEventArgs e)
        {
            // Safety Exit
            if (e.Key == EXIT_KEY) CleanupAndExit();

            if (e.Key == TRIGGER_KEY)
            {
                // IMPORTANT: Eat the key so Windows doesn't see it!
                e.Handled = true;

                if (this.Visibility != Visibility.Visible)
                {
                    // ... (Your existing code to show window and start timer) ...
                    Win32Point mousePos;
                    GetCursorPos(out mousePos);
                    var dpi = GetDpiScale();

                    double wpfX = mousePos.X / dpi.X;
                    double wpfY = mousePos.Y / dpi.Y;
                    this.Left = wpfX - _radius;
                    this.Top = wpfY - _radius;

                    this.Show();
                    this.Activate();
                    _trapTimer.Start();
                }
            }
        }

        private void GlobalHook_OnKeyUp(object sender, GlobalKeyEventArgs e)
        {
            if (e.Key == TRIGGER_KEY)
            {
                e.Handled = true;
                _trapTimer.Stop();
                
                // 1. Get the direction (e.g., "Top-Right")
                string selectedSlice = GetSelectedSlice();
                
                // 2. Find the matching command in settings
                var action = _settings.Slices.FirstOrDefault(s => s.Id == selectedSlice);

                if (action != null)
                {
                    Console.WriteLine($"[EXECUTE] {action.Label} -> {action.Path} {action.Args}");
                    RunCommand(action);
                }
                else
                {
                    Console.WriteLine($"[NO ACTION] Slice: {selectedSlice}");
                }

                this.Hide();
            }
        }

        // --- NEW: Run Command Helper ---
        private void RunCommand(SliceConfig config)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = config.Path,
                    Arguments = config.Args,
                    UseShellExecute = true // Important for opening URLs or generic files
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run {config.Path}: {ex.Message}");
            }
        }

        // --- CIRCULAR TRAP LOGIC ---
        private void TrapMouseInCircle(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;

            Win32Point mousePos;
            GetCursorPos(out mousePos);
            var dpi = GetDpiScale();

            // Calculate Center in Screen Pixels
            double centerX = (this.Left + _radius) * dpi.X;
            double centerY = (this.Top + _radius) * dpi.Y;

            // Vector from center to mouse
            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Allowed radius in pixels
            double allowedRadius = _radius * dpi.X; 

            // If we are outside the circle, pull it back!
            if (distance > allowedRadius)
            {
                double angle = Math.Atan2(dy, dx);
                
                // Calculate point on the edge
                int newX = (int)(centerX + allowedRadius * Math.Cos(angle));
                int newY = (int)(centerY + allowedRadius * Math.Sin(angle));

                SetCursorPos(newX, newY);
            }
        }

        // --- SELECTION LOGIC (Corrected for X shape) ---
        private string GetSelectedSlice()
        {
            Win32Point mousePos;
            GetCursorPos(out mousePos);
            var dpi = GetDpiScale();

            double centerX = (this.Left + _radius) * dpi.X;
            double centerY = (this.Top + _radius) * dpi.Y;
            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Dead Zone Check (40px)
            if (distance < (40 * dpi.X)) return "None";

            // Angle Logic
            // 0 is Right, 90 is Bottom, 180 is Left, 270 is Top
            double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
            if (angle < 0) angle += 360;

            // Visuals are a "+" sign, so we have 4 corners:
            if (angle >= 0 && angle < 90) return "Bottom-Right";
            if (angle >= 90 && angle < 180) return "Bottom-Left";
            if (angle >= 180 && angle < 270) return "Top-Left";
            return "Top-Right";
        }

        private void CleanupAndExit()
        {
            _trapTimer.Stop();
            _globalHook.Unhook();
            Application.Current.Shutdown();
        }

        private Point GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
                return new Point(source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22);
            return new Point(1.0, 1.0);
        }

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out Win32Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; };
    }

    public class AppSettings
    {
        public List<SliceConfig> Slices { get; set; } = new List<SliceConfig>();
    }

    public class SliceConfig
    {
        public string Id { get; set; } // "Top-Right", etc.
        public string Label { get; set; } // "Notepad"
        public string Path { get; set; }  // "notepad.exe"
        public string Args { get; set; }  // Arguments (optional)
    }
}