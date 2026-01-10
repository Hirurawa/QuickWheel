using System.Runtime.InteropServices;
using QuickWheel.Core;
using QuickWheel.Infrastructure;
using QuickWheel.Interfaces;

namespace QuickWheel.Services
{
    public class InputSender : IInputSender
    {
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
                        mouseData = NativeMethods.XBUTTON2,
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
                        mouseData = NativeMethods.XBUTTON2,
                        dwExtraInfo = Constants.InputInjectionSignature
                    }
                }
            };

            NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size);
        }
    }
}
