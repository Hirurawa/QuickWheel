using System;
using QuickWheel.Core;
using QuickWheel.Interfaces;

namespace QuickWheel.Services
{
    public class GlobalInputService : IInputService
    {
        private readonly GlobalKeyboardHook _hook;
        private readonly GlobalMouseHook _mouseHook;
        private bool _isEnabled;

        public event EventHandler<GlobalInputEventArgs>? OnKeyDown;
        public event EventHandler<GlobalInputEventArgs>? OnKeyUp;

        public GlobalInputService()
        {
            _hook = new GlobalKeyboardHook();
            _mouseHook = new GlobalMouseHook();

            // Keyboard Events
            _hook.OnKeyDown += (s, e) =>
            {
                var args = new GlobalInputEventArgs(e.Key);
                OnKeyDown?.Invoke(this, args);
                e.Handled = args.Handled;
            };
            _hook.OnKeyUp += (s, e) =>
            {
                var args = new GlobalInputEventArgs(e.Key);
                OnKeyUp?.Invoke(this, args);
                e.Handled = args.Handled;
            };

            // Mouse Events (Mapped to Key Events)
            _mouseHook.OnButtonDown += (s, e) =>
            {
                var args = new GlobalInputEventArgs(e.Key);
                OnKeyDown?.Invoke(this, args);
                e.Handled = args.Handled;
            };
            _mouseHook.OnButtonUp += (s, e) =>
            {
                var args = new GlobalInputEventArgs(e.Key);
                OnKeyUp?.Invoke(this, args);
                e.Handled = args.Handled;
            };
        }

        public void Enable()
        {
            if (!_isEnabled)
            {
                _hook.Hook();
                _mouseHook.Hook();
                _isEnabled = true;
            }
        }

        public void Disable()
        {
            if (_isEnabled)
            {
                _hook.Unhook();
                _mouseHook.Unhook();
                _isEnabled = false;
            }
        }
    }
}
