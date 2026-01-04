using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls; // For adding TextBlocks/Lines
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes; // For Line/Ellipse
using System.Windows.Threading;

namespace QuickWheel
{
    public partial class MainWindow : Window
    {
        private GlobalKeyboardHook _globalHook;
        private const Key TRIGGER_KEY = Key.Tab;
        private const Key EXIT_KEY = Key.Escape;
        private DispatcherTimer _trapTimer;
        
        private AppSettings _settings;
        private double _radius = 150;
        
        // Dynamic Math Variables
        private double _sliceAngle; // How big is one slice? (360 / count)
        
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            // Setup Window Position (Warmup)
            this.Left = -10000;
            this.Top = -10000;
            this.Show();
            this.Hide();

            // Setup Hook
            _globalHook = new GlobalKeyboardHook();
            _globalHook.OnKeyDown += GlobalHook_OnKeyDown;
            _globalHook.OnKeyUp += GlobalHook_OnKeyUp;
            _globalHook.Hook();

            // Setup Timer
            _trapTimer = new DispatcherTimer();
            _trapTimer.Interval = TimeSpan.FromMilliseconds(10);
            _trapTimer.Tick += TrapMouseInCircle;
        }

        private void LoadSettings()
        {
            try 
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                }
                else
                {
                    _settings = new AppSettings(); 
                }
            }
            catch { _settings = new AppSettings(); }

            // DRAW THE WHEEL DYNAMICALLY
            DrawDynamicWheel();
        }

        private void DrawDynamicWheel()
        {
            int count = _settings.Slices.Count;
            if (count == 0) return;

            // Calculate angle per slice
            _sliceAngle = 360.0 / count;

            // We want to draw Lines and Labels.
            // Note: 0 degrees in Math is 3 o'clock (Right). 
            // We usually want item 1 at the top, but let's keep it simple: Item 1 starts at 0 deg (Right).

            for (int i = 0; i < count; i++)
            {
                // 1. Draw Separator Line (at the start of the slice)
                double angleRad = (i * _sliceAngle) * (Math.PI / 180.0);
                
                // Calculate end point of the line (start is center 150,150)
                double x = 150 + 150 * Math.Cos(angleRad);
                double y = 150 + 150 * Math.Sin(angleRad);

                Line div = new Line
                {
                    X1 = 150, Y1 = 150,
                    X2 = x, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), // Faint white
                    StrokeThickness = 1
                };
                
                // Add line to canvas BEFORE the dead zone (index 0 is the background circle)
                // We add at index 1 so it sits on top of background
                WheelCanvas.Children.Insert(1, div);

                // 2. Draw Label (in the middle of the slice)
                double midAngle = (i * _sliceAngle) + (_sliceAngle / 2);
                double midRad = midAngle * (Math.PI / 180.0);

                // Position text at 100px from center (so it fits inside)
                double textX = 150 + 100 * Math.Cos(midRad);
                double textY = 150 + 100 * Math.Sin(midRad);

                TextBlock txt = new TextBlock
                {
                    Text = _settings.Slices[i].Label,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };

                // Center the text block on that point
                // We need to measure it roughly to center it, but RenderTransform is easier
                txt.RenderTransform = new TranslateTransform(-20, -10); // Rough center alignment
                
                // Set position using Margin (since we are in a Grid, this is a bit hacky but works for MVP)
                // Better way: Use Canvas. But Grid works if we set alignment.
                txt.HorizontalAlignment = HorizontalAlignment.Left;
                txt.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                txt.Margin = new Thickness(textX, textY, 0, 0);

                WheelCanvas.Children.Add(txt);
            }
        }

        // --- SELECTION LOGIC ---
        private SliceConfig GetSelectedSlice()
        {
            Win32Point mousePos;
            GetCursorPos(out mousePos);
            var dpi = GetDpiScale();

            double centerX = (this.Left + _radius) * dpi.X;
            double centerY = (this.Top + _radius) * dpi.Y;
            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Dead Zone
            if (distance < (40 * dpi.X)) 
            {
                CenterLabel.Text = "";
                return null;
            }

            // Calculate Angle
            double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
            if (angle < 0) angle += 360;

            // Math: Index = Floor(Angle / SliceAngle)
            int index = (int)(angle / _sliceAngle);

            // Safety check
            if (index >= 0 && index < _settings.Slices.Count)
            {
                var slice = _settings.Slices[index];
                CenterLabel.Text = slice.Label; // Show text in center
                return slice;
            }
            
            return null;
        }

        private void GlobalHook_OnKeyUp(object sender, GlobalKeyEventArgs e)
        {
            if (e.Key == TRIGGER_KEY)
            {
                e.Handled = true;
                _trapTimer.Stop();

                var slice = GetSelectedSlice();
                if (slice != null)
                {
                    Console.WriteLine($"[EXECUTE] {slice.Label}");
                    RunCommand(slice);
                }
                
                this.Hide();
            }
        }
        
         private void GlobalHook_OnKeyDown(object sender, GlobalKeyEventArgs e)
        {
            if (e.Key == EXIT_KEY) CleanupAndExit();
            if (e.Key == TRIGGER_KEY)
            {
                e.Handled = true;
                if (this.Visibility != Visibility.Visible)
                {
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
                // Update center text while holding
                GetSelectedSlice(); 
            }
        }

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
            // Update Center Text while moving
            GetSelectedSlice();
        }

        private void RunCommand(SliceConfig config)
        {
            try { Process.Start(new ProcessStartInfo { FileName = config.Path, Arguments = config.Args, UseShellExecute = true }); }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
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
        public string Label { get; set; } // "Notepad"
        public string Path { get; set; }  // "notepad.exe"
        public string Args { get; set; }  // Arguments (optional)
    }
}