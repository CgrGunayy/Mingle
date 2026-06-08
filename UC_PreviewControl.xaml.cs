using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MingleWPF
{
    public partial class UC_PreviewControl : UserControl
    {


        private TimelineClip? currentPreviewClip = null;

        public UC_PreviewControl()
        {
            InitializeComponent();
        }

        public void Play()
        {
            if (currentPreviewClip == null)
                return;

            PreviewPlayer.Play();
        }

        public void Pause()
        {

            PreviewPlayer.Pause();
        }

        public void UpdatePreview(TimelineData data, double playheadSeconds, bool isPlaying = false)
        {
            var currentClip = data.Clips
                .Where(c => playheadSeconds >= c.StartSecondsOnTimeline && playheadSeconds < c.EndSecondsOnTimeline)
                .OrderByDescending(c => data.Layers[c.LayerID].LayerOrder)
                .FirstOrDefault();

            if (currentClip != null)
            {
                NoMediaText.Visibility = Visibility.Hidden;
                PreviewPlayer.Visibility = Visibility.Visible;

                if (currentPreviewClip != currentClip && currentClip.FileData != null)
                {
                    PreviewPlayer.Source = new Uri(currentClip.FileData.Path);
                    currentPreviewClip = currentClip;

                    double initialPos = (playheadSeconds - currentClip.StartSecondsOnTimeline) + currentClip.VideoStartSecond;
                    PreviewPlayer.Position = TimeSpan.FromSeconds(initialPos);

                    if (isPlaying) PreviewPlayer.Play();
                    else PreviewPlayer.Pause();
                }
                else
                {
                    if (!isPlaying)
                    {
                        double currentPos = (playheadSeconds - currentClip.StartSecondsOnTimeline) + currentClip.VideoStartSecond;
                        PreviewPlayer.Position = TimeSpan.FromSeconds(currentPos);
                    }
                }
            }
            else
            {
                if (currentPreviewClip != null)
                {
                    PreviewPlayer.Stop();
                    PreviewPlayer.Source = null;
                    currentPreviewClip = null;
                }

                PreviewPlayer.Visibility = Visibility.Hidden;
                NoMediaText.Visibility = Visibility.Visible;
            }
        }
    }
}
