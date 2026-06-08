using System.Windows.Media;

namespace MingleWPF
{
    public class TimelineClip
    {
        public uint ClipID { get; set; }
        public string ClipTitle { get; set; } = string.Empty;
        public double StartSecondsOnTimeline { get; set; }
        public double EndSecondsOnTimeline { get; set; }
        public double ClipDuration { get; set; }
        public FileData? FileData { get; set; } = null;

        public uint LayerID { get; set; }

        // File Type == Video
        public double VideoStartSecond { get; set; }
        public double VideoDuration { get; set; }
    }
}
