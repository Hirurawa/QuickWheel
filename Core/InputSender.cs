using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
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

        void IInputSender.SendCtrlV() => SendCtrlV();

        public void Send(Key key)
        {
            // Check if it's one of our custom mouse keys
            if (IsMouseKey(key))
            {
                SendMouseInput(key);
            }
            else
            {
                SendKeyboardInput(key);
            }
        }

        private bool IsMouseKey(Key key)
        {
            return key == Constants.KeyMouseLeft ||
                   key == Constants.KeyMouseRight ||
                   key == Constants.KeyMouseMiddle ||
                   key == Constants.KeyMouseX1 ||
                   key == Constants.KeyMouseX2;
        }

        private void SendMouseInput(Key key)
        {
            var inputs = new NativeMethods.INPUT[2];
            uint downFlag = 0;
            uint upFlag = 0;
            uint mouseData = 0;

            if (key == Constants.KeyMouseLeft)
            {
                downFlag = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_LEFTUP;
            }
            else if (key == Constants.KeyMouseRight)
            {
                downFlag = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_RIGHTUP;
            }
            else if (key == Constants.KeyMouseMiddle)
            {
                downFlag = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_MIDDLEUP;
            }
            else if (key == Constants.KeyMouseX1)
            {
                downFlag = NativeMethods.MOUSEEVENTF_XDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_XUP;
                mouseData = NativeMethods.XBUTTON1;
            }
            else if (key == Constants.KeyMouseX2)
            {
                downFlag = NativeMethods.MOUSEEVENTF_XDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_XUP;
                mouseData = NativeMethods.XBUTTON2_UINT;
            }

            // Down
            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = downFlag,
                        mouseData = mouseData,
                        dwExtraInfo = Constants.InputInjectionSignature
                    }
                }
            };

            // Up
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = upFlag,
                        mouseData = mouseData,
                        dwExtraInfo = Constants.InputInjectionSignature
                    }
                }
            };

            NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size);
        }

        private void SendKeyboardInput(Key key)
        {
            int vKey = KeyInterop.VirtualKeyFromKey(key);

            var inputs = new NativeMethods.INPUT[2];
            // Down
            inputs[0] = CreateKeyInput((ushort)vKey, false);
            // Up
            inputs[1] = CreateKeyInput((ushort)vKey, true);

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
                        dwExtraInfo = Constants.InputInjectionSignature // Use signature here too just in case
                    }
                }
            };
        }

        // Keep legacy method just in case, but deprecated
        public void SendForwardClick()
        {
             Send(Constants.KeyMouseX2);
        }
    }
}
