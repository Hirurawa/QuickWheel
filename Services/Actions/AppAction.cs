using System.Diagnostics;
using QuickWheel.Interfaces;
using QuickWheel.Models;

namespace QuickWheel.Services.Actions
{
    public class AppAction : ISliceAction
    {
        public void Execute(SliceConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Path)) return;

            Process.Start(new ProcessStartInfo
            {
                FileName = config.Path,
                Arguments = config.Args ?? "",
                UseShellExecute = true
            });
        }
    }
}
