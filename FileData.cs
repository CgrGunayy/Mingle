using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string ThumbnailPath { get; set; } = string.Empty;
        public FileType Type { get; set; }
    }
}
