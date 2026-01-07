using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuickWheel.Core;
using QuickWheel.Infrastructure;
using QuickWheel.Models;
using QuickWheel.ViewModels;
using System.Windows.Media.Imaging;
using System.IO;

namespace QuickWheel
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer _trapTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Warmup Window
            this.Left = -10000;
            this.Top = -10000;
            this.Show();
            this.Hide();

            SetupTimers();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            EnableBlur();
            this.SizeChanged += (s, ev) => UpdateWindowRegion();
            UpdateWindowRegion();
        }

        private void UpdateWindowRegion()
        {
            if (this.ActualWidth <= 0 || this.ActualHeight <= 0) return;

            var dpi = GetDpiScale();
            int width = (int)(this.ActualWidth * dpi.X);
            int height = (int)(this.ActualHeight * dpi.Y);

            // Create Elliptic Region
            // CreateEllipticRgn takes (nLeftRect, nTopRect, nRightRect, nBottomRect)
            // Coordinates are bounding box.
            IntPtr hRgn = NativeMethods.CreateEllipticRgn(0, 0, width, height);

            // SetWindowRgn transfers ownership
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            NativeMethods.SetWindowRgn(helper.Handle, hRgn, true);
        }

        private void EnableBlur()
        {
            var windowHelper = new System.Windows.Interop.WindowInteropHelper(this);
            var accent = new NativeMethods.AccentPolicy
            {
                AccentState = NativeMethods.AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var accentStructSize = System.Runtime.InteropServices.Marshal.SizeOf(accent);
            var accentPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(accentStructSize);
            System.Runtime.InteropServices.Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new NativeMethods.WindowCompositionAttributeData
            {
                Attribute = NativeMethods.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            NativeMethods.SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            System.Runtime.InteropServices.Marshal.FreeHGlobal(accentPtr);

            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    _viewModel = vm;
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    _viewModel.RequestClose += (sender, args) =>
                    {
                         this.Hide();
                         _trapTimer.Stop();
                    };
                    _viewModel.RequestShow += (sender, args) =>
                    {
                        UpdateWindowRegion();
                        // Opacity is already 0 from XAML/Previous close

                        PositionWindowAtMouse();

                        // Reset Scale
                        WindowScale.ScaleX = 0;
                        WindowScale.ScaleY = 0;

                        this.Show();
                        this.Activate();

                        // Animate Scale
                        var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                        WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                        WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);

                        // Animate Opacity
                        var fadeAnim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                        this.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

                        DrawDynamicWheel(_viewModel.CurrentSlices);
                        _trapTimer.Start();
                    };

                    // Initial draw if any
                    if (_viewModel.CurrentSlices != null)
                         DrawDynamicWheel(_viewModel.CurrentSlices);
                }
            };
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentSlices))
            {
                DrawDynamicWheel(_viewModel.CurrentSlices);
                if (this.Visibility == Visibility.Visible)
                {
                    CenterMouse();
                }
            }
            if (e.PropertyName == nameof(MainViewModel.CenterText))
            {
                CenterLabel.Text = _viewModel.CenterText;
            }
        }

        private void SetupTimers()
        {
            _trapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Constants.TrapIntervalMs) };
            _trapTimer.Tick += TrapMouseInCircle;
        }

        // --- DRAWING ---
        // Ideally this would be a custom control, but keeping it here for simplicity as per plan
        private void DrawDynamicWheel(List<SliceConfig> items)
        {
            DynamicLayer.Children.Clear();
            SelectionHighlight.Opacity = 0;
            
            if (items == null) return;
            int count = items.Count;
            if (count == 0) return;

            double sliceAngle = 360.0 / count;

            for (int i = 0; i < count; i++)
            {
                // 1. Divider Line
                double angleRad = (i * sliceAngle) * (Math.PI / 180.0);
                Line div = new Line
                {
                    X1 = Constants.WheelRadius + Constants.InnerRadius * Math.Cos(angleRad),
                    Y1 = Constants.WheelRadius + Constants.InnerRadius * Math.Sin(angleRad),
                    X2 = Constants.WheelRadius + Constants.WheelRadius * Math.Cos(angleRad),
                    Y2 = Constants.WheelRadius + Constants.WheelRadius * Math.Sin(angleRad),
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 1
                };
                DynamicLayer.Children.Add(div);

                // 2. CALCULATE POSITION
                double midAngle = (i * sliceAngle) + (sliceAngle / 2);
                double midRad = midAngle * (Math.PI / 180.0);
                
                // Move content out to 115px from center
                double contentX = Constants.WheelRadius + 115 * Math.Cos(midRad);
                double contentY = Constants.WheelRadius + 115 * Math.Sin(midRad);

                // 3. ICON or TEXT?
                var slice = items[i];
                
                // Create a container (StackPanel) so we can stack Icon + Text if needed
                StackPanel panel = new StackPanel 
                { 
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // A. Check for Icon
                if (!string.IsNullOrEmpty(slice.Icon) && File.Exists(slice.Icon))
                {
                    Image icon = new Image
                    {
                        Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(slice.Icon))),
                        Width = 32, // Standard icon size
                        Height = 32,
                        Margin = new Thickness(0,0,0,5) // Space between icon and text
                    };
                    // Optimization: Decode pixel width to save RAM
                    // (WPF handles simple URIs fine for small apps)
                    panel.Children.Add(icon);
                }

                // B. Text Label
                TextBlock txt = new TextBlock
                {
                    Text = slice.Label,
                    Foreground = Brushes.White,
                    FontSize = 12, // Slightly smaller since we have icons
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                panel.Children.Add(txt);

                // 4. Position the Panel
                // We use RenderTransform to center the panel exactly on the calculated point
                panel.RenderTransform = new TranslateTransform(-20, -20); // Rough centering offset
                
                // Note: To center perfectly dynamically, we'd use a Canvas.SetLeft/Top, 
                // but Margins work if we set alignment to Top/Left on the container.
                panel.HorizontalAlignment = HorizontalAlignment.Left;
                panel.VerticalAlignment = VerticalAlignment.Top;
                panel.Margin = new Thickness(contentX, contentY, 0, 0);

                DynamicLayer.Children.Add(panel);
            }
        }

        // --- MOUSE LOGIC ---
        private void TrapMouseInCircle(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;

            NativeMethods.Win32Point mousePos;
            NativeMethods.GetCursorPos(out mousePos);
            var dpi = GetDpiScale();
            double centerX = (this.Left + Constants.WheelRadius) * dpi.X;
            double centerY = (this.Top + Constants.WheelRadius) * dpi.Y;
            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;

            // Trap Logic
            if (Math.Sqrt(dx * dx + dy * dy) > (Constants.WheelRadius * dpi.X))
            {
                double angle = Math.Atan2(dy, dx);
                NativeMethods.SetCursorPos(
                    (int)(centerX + (Constants.WheelRadius * dpi.X) * Math.Cos(angle)),
                    (int)(centerY + (Constants.WheelRadius * dpi.X) * Math.Sin(angle)));
            }

            // Selection Logic - Feed back to VM
            CheckSelection(dx, dy, dpi.X);
        }

        private void CheckSelection(double dx, double dy, double dpiScale)
        {
            if (_viewModel == null || _viewModel.CurrentSlices == null) return;

            int count = _viewModel.CurrentSlices.Count;
            int index = -1;

            if (Math.Sqrt(dx * dx + dy * dy) > (Constants.DeadzoneRadius * dpiScale))
            {
                double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
                if (angle < 0) angle += 360;
                
                index = (int)(angle / (360.0 / count));
                if (index >= 0 && index < count)
                {
                    _viewModel.SelectedSlice = _viewModel.CurrentSlices[index];
                }
                else
                {
                    _viewModel.SelectedSlice = null;
                }
            }
            else
            {
                 _viewModel.SelectedSlice = null;
            }

            UpdateHighlight(index, count);
        }

        private void UpdateHighlight(int index, int totalCount)
        {
            if (index < 0 || totalCount == 0)
            {
                SelectionHighlight.Opacity = 0;
                return;
            }

            double radius = Constants.WheelRadius - 2;
            double innerRadius = Constants.InnerRadius;
            double center = Constants.WheelRadius;
            double sliceAngle = 360.0 / totalCount;
            
            double startAngle = index * sliceAngle;
            double endAngle = (index + 1) * sliceAngle;

            double startRad = startAngle * (Math.PI / 180.0);
            double endRad = endAngle * (Math.PI / 180.0);

            // 1. Inner Arc Start (at Start Angle)
            Point innerStart = new Point(center + innerRadius * Math.Cos(startRad), center + innerRadius * Math.Sin(startRad));
            // 2. Outer Arc Start (at Start Angle)
            Point outerStart = new Point(center + radius * Math.Cos(startRad), center + radius * Math.Sin(startRad));
            // 3. Outer Arc End (at End Angle)
            Point outerEnd = new Point(center + radius * Math.Cos(endRad), center + radius * Math.Sin(endRad));
            // 4. Inner Arc End (at End Angle)
            Point innerEnd = new Point(center + innerRadius * Math.Cos(endRad), center + innerRadius * Math.Sin(endRad));

            PathFigure figure = new PathFigure();
            figure.StartPoint = innerStart;
            figure.Segments.Add(new LineSegment(outerStart, true));
            figure.Segments.Add(new ArcSegment(
                outerEnd,
                new Size(radius, radius), 
                0, 
                false,
                SweepDirection.Clockwise, 
                true));
            figure.Segments.Add(new LineSegment(innerEnd, true));
            figure.Segments.Add(new ArcSegment(
                innerStart,
                new Size(innerRadius, innerRadius),
                0,
                false,
                SweepDirection.CounterClockwise,
                true));
            
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            
            SelectionHighlight.Data = geometry;
            SelectionHighlight.Opacity = 1;
        }

        private void PositionWindowAtMouse()
        {
            NativeMethods.Win32Point mousePos;
            NativeMethods.GetCursorPos(out mousePos);
            var dpi = GetDpiScale();
            this.Left = (mousePos.X / dpi.X) - Constants.WheelRadius;
            this.Top = (mousePos.Y / dpi.Y) - Constants.WheelRadius;
        }
        
        private void CenterMouse()
        {
            var dpi = GetDpiScale();
            NativeMethods.SetCursorPos(
                (int)((this.Left + Constants.WheelRadius) * dpi.X),
                (int)((this.Top + Constants.WheelRadius) * dpi.Y));
        }

        private Point GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            return (source?.CompositionTarget != null) 
                ? new Point(source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22) 
                : new Point(1.0, 1.0);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
