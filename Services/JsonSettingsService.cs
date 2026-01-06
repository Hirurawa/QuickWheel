using System;
using System.IO;
using System.Text.Json;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;
using QuickWheel.Models;

namespace QuickWheel.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly ILogger _logger;

        public JsonSettingsService(ILogger logger)
        {
            _logger = logger;
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(Constants.SettingsFileName))
                {
                    string json = File.ReadAllText(Constants.SettingsFileName);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                    _logger.Log("Settings loaded successfully.");
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load settings", ex);
            }

            _logger.Log("Using default settings.");
            return new AppSettings();
        }
    }
}
