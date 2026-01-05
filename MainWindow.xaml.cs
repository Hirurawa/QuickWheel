using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuickWheel.Core;
using QuickWheel.Models;
using System.Windows.Media.Effects;

namespace QuickWheel
{
    public partial class MainWindow : Window
    {
        // Settings & State
        private AppSettings _settings;
        private List<SliceConfig> _currentContext;
        private Stack<List<SliceConfig>> _navigationStack = new Stack<List<SliceConfig>>();
        
        // Logic Vars
        private GlobalKeyboardHook _globalHook;
        private DispatcherTimer _trapTimer;
        private DispatcherTimer _hoverTimer;
        private SliceConfig _lastHoveredSlice = null;

        // Constants
        private const double WHEEL_RADIUS = 150;
        private const double DEADZONE_RADIUS = 40;
        private const Key TRIGGER_KEY = Key.Tab;
        private const Key EXIT_KEY = Key.Escape;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            // Warmup Window
            this.Left = -10000;
            this.Top = -10000;
            this.Show();
            this.Hide();

            // Initialize Hooks & Timers
            SetupHooks();
            SetupTimers();
        }

        private void SetupHooks()
        {
            _globalHook = new GlobalKeyboardHook();
            _globalHook.OnKeyDown += GlobalHook_OnKeyDown;
            _globalHook.OnKeyUp += GlobalHook_OnKeyUp;
            _globalHook.Hook();
        }

        private void SetupTimers()
        {
            _trapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _trapTimer.Tick += TrapMouseInCircle;

            _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _hoverTimer.Tick += HoverTimer_Tick;
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
                else _settings = new AppSettings();
            }
            catch { _settings = new AppSettings(); }
            
