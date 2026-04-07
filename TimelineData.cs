using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MingleWPF
{
    public class TimelineClip
    {
        public uint ClipID { get; set; }
        public string ClipTitle { get; set; } = string.Empty;
        public double ClipStartSecond { get; set; }
        public double ClipEndSecond { get; set; }
        public double ClipDuration { get; set; }
        public FileData? FileData { get; set; } = null;
    }

    public class TimelineData
    {
        public List<TimelineClip> Clips { get; private set; } = new List<TimelineClip>();

        public void AddClip(TimelineClip clip)
        {
            Clips.Add(clip);
        }

        public void RemoveClip(uint clipId)
        {
            int index = Clips.FindIndex(clip => clip.ClipID == clipId);
            if (index != -1)
                Clips.RemoveAt(index);
        }
    }
}
