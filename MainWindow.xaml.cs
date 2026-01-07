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
using System.Windows.Media.Effects;
using System.IO;

namespace QuickWheel
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer _trapTimer;
        private List<StackPanel> _slicePanels = new List<StackPanel>();

        public MainWindow()
        {
            InitializeComponent();

            // Warmup Window
            this.Left = -10000;
            this.Top = -10000;
            this.Show();
            this.Hide();

            SetupTimers();

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
                        PositionWindowAtMouse();
                        this.Show();
                        this.Activate();
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
            _slicePanels.Clear();
            
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
                    X1 = Constants.WheelCenter + Constants.InnerRadius * Math.Cos(angleRad),
                    Y1 = Constants.WheelCenter + Constants.InnerRadius * Math.Sin(angleRad),
                    X2 = Constants.WheelCenter + Constants.WheelRadius * Math.Cos(angleRad),
                    Y2 = Constants.WheelCenter + Constants.WheelRadius * Math.Sin(angleRad),
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 1
                };
                DynamicLayer.Children.Add(div);

                // 2. CALCULATE POSITION
                double midAngle = (i * sliceAngle) + (sliceAngle / 2);
                double midRad = midAngle * (Math.PI / 180.0);
                
                double distanceFromCenter = 90;

                // Move content out to 115px from center
                double contentX = Constants.WheelCenter + distanceFromCenter * Math.Cos(midRad);
                double contentY = Constants.WheelCenter + distanceFromCenter * Math.Sin(midRad);

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
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                panel.Children.Add(txt);

                // 4. Position the Panel                
                panel.HorizontalAlignment = HorizontalAlignment.Left;
                panel.VerticalAlignment = VerticalAlignment.Top;
                panel.Margin = new Thickness(contentX, contentY, 0, 0);

                panel.RenderTransformOrigin = new Point(0.5, 0.5);
                panel.Loaded += (s, e) =>
                {
                    if (s is StackPanel p)
                    {
                        // Shift Left by 50% of Width, Up by 50% of Height
                        p.RenderTransform = new TranslateTransform(-p.ActualWidth / 2, -p.ActualHeight / 2);
                    }
                };
                
                _slicePanels.Add(panel);
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
            double centerX = (this.Left + Constants.WheelCenter) * dpi.X;
            double centerY = (this.Top + Constants.WheelCenter) * dpi.Y;
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
            UpdateLabelVisuals(index);
        }

        private void UpdateHighlight(int index, int totalCount)
        {
            if (index < 0 || totalCount == 0)
            {
                SelectionHighlight.Opacity = 0;
                return;
            }

            double radius = Constants.WheelRadius - 2;
            double center = Constants.WheelCenter;
            double sliceAngle = 360.0 / totalCount;
            
            double startAngle = index * sliceAngle;
            double endAngle = (index + 1) * sliceAngle;

            double startRad = startAngle * (Math.PI / 180.0);
            double endRad = endAngle * (Math.PI / 180.0);

            Point centerPt = new Point(center, center);
            Point startPt = new Point(center + radius * Math.Cos(startRad), center + radius * Math.Sin(startRad));
            Point endPt = new Point(center + radius * Math.Cos(endRad), center + radius * Math.Sin(endRad));

            PathFigure figure = new PathFigure();
            figure.StartPoint = centerPt;
            figure.Segments.Add(new LineSegment(startPt, true));
            figure.Segments.Add(new ArcSegment(
                endPt, 
                new Size(radius, radius), 
                0, 
                false,
                SweepDirection.Clockwise, 
                true));
            
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            
            SelectionHighlight.Data = geometry;
            SelectionHighlight.Opacity = 1;
        }

        private void UpdateLabelVisuals(int activeIndex)
        {
            for (int i = 0; i < _slicePanels.Count; i++)
            {
                var panel = _slicePanels[i];
                
                TextBlock textBlock = null;
                foreach (var child in panel.Children)
                {
                    if (child is TextBlock tb)
                    {
                        textBlock = tb;
                        break;
                    }
                }

                if (i == activeIndex)
                {
                    // ACTIVE: Add Neon Glow
                    panel.Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 180, 0),
                        //Color = Color.FromRgb(255, 20, 147), // Cyberpunk Pink (DeepPink)
                        //Color = Color.FromRgb(57, 255, 20), // Electric Lime
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                    
                    panel.Opacity = 1.0;
                    if (textBlock != null) 
                        //textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 209, 255));
                        textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0));
                        //textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 20, 147));
                        //textBlock.Foreground = new SolidColorBrush(Color.FromRgb(57, 255, 20));
                }
                else
                {
                    // INACTIVE: Remove Glow
                    panel.Effect = null;
                    
                    // Optional: Dim the inactive ones slightly for contrast
                    panel.Opacity = 0.6;
                    
                    if (textBlock != null) 
                        //textBlock.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)); // Light Gray
                        textBlock.Foreground = Brushes.White;
                }
            }
        }

        private void PositionWindowAtMouse()
        {
            NativeMethods.Win32Point mousePos;
            NativeMethods.GetCursorPos(out mousePos);
            var dpi = GetDpiScale();
            this.Left = (mousePos.X / dpi.X) - Constants.WheelCenter;
            this.Top = (mousePos.Y / dpi.Y) - Constants.WheelCenter;
        }
        
        private void CenterMouse()
        {
            var dpi = GetDpiScale();
            NativeMethods.SetCursorPos(
                (int)((this.Left + Constants.WheelCenter) * dpi.X),
                (int)((this.Top + Constants.WheelCenter) * dpi.Y));
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
