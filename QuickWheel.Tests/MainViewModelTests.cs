using System;
using System.Collections.Generic;
using QuickWheel.Interfaces;
using QuickWheel.Models;
using QuickWheel.Services;
using QuickWheel.ViewModels;
using Xunit;

namespace QuickWheel.Tests
{
    public class MainViewModelTests
    {
        private class MockLogger : ILogger { public void Log(string m) { } public void LogError(string m, Exception e) { } }
        private class MockInput : IInputService
        {
            public event EventHandler<InputEventArgs> OnKeyDown;
            public event EventHandler<InputEventArgs> OnKeyUp;
            public void Enable() { }
            public void Disable() { }
        }
        private class MockSettings : ISettingsService
        {
            public AppSettings LoadSettings() => new AppSettings
            {
                Slices = new List<SliceConfig>
                {
                    new SliceConfig { Label = "Folder", Items = new List<SliceConfig> { new SliceConfig { Label = "Item" } } }
                }
            };
        }

        [Fact]
        public void Initialization_LoadsSettings()
        {
            var vm = new MainViewModel(new MockLogger(), new MockInput(), new MockSettings(), new ActionFactory());
            Assert.NotNull(vm.CurrentSlices);
            Assert.Single(vm.CurrentSlices);
            Assert.Equal("Folder", vm.CurrentSlices[0].Label);
        }

        [Fact]
        public void Selection_UpdatesCenterText()
        {
            var vm = new MainViewModel(new MockLogger(), new MockInput(), new MockSettings(), new ActionFactory());
            var slice = vm.CurrentSlices[0];

            vm.SelectedSlice = slice;

            Assert.Equal("Folder", vm.CenterText);
        }
    }
}
