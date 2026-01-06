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
                    X1 = Constants.WheelRadius, Y1 = Constants.WheelRadius,
                    X2 = Constants.WheelRadius + Constants.WheelRadius * Math.Cos(angleRad),
                    Y2 = Constants.WheelRadius + Constants.WheelRadius * Math.Sin(angleRad),
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
                        Constants.WheelRadius + 100 * Math.Cos(midRad),
                        Constants.WheelRadius + 100 * Math.Sin(midRad), 0, 0)
                };
                DynamicLayer.Children.Add(txt);
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
            double center = Constants.WheelRadius;
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
