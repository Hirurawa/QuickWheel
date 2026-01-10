namespace QuickWheel.Infrastructure
{
    public static class Constants
    {
        public const double WindowSize = 340;
        public const double WheelCenter = WindowSize / 2;
        public const double WheelRadius = 150;
        public const double InnerRadius = 50;
        public const double DeadzoneRadius = 40;
        public const int TrapIntervalMs = 10;
        public const int HoverIntervalMs = 350;
        public const string SettingsFileName = "settings.json";

        // Activation / Input Injection
        public const int ActivationDelayMs = 200;
        public static readonly System.Windows.Input.Key ActivationButton = (System.Windows.Input.Key)169; // Key.XButton2
        public static readonly System.IntPtr InputInjectionSignature = (System.IntPtr)0xFF55;
    }
}
