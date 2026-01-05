using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QuickWheel.Core;
using QuickWheel.Interfaces;
using QuickWheel.Models;

namespace QuickWheel.Services.Actions
{
    public class PasteAction : ISliceAction
    {
        public void Execute(SliceConfig config)
        {
            if (string.IsNullOrEmpty(config.Data)) return;

            Clipboard.SetData(DataFormats.Text, config.Data);
            Task.Run(() =>
            {
                Thread.Sleep(250);
                InputSender.SendCtrlV();
            });
        }
    }
}
