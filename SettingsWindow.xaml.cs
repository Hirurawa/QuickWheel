using System;
using System.Windows;
using System.Windows.Input;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;
using QuickWheel.Models;

namespace QuickWheel
{
    public partial class SettingsWindow : Window
    {
        private readonly IInputService _inputService;
        private readonly ISettingsService _settingsService;
        private readonly AppSettings _settings;
        private bool _isListening;

        public event EventHandler SettingsChanged;

        public SettingsWindow(IInputService inputService, ISettingsService settingsService)
        {
            InitializeComponent();
            _inputService = inputService;
            _settingsService = settingsService;

            _settings = _settingsService.LoadSettings();
            UpdateKeyDisplay((Key)_settings.ActivationKey);

            // Initialize Sliders
            ActivationDelaySlider.Value = _settings.ActivationDelay > 0 ? _settings.ActivationDelay : Constants.ActivationDelayMs;
            HoverIntervalSlider.Value = _settings.HoverInterval > 0 ? _settings.HoverInterval : Constants.HoverIntervalMs;

            this.Closed += SettingsWindow_Closed;
        }

        private void UpdateKeyDisplay(Key key)
        {
            CurrentKeyText.Text = GetKeyName(key);
        }

        private void ActivationDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ActivationDelayText != null)
            {
                int val = (int)e.NewValue;
                ActivationDelayText.Text = $"{val} ms";
                if (_settings != null)
                {
                    _settings.ActivationDelay = val;
                }
            }
        }

        private void HoverIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HoverIntervalText != null)
            {
                int val = (int)e.NewValue;
                HoverIntervalText.Text = $"{val} ms";
                if (_settings != null)
                {
                    _settings.HoverInterval = val;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.SaveSettings(_settings);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private string GetKeyName(Key key)
        {
            if (key == Constants.KeyMouseLeft) return "Left Mouse";
            if (key == Constants.KeyMouseRight) return "Right Mouse";
            if (key == Constants.KeyMouseMiddle) return "Middle Mouse";
            if (key == Constants.KeyMouseX1) return "Mouse X1";
            if (key == Constants.KeyMouseX2) return "Mouse X2";
            return key.ToString();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isListening)
            {
                _isListening = true;
                StatusText.Visibility = Visibility.Visible;
                ChangeButton.IsEnabled = false;

                // Subscribe to global input
                _inputService.OnKeyDown += OnGlobalKeyDown;
            }
        }

        private void OnGlobalKeyDown(object sender, GlobalInputEventArgs e)
        {
            if (_isListening)
            {
                // Capture the key
                var newKey = e.Key;
                e.Handled = true; // Consume the event so it doesn't trigger other things

                Dispatcher.Invoke(() =>
                {
                    _settings.ActivationKey = (int)newKey;

                    // Don't save immediately!
                    // _settingsService.SaveSettings(_settings);

                    UpdateKeyDisplay(newKey);

                    // Reset UI
                    StatusText.Visibility = Visibility.Collapsed;
                    ChangeButton.IsEnabled = true;
                    _isListening = false;

                    // Unsubscribe
                    _inputService.OnKeyDown -= OnGlobalKeyDown;

                    // Don't notify main app yet
                    // SettingsChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        private void SettingsWindow_Closed(object sender, EventArgs e)
        {
            if (_isListening)
            {
                _inputService.OnKeyDown -= OnGlobalKeyDown;
            }
        }
    }
}
