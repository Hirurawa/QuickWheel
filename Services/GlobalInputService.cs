using System;
using QuickWheel.Core;
using QuickWheel.Interfaces;

namespace QuickWheel.Services
{
    public class GlobalInputService : IInputService
    {
        private readonly GlobalKeyboardHook _hook;
        private bool _isEnabled;

        public event EventHandler<InputEventArgs> OnKeyDown;
        public event EventHandler<InputEventArgs> OnKeyUp;

        public GlobalInputService()
        {
            _hook = new GlobalKeyboardHook();
            _hook.OnKeyDown += (s, e) =>
            {
                var args = new InputEventArgs(e.Key);
                OnKeyDown?.Invoke(this, args);
                e.Handled = args.Handled;
            };
            _hook.OnKeyUp += (s, e) =>
            {
                var args = new InputEventArgs(e.Key);
                OnKeyUp?.Invoke(this, args);
                e.Handled = args.Handled;
            };
        }

        public void Enable()
        {
            if (!_isEnabled)
            {
                _hook.Hook();
                _isEnabled = true;
            }
        }

        public void Disable()
        {
            if (_isEnabled)
            {
                _hook.Unhook();
                _isEnabled = false;
            }
        }
    }
}
