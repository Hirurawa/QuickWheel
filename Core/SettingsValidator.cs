using System;

namespace QuickWheel.Core
{
    public static class SettingsValidator
    {
        public static bool ValidateRange(string input, double min, double max, out int result)
        {
            result = 0;
            if (int.TryParse(input, out int parsed))
            {
                if (parsed >= min && parsed <= max)
                {
                    result = parsed;
                    return true;
                }
            }
            return false;
        }
    }
}
