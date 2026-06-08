namespace MingleWPF
{
    public enum LayerType
    {
        Video,
        Audio
    }

    public class TimelineLayer
    {
        public uint LayerID { get; set; }
        public int LayerOrder { get; set; }
        public string LayerTitle { get; set; } = string.Empty;
        public LayerType LayerType { get; set; }

        public List<TimelineClip> Clips { get; set; } = new List<TimelineClip>();
    }
}
