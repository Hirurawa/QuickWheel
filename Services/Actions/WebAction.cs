using System.Diagnostics;
using QuickWheel.Interfaces;
using QuickWheel.Models;

namespace QuickWheel.Services.Actions
{
    public class WebAction : ISliceAction
    {
        public void Execute(SliceConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Path)) return;

            string url = config.Path;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
