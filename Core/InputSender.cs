using System;
using System.Runtime.InteropServices;
using QuickWheel.Core;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;

namespace QuickWheel.Core
{
    public class InputSender : IInputSender
    {
        public static void SendCtrlV()
        {
            var inputs = new NativeMethods.INPUT[4];

            // 1. Ctrl Down (VK_CONTROL = 0x11)
            inputs[0] = CreateKeyInput(0x11, false);
            // 2. V Down (VK_V = 0x56)
            inputs[1] = CreateKeyInput(0x56, false);
            // 3. V Up
            inputs[2] = CreateKeyInput(0x56, true);
            // 4. Ctrl Up
            inputs[3] = CreateKeyInput(0x11, true);

            // Send
            NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size);
        }

        private static NativeMethods.INPUT CreateKeyInput(ushort code, bool keyUp)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = code,
                        wScan = 0,
                        dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        public void SendForwardClick()
        {
            var inputs = new NativeMethods.INPUT[2];

            // XBUTTON2 Down
            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = NativeMethods.MOUSEEVENTF_XDOWN,
                        mouseData = NativeMethods.XBUTTON2_UINT,
                        dwExtraInfo = Constants.InputInjectionSignature
                    }
                }
            };

            // XBUTTON2 Up
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = NativeMethods.MOUSEEVENTF_XUP,
                        mouseData = NativeMethods.XBUTTON2_UINT,
                        dwExtraInfo = Constants.InputInjectionSignature
                    }
                }
            };

            NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size);
        }
    }
}
