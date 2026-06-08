using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MingleWPF
{
    public class ClipSaveData
    {
        public uint ClipId { get; set; }
        public string ClipTitle { get; set; } = string.Empty;
        public double StartSecondsOnTimeline { get; set; }
        public double EndSecondsOnTimeline { get; set; }
        public double ClipDuration { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public uint LayerID { get; set; }

        public double VideoStartSecond { get; set; }
        public double VideoDuration { get; set; }
    }

    public class LayerSaveData
    {
        public uint LayerID { get; set; }
        public string LayerTitle { get; set; } = string.Empty;
        public int LayerOrder { get; set; }
        public LayerType LayerType { get; set; }
        public List<ClipSaveData> Clips { get; set; } = new List<ClipSaveData>();
    }

    public class ProjectSaveData
    {
        public List<FileData> ImportedFiles { get; set; } = new List<FileData>();
        public List<LayerSaveData> Layers { get; set; } = new List<LayerSaveData>();
        public double PlayheadSeconds { get; set; }
    }
}
