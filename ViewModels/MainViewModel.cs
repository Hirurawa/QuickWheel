using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;
using QuickWheel.Models;
using QuickWheel.Services;

namespace QuickWheel.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger _logger;
        private readonly IInputService _inputService;
        private readonly ISettingsService _settingsService;
        private readonly ActionFactory _actionFactory;
        private readonly IInputSender _inputSender;

        private List<SliceConfig> _currentSlices;
        private Stack<List<SliceConfig>> _navigationStack;
        private DispatcherTimer _hoverTimer;
        private DispatcherTimer _activationTimer;
        private SliceConfig _lastHoveredSlice;
        private bool _isVisible;
        private string _centerText;
        private int _activationKey;

        public event EventHandler RequestClose;
        public event EventHandler RequestShow;

        public MainViewModel(
            ILogger logger,
            IInputService inputService,
            ISettingsService settingsService,
            ActionFactory actionFactory,
            IInputSender inputSender)
        {
            _logger = logger;
            _inputService = inputService;
            _settingsService = settingsService;
            _actionFactory = actionFactory;
            _inputSender = inputSender;

            _navigationStack = new Stack<List<SliceConfig>>();
            _inputService.OnKeyDown += OnKeyDown;
            _inputService.OnKeyUp += OnKeyUp;

            _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Constants.HoverIntervalMs) };
            _hoverTimer.Tick += HoverTimer_Tick;

            _activationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Constants.ActivationDelayMs) };
            _activationTimer.Tick += ActivationTimer_Tick;

            LoadSettings();
        }

        public List<SliceConfig> CurrentSlices
        {
            get => _currentSlices;
            set => SetProperty(ref _currentSlices, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string CenterText
        {
            get => _centerText;
            set => SetProperty(ref _centerText, value);
        }

        public void Initialize()
        {
            _inputService.Enable();
            _logger.Log("MainViewModel Initialized and Input Service Enabled.");
        }

        public void OpenSettings()
        {
            var win = new SettingsWindow(_inputService, _settingsService);
            win.SettingsChanged += (s, e) => LoadSettings(); // Reload settings when changed
            win.ShowDialog();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings();
            _currentSlices = settings.Slices;
            _activationKey = settings.ActivationKey;

            // Fallback if 0
            if (_activationKey == 0) _activationKey = 205; // MouseX2 default
        }

        private void OnKeyDown(object sender, GlobalInputEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Shutdown();
            }

            if ((int)e.Key == _activationKey)
            {
                e.Handled = true;
                if (!IsVisible && !_activationTimer.IsEnabled)
                {
                    _activationTimer.Start();
                }
            }
        }

        private void OnKeyUp(object sender, GlobalInputEventArgs e)
        {
            if ((int)e.Key == _activationKey)
            {
                e.Handled = true;

                if (_activationTimer.IsEnabled)
                {
                    // Timer still running -> It was a short CLICK.
                    _activationTimer.Stop();
                    // Send the original key click
                    _inputSender.Send((Key)_activationKey);
                }
                else if (IsVisible)
                {
                    // Wheel is open -> It was a HOLD.
                    _hoverTimer.Stop();
                    ExecuteCurrentSelection();
                }
            }
        }

        private void ActivationTimer_Tick(object sender, EventArgs e)
        {
            _activationTimer.Stop();
            // Timer finished -> User is HOLDING the button. Open the wheel.
            ResetState();
            IsVisible = true;
            RequestShow?.Invoke(this, EventArgs.Empty);
        }

        private SliceConfig _selectedSlice;
        public SliceConfig SelectedSlice
        {
            get => _selectedSlice;
            set
            {
                if (SetProperty(ref _selectedSlice, value))
                {
                    UpdateCenterText();
                    HandleHoverLogic();
                }
            }
        }

        private void UpdateCenterText()
        {
            if (SelectedSlice != null)
                CenterText = SelectedSlice.Label;
            else
                CenterText = _navigationStack.Count > 0 ? "Back" : "Cancel";
        }

        private void HandleHoverLogic()
        {
            if (SelectedSlice != _lastHoveredSlice)
            {
                _lastHoveredSlice = SelectedSlice;
                _hoverTimer.Stop();
                if (SelectedSlice != null && SelectedSlice.Items?.Count > 0)
                {
                    _hoverTimer.Start();
                }
            }
        }

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
            _navigationStack.Push(CurrentSlices);
            CurrentSlices = folder.Items;
            _logger.Log($"Navigated into {folder.Label}");
        }

        private void NavigateBack()
        {
            if (_navigationStack.Count > 0)
            {
                CurrentSlices = _navigationStack.Pop();
            }
        }

        private void ExecuteCurrentSelection()
        {
            var slice = SelectedSlice;

            if (slice == null) // Dead Zone
            {
                if (_navigationStack.Count > 0)
                    NavigateBack();
                else
                    HideWindow();
            }
            else if (slice.Items != null && slice.Items.Count > 0) // Enter Folder
            {
                NavigateInto(slice);
            }
            else // Execute
            {
                HideWindow();
                try
                {
                    _actionFactory.Execute(slice);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing slice {slice.Label}", ex);
                }
            }
        }

        private void ResetState()
        {
            var settings = _settingsService.LoadSettings();
            _currentSlices = settings.Slices;
            OnPropertyChanged(nameof(CurrentSlices));

            _navigationStack.Clear();
            SelectedSlice = null;
        }

        private void HideWindow()
        {
            IsVisible = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Shutdown()
        {
            _inputService.Disable();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