            _currentContext = _settings.Slices;
        }

        // --- DRAWING ---
        private void DrawDynamicWheel(List<SliceConfig> items)
        {
            // FIX: Only clear the dynamic layer. Do not touch CenterLabel!
            DynamicLayer.Children.Clear();
            SelectionHighlight.Opacity = 0;
            
            int count = items.Count;
            if (count == 0) return;

            double sliceAngle = 360.0 / count;

            for (int i = 0; i < count; i++)
            {
                // 1. Divider Line
                double angleRad = (i * sliceAngle) * (Math.PI / 180.0);
                Line div = new Line
                {
                    X1 = WHEEL_RADIUS, Y1 = WHEEL_RADIUS,
                    X2 = WHEEL_RADIUS + WHEEL_RADIUS * Math.Cos(angleRad),
                    Y2 = WHEEL_RADIUS + WHEEL_RADIUS * Math.Sin(angleRad),
                    Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    StrokeThickness = 1
                };
                DynamicLayer.Children.Add(div);

                // 2. Label
                string text = items[i].Label + ((items[i].Items != null && items[i].Items.Count > 0) ? " >" : "");
                double midAngle = (i * sliceAngle) + (sliceAngle / 2);
                double midRad = midAngle * (Math.PI / 180.0);

                TextBlock txt = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    RenderTransform = new TranslateTransform(-20, -10),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(
                        WHEEL_RADIUS + 100 * Math.Cos(midRad), 
                        WHEEL_RADIUS + 100 * Math.Sin(midRad), 0, 0)
                };
                DynamicLayer.Children.Add(txt);
            }
        }

        // --- INPUT HANDLERS ---
        private void GlobalHook_OnKeyDown(object sender, GlobalKeyEventArgs e)
        {
            if (e.Key == EXIT_KEY) CleanupAndExit();
            if (e.Key == TRIGGER_KEY)
            {
                e.Handled = true;
                if (this.Visibility != Visibility.Visible)
                {
                    // Reset State
                    _currentContext = _settings.Slices;
                    _navigationStack.Clear();
                    DrawDynamicWheel(_currentContext);

                    // Move Window
                    NativeMethods.Win32Point mousePos;
                    NativeMethods.GetCursorPos(out mousePos);
                    var dpi = GetDpiScale();
                    this.Left = (mousePos.X / dpi.X) - WHEEL_RADIUS;
                    this.Top = (mousePos.Y / dpi.Y) - WHEEL_RADIUS;

                    this.Show(); this.Activate();
                    _trapTimer.Start();
                }
                CheckSelection();
            }
        }

        private void GlobalHook_OnKeyUp(object sender, GlobalKeyEventArgs e)
        {
            if (e.Key == TRIGGER_KEY)
            {
                e.Handled = true;
                _trapTimer.Stop();
                _hoverTimer.Stop();

                var slice = GetSelectedSlice();

                if (slice == null) // Dead Zone
                {
                    if (_navigationStack.Count > 0) NavigateBack();
                    else this.Hide();
                }
                else if (slice.Items != null && slice.Items.Count > 0) // Enter Folder
                {
                    NavigateInto(slice);
                    _trapTimer.Start();
                }
                else // Execute
                {
                    RunCommand(slice);
                    this.Hide();
                }
            }
        }

        // --- NAVIGATION & LOGIC ---
        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (_lastHoveredSlice != null && _lastHoveredSlice.Items?.Count > 0)
            {
                _hoverTimer.Stop();
                NavigateInto(_lastHoveredSlice);
            }
        }

        private void NavigateInto(SliceConfig folder)
        {
            _navigationStack.Push(_currentContext);
            _currentContext = folder.Items;
            DrawDynamicWheel(_currentContext);
            CenterMouse();
            Console.WriteLine($"[NAV] Entered {folder.Label}");
        }

        private void NavigateBack()
        {
            _currentContext = _navigationStack.Pop();
            DrawDynamicWheel(_currentContext);
            CenterMouse();
            _trapTimer.Start();
        }

        private void CheckSelection()
        {
            // 1. Calculate Index manually (we need the number, not just the config object)
            int index = -1;
            SliceConfig slice = null;

            if (_currentContext.Count > 0)
            {
                NativeMethods.Win32Point mousePos;
                NativeMethods.GetCursorPos(out mousePos);
                var dpi = GetDpiScale();
                double centerX = (this.Left + 150) * dpi.X;
                double centerY = (this.Top + 150) * dpi.Y;
                double dx = mousePos.X - centerX; 
                double dy = mousePos.Y - centerY;
                
                // Only select if outside deadzone
                if (Math.Sqrt(dx * dx + dy * dy) > (40 * dpi.X))
                {
                    double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
                    if (angle < 0) angle += 360;
                    
                    index = (int)(angle / (360.0 / _currentContext.Count));
                    if (index >= 0 && index < _currentContext.Count)
                    {
                        slice = _currentContext[index];
                    }
                }
            }

            // 2. Update The Glow
            UpdateHighlight(index, _currentContext.Count);

            // 3. Update Text & Hover Logic (Existing logic)
            CenterLabel.Text = slice?.Label ?? (_navigationStack.Count > 0 ? "Back" : "Cancel");

            if (slice != _lastHoveredSlice)
            {
                _lastHoveredSlice = slice;
                _hoverTimer.Stop();
                if (slice != null) _hoverTimer.Start();
            }
        }

        private SliceConfig GetSelectedSlice()
        {
            if (_currentContext.Count == 0) return null;

            NativeMethods.Win32Point mousePos;
            NativeMethods.GetCursorPos(out mousePos);
            var dpi = GetDpiScale();
            double centerX = (this.Left + WHEEL_RADIUS) * dpi.X;
            double centerY = (this.Top + WHEEL_RADIUS) * dpi.Y;
            double dx = mousePos.X - centerX; 
            double dy = mousePos.Y - centerY;
            
            if (Math.Sqrt(dx*dx + dy*dy) < (DEADZONE_RADIUS * dpi.X)) return null;

            double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
            if (angle < 0) angle += 360;

            int index = (int)(angle / (360.0 / _currentContext.Count));
            if (index >= 0 && index < _currentContext.Count) return _currentContext[index];
            return null;
        }

        // --- HELPERS ---
        private void TrapMouseInCircle(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;
            
            NativeMethods.Win32Point mousePos;
            NativeMethods.GetCursorPos(out mousePos);
            var dpi = GetDpiScale();
            double centerX = (this.Left + WHEEL_RADIUS) * dpi.X;
            double centerY = (this.Top + WHEEL_RADIUS) * dpi.Y;
            double dx = mousePos.X - centerX; 
            double dy = mousePos.Y - centerY;

            if (Math.Sqrt(dx * dx + dy * dy) > (WHEEL_RADIUS * dpi.X))
            {
                double angle = Math.Atan2(dy, dx);
                NativeMethods.SetCursorPos(
                    (int)(centerX + (WHEEL_RADIUS * dpi.X) * Math.Cos(angle)),
                    (int)(centerY + (WHEEL_RADIUS * dpi.X) * Math.Sin(angle)));
            }
            CheckSelection();
        }

        private void UpdateHighlight(int index, int totalCount)
        {
            if (index < 0 || totalCount == 0)
            {
                SelectionHighlight.Opacity = 0; // Hide if nothing selected
                return;
            }

            double radius = 148; // Slightly smaller than window to avoid clipping
            double center = 150;
            double sliceAngle = 360.0 / totalCount;
            
            // Calculate Start and End Angles
            double startAngle = index * sliceAngle;
            double endAngle = (index + 1) * sliceAngle;

            // Convert to Radians
            double startRad = startAngle * (Math.PI / 180.0);
            double endRad = endAngle * (Math.PI / 180.0);

            // Calculate Points
            Point centerPt = new Point(center, center);
            Point startPt = new Point(center + radius * Math.Cos(startRad), center + radius * Math.Sin(startRad));
            Point endPt = new Point(center + radius * Math.Cos(endRad), center + radius * Math.Sin(endRad));

            // Create the "Pie Slice" Geometry
            PathFigure figure = new PathFigure();
            figure.StartPoint = centerPt;
            
            // 1. Line to Start
            figure.Segments.Add(new LineSegment(startPt, true));
            
            // 2. Arc to End
            figure.Segments.Add(new ArcSegment(
                endPt, 
                new Size(radius, radius), 
                0, 
                false, // IsLargeArc (always false for slices < 180 deg)
                SweepDirection.Clockwise, 
                true));
            
            // 3. Line back to Center is implied by PathFigure.IsClosed = true

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            
            // Apply to the Highlight Path
            SelectionHighlight.Data = geometry;
            SelectionHighlight.Opacity = 1; // Show it
        }
        private void CenterMouse()
        {
            var dpi = GetDpiScale();
            NativeMethods.SetCursorPos(
                (int)((this.Left + WHEEL_RADIUS) * dpi.X), 
                (int)((this.Top + WHEEL_RADIUS) * dpi.Y));
        }

        private void RunCommand(SliceConfig config)
        {
            try
            {
                if (config.Type == SliceType.App)
                {
                    Process.Start(new ProcessStartInfo 
                    { 
                        FileName = config.Path, 
                        Arguments = config.Args, 
                        UseShellExecute = true 
                    });
                }
                else if (config.Type == SliceType.Paste)
                {
                    // 1. Set text to Clipboard
                    Clipboard.SetText(config.Data);

                    // 2. Wait for window to hide and focus to return
                    // We do this in a separate task so we don't freeze the UI thread while waiting
                    System.Threading.Tasks.Task.Run(() => 
                    {
                        Thread.Sleep(100); // Give Windows 100ms to switch focus back to your text editor
                        
                        // 3. Simulate Ctrl+V
                        InputSender.SendCtrlV();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing {config.Label}: {ex.Message}");
            }
        }

        private void CleanupAndExit()
        {
            _trapTimer.Stop(); _hoverTimer.Stop(); _globalHook.Unhook();
            Application.Current.Shutdown();
        }

        private Point GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            return (source?.CompositionTarget != null) 
                ? new Point(source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22) 
                : new Point(1.0, 1.0);
        }
    }
}
