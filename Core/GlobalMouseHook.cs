using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace QuickWheel.Core
{
    public class GlobalMouseHook
    {
        public event EventHandler<GlobalKeyEventArgs> OnButtonDown;
        public event EventHandler<GlobalKeyEventArgs> OnButtonUp;

        private NativeMethods.HookProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public GlobalMouseHook() => _proc = HookCallback;

        public void Hook() => _hookID = SetHook(_proc);
        public void Unhook() => NativeMethods.UnhookWindowsHookEx(_hookID);

        private IntPtr SetHook(NativeMethods.HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)NativeMethods.WM_XBUTTONDOWN || wParam == (IntPtr)NativeMethods.WM_XBUTTONUP)
                {
                    var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                    // High word of mouseData specifies which XButton was pressed.
                    // XBUTTON1 = 0x0001, XBUTTON2 = 0x0002
                    int xButton = (int)(hookStruct.mouseData >> 16);

                    if (xButton == NativeMethods.XBUTTON2)
                    {
                        // 169 is the integer value for Key.XButton2
                        var args = new GlobalKeyEventArgs((Key)169);

                        if (wParam == (IntPtr)NativeMethods.WM_XBUTTONDOWN)
                            OnButtonDown?.Invoke(this, args);
                        else if (wParam == (IntPtr)NativeMethods.WM_XBUTTONUP)
                            OnButtonUp?.Invoke(this, args);

                        if (args.Handled) return (IntPtr)1;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
