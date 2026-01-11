using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;
using QuickWheel.Models;
using QuickWheel.Services;
using QuickWheel.ViewModels;

namespace QuickWheel.Tests
{
    [TestClass]
    public class ActivationLogicTests
    {
        private MockLogger _logger;
        private MockInputService _inputService;
        private MockSettingsService _settingsService;
        private ActionFactory _actionFactory;
        private MockInputSender _inputSender;
        private MainViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _logger = new MockLogger();
            _inputService = new MockInputService();
            _settingsService = new MockSettingsService();
            _actionFactory = new ActionFactory();
            _inputSender = new MockInputSender();

            _viewModel = new MainViewModel(_logger, _inputService, _settingsService, _actionFactory, _inputSender);
        }

        [TestMethod]
        public void ButtonDown_StartsTimer_DoesNotShowImmediately()
        {
            // Act
            _inputService.TriggerKeyDown(Constants.ActivationButton);

            // Assert
            // Cannot easily test DispatcherTimer in unit tests without a dispatcher frame,
            // but we can assert state.
            Assert.IsFalse(_viewModel.IsVisible, "Wheel should not be visible immediately on Button Down");

            // Note: In a real environment with Dispatcher, we would check if timer is enabled.
            // Since DispatcherTimer requires a UI thread message loop, this is limited.
        }

        [TestMethod]
        public void ButtonUp_BeforeTimerExpires_SendsClick()
        {
            // Arrange
            _inputService.TriggerKeyDown(Constants.ActivationButton);

            // Act: Button Up immediately
            _inputService.TriggerKeyUp(Constants.ActivationButton);

            // Assert
            Assert.IsTrue(_inputSender.WasClickSent, "Should have sent a forwarded click");
            Assert.IsFalse(_viewModel.IsVisible, "Wheel should remain hidden");
        }
    }

    // Mocks
    public class MockLogger : ILogger
    {
        public void Log(string message) { }
        public void LogError(string message, Exception ex) { }
    }

    public class MockInputService : IInputService
    {
        public event EventHandler<GlobalInputEventArgs> OnKeyDown;
        public event EventHandler<GlobalInputEventArgs> OnKeyUp;

        public void Enable() { }
        public void Disable() { }

        public void TriggerKeyDown(Key key)
        {
            OnKeyDown?.Invoke(this, new GlobalInputEventArgs(key));
        }

        public void TriggerKeyUp(Key key)
        {
            OnKeyUp?.Invoke(this, new GlobalInputEventArgs(key));
        }
    }

    public class MockSettingsService : ISettingsService
    {
        public SettingsModel LoadSettings()
        {
            return new SettingsModel { Slices = new List<SliceConfig>() };
        }

        public void SaveSettings(SettingsModel settings) { }
    }

    public class MockInputSender : IInputSender
    {
        public bool WasClickSent { get; private set; }
        public void SendForwardClick()
        {
            WasClickSent = true;
        }
    }
}
