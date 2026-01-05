using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickWheel.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SliceType
    {
        App,
        Paste,
        Key
    }

    public class AppSettings
    {
        public List<SliceConfig> Slices { get; set; } = new List<SliceConfig>();
    }

    public class SliceConfig
    {
        public string Label { get; set; }
        
        // This defaults to "App" if missing in JSON
        public SliceType Type { get; set; } = SliceType.App; 
        
        public string Path { get; set; }
        public string Args { get; set; }
        public string Data { get; set; }  // For Paste text
        public List<SliceConfig> Items { get; set; }
    }
}