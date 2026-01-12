using System.Windows.Input;

namespace QuickWheel.Interfaces
{
    public interface IInputSender
    {
        void SendCtrlV();
        void Send(Key key);
    }
}
