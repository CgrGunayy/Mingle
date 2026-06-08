using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MingleWPF
{
    public class TimelineData
    {
        public List<TimelineClip> Clips { get; private set; } = new List<TimelineClip>();
        public Dictionary<uint, TimelineLayer> Layers { get; private set; } = new Dictionary<uint, TimelineLayer>();

        Random randomIDGenerator;

        public TimelineData()
        {
            randomIDGenerator = new Random();
        }

        public void AddClip(TimelineClip clip, TimelineLayer layer)
        {
            clip.LayerID = layer.LayerID;
            layer.Clips.Add(clip);
            Clips.Add(clip);
        }

        public void RemoveClip(uint clipId)
        {
            int index = Clips.FindIndex(clip => clip.ClipID == clipId);
            if (index != -1)
            {
                uint layerID = Clips[index].LayerID;
                Layers[layerID].Clips.Remove(Clips[index]);
                Clips.RemoveAt(index);
            }
        }

        public void AddLayer(string layerName, LayerType layerType, int layerOrder, out uint layerID)
        {
            layerID = 0;
            while (layerID == 0 || Layers.ContainsKey(layerID)) layerID = rnd32();

            TimelineLayer newLayer = new TimelineLayer()
            {
                LayerID = layerID,
                LayerOrder = layerOrder,
                LayerTitle = layerName,
                LayerType = layerType
            };

            Layers.Add(layerID, newLayer);
        }

        public bool TryToGetClipAtSeconds(double seconds, out TimelineClip? resultClip, uint layerId = 0)
        {
            foreach (var _clip in Clips)
            {
                if (layerId != 0 && _clip.LayerID != layerId)
                    continue;

                if (_clip.StartSecondsOnTimeline <= seconds && _clip.EndSecondsOnTimeline >= seconds)
                {
                    resultClip = _clip;
                    return true;
                }
            }

            resultClip = null;
            return false;
        }

        private uint rnd32()
        {
            return (uint)(randomIDGenerator.Next(1 << 30)) << 2 | (uint)(randomIDGenerator.Next(1 << 2));
        }
    }
}
