using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Text.Json.Serialization;

namespace MingleWPF
{
    public enum FileType
    {
        Video,
        Image,
        Effect,
        Audio
    }

    public class FileData
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        [JsonIgnore] public BitmapImage? Thumbnail { get; set; }
        public string ThumbnailPath { get; set; } = string.Empty;
        public FileType Type { get; set; }
    }
}
