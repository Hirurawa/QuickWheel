using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace QuickWheel.Core
{
    public class GlobalKeyEventArgs : EventArgs
    {
        public Key Key { get; }
        public bool Handled { get; set; } = false;
        public GlobalKeyEventArgs(Key key) { Key = key; }
    }

    public class GlobalKeyboardHook
    {
        public event EventHandler<GlobalKeyEventArgs> OnKeyDown;
        public event EventHandler<GlobalKeyEventArgs> OnKeyUp;

        private NativeMethods.HookProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public GlobalKeyboardHook() => _proc = HookCallback;
        public void Hook() => _hookID = SetHook(_proc);
        public void Unhook() => NativeMethods.UnhookWindowsHookEx(_hookID);

        private IntPtr SetHook(NativeMethods.HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                var args = new GlobalKeyEventArgs(key);

                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                    OnKeyDown?.Invoke(this, args);
                else if (wParam == (IntPtr)NativeMethods.WM_KEYUP || wParam == (IntPtr)NativeMethods.WM_SYSKEYUP)
                    OnKeyUp?.Invoke(this, args);

                if (args.Handled) return (IntPtr)1;
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
