using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickWheel.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SliceType
    {
        App,
        Paste,
        Key,
        Web
    }

    public class AppSettings
    {
        public int ActivationKey { get; set; } = 205; // Default: MouseX2
        public int ActivationDelay { get; set; } = 200;
        public int HoverInterval { get; set; } = 350;
        public int FadeInDuration { get; set; } = 100;
        public List<SliceConfig> Slices { get; set; } = new List<SliceConfig>();
    }

    public class SliceConfig
    {
        public string Label { get; set; }
        public string Icon { get; set; }
        public SliceType Type { get; set; } = SliceType.App; // This defaults to "App" if missing in JSON
        public string Path { get; set; }
        public string Args { get; set; }
        public string Data { get; set; }  // For Paste text
        public List<SliceConfig> Items { get; set; }
    }
}
