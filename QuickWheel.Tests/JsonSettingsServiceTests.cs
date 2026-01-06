using System;
using System.Collections.Generic;
using QuickWheel.Interfaces;
using QuickWheel.Models;
using QuickWheel.Services;
using Xunit;

namespace QuickWheel.Tests
{
    public class JsonSettingsServiceTests
    {
        private class MockLogger : ILogger
        {
            public void Log(string message) { }
            public void LogError(string message, Exception ex = null) { }
        }

        [Fact]
        public void LoadSettings_ReturnsDefault_WhenFileMissing()
        {
            // Arrange
            var logger = new MockLogger();
            var service = new JsonSettingsService(logger);

            // Act
            var settings = service.LoadSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.Slices);
            Assert.Empty(settings.Slices);
        }
    }
}
