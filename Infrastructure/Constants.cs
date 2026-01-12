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

        // Mouse "Keys" (Virtual)
        // Values > 200 to avoid conflict with standard Key enum (Max ~172)
        public const System.Windows.Input.Key KeyMouseLeft = (System.Windows.Input.Key)201;
        public const System.Windows.Input.Key KeyMouseRight = (System.Windows.Input.Key)202;
        public const System.Windows.Input.Key KeyMouseMiddle = (System.Windows.Input.Key)203;
        public const System.Windows.Input.Key KeyMouseX1 = (System.Windows.Input.Key)204;
        public const System.Windows.Input.Key KeyMouseX2 = (System.Windows.Input.Key)205;

        // public static readonly System.Windows.Input.Key ActivationButton = (System.Windows.Input.Key)169; // Key.XButton2 // DEPRECATED
        public static readonly System.IntPtr InputInjectionSignature = (System.IntPtr)0xFF55;
    }
}
