using System.Collections.Generic;

namespace QuickWheel.Models
{
    public class AppSettings
    {
        public List<SliceConfig> Slices { get; set; } = new List<SliceConfig>();
    }

    public class SliceConfig
    {
        public string Label { get; set; }
        public string Path { get; set; }
        public string Args { get; set; }
        public List<SliceConfig> Items { get; set; } // For Folders
    }
}
