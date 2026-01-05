using System;
using System.Windows.Input;

namespace QuickWheel.Interfaces
{
    public class GlobalInputEventArgs : EventArgs
    {
        public Key Key { get; }
        public bool Handled { get; set; }
        public GlobalInputEventArgs(Key key) { Key = key; }
    }

    public interface IInputService
    {
        event EventHandler<GlobalInputEventArgs> OnKeyDown;
        event EventHandler<GlobalInputEventArgs> OnKeyUp;
        void Enable();
        void Disable();
    }
}
