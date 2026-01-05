using System;
using System.Runtime.InteropServices;

namespace QuickWheel.Core
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out Win32Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Point
        {
            public int X;
            public int Y;
        }
    }
}
