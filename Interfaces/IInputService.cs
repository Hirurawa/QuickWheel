using System;
using System.Windows.Input;

namespace QuickWheel.Interfaces
{
    public class InputEventArgs : EventArgs
    {
        public Key Key { get; }
        public bool Handled { get; set; }
        public InputEventArgs(Key key) { Key = key; }
    }

    public interface IInputService
    {
        event EventHandler<InputEventArgs> OnKeyDown;
        event EventHandler<InputEventArgs> OnKeyUp;
        void Enable();
        void Disable();
    }
}
