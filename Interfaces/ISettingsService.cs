using QuickWheel.Models;

namespace QuickWheel.Interfaces
{
    public interface ISettingsService
    {
        AppSettings LoadSettings();
        void SaveSettings(AppSettings settings);
    }
}
