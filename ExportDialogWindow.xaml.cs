using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;

namespace MingleWPF
{
    public partial class ExportDialogWindow : Window
    {
        public bool IsAborted { get; private set; } = false;
        public string LayerNameInput { get; private set; } = string.Empty;
        public string LayerTypeInput { get; private set; } = string.Empty;

        TimelineData data;

        public ExportDialogWindow(TimelineData data)
        {
            this.data = data;
            InitializeComponent();
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsAborted = true;
            this.Close();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            await ExportTimelineAsync(videoNameInputBox.Text + ".mp4");
        }

        public async Task ExportTimelineAsync(string exportPath)
        {
            List<TimelineClip> clips = data.Clips;
            if (clips.Count == 0)
            {
                MessageBox.Show("Henüz projeniz boş", "Başarısız");
                return;
            }

            double totalDuration = clips.Max(c => c.EndSecondsOnTimeline);
            string durationStr = totalDuration.ToString(CultureInfo.InvariantCulture);

            StringBuilder arguments = new StringBuilder();

            arguments.Append($"-f lavfi -i color=c=black:s=1920x1080:r=30:d={durationStr} ");

            foreach (var clip in clips)
            {
                if (clip.FileData != null)
                {
                    string t = clip.ClipDuration.ToString(CultureInfo.InvariantCulture);

                    if (clip.FileData.Type == FileType.Image)
                    {
                        arguments.Append($"-loop 1 -framerate 30 -t {t} -i \"{clip.FileData.Path}\" ");
                    }
                    else
                    {
                        string ss = clip.VideoStartSecond.ToString(CultureInfo.InvariantCulture);
                        arguments.Append($"-ss {ss} -t {t} -i \"{clip.FileData.Path}\" ");
                    }
                }
            }

            arguments.Append("-filter_complex \"");

            List<string> audioInputs = new List<string>();

            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                int inputIndex = i + 1;

                string delay = clip.StartSecondsOnTimeline.ToString(CultureInfo.InvariantCulture);
                int delayMs = (int)(clip.StartSecondsOnTimeline * 1000);

                arguments.Append($"[{inputIndex}:v]scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setpts=PTS-STARTPTS+{delay}/TB[v{inputIndex}]; ");

                if (clip.FileData?.Type == FileType.Video)
                {
                    arguments.Append($"[{inputIndex}:a]adelay={delayMs}|{delayMs}[a{inputIndex}]; ");
                    audioInputs.Add($"[a{inputIndex}]");
                }
            }

            string currentBackground = "[0:v]";

            var orderedClips = clips.Select((clip, index) => new { clip, inputIndex = index + 1 })
                                        .OrderBy(c => data.Layers[c.clip.LayerID].LayerOrder)
                                        .ToList();

            for (int i = 0; i < orderedClips.Count; i++)
            {
                int inIdx = orderedClips[i].inputIndex;
                string nextBackground = $"[bg{inIdx}]";

                if (i == orderedClips.Count - 1) nextBackground = "[outv]";

                arguments.Append($"{currentBackground}[v{inIdx}]overlay=eof_action=pass{nextBackground}; ");

                currentBackground = nextBackground;
            }

            if (audioInputs.Count == 1)
            {
                arguments.Append($"{audioInputs[0]}anull[outa]");
            }
            else if (audioInputs.Count > 1)
            {
                foreach (string audioTag in audioInputs)
                {
                    arguments.Append(audioTag);
                }

                arguments.Append($"amix=inputs={audioInputs.Count}:dropout_transition=0:normalize=0[outa]");
            }

            arguments.Append("\" ");

            arguments.Append("-map \"[outv]\" ");

            if (audioInputs.Count > 0)
            {
                arguments.Append("-map \"[outa]\" -c:a aac -b:a 320k ");
            }

            arguments.Append($"-c:v libx264 -preset fast -y \"{exportPath}\"");

            try
            {
                ProgressBarDialogWindow progressBar = new ProgressBarDialogWindow();

                progressBar.RenderProgressBar.Value = 0;
                progressBar.RenderProgressText.Text = "% 0.0";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("time="))
                        {
                            Match match = Regex.Match(e.Data, @"time=(?<time>\d{2}:\d{2}:\d{2}\.\d+)");

                            if (match.Success)
                            {
                                if (TimeSpan.TryParse(match.Groups["time"].Value, out TimeSpan renderedTime))
                                {
                                    double percentage = (renderedTime.TotalSeconds / totalDuration) * 100;
                                    if (percentage > 100) percentage = 100;

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        progressBar.RenderProgressBar.Value = percentage;
                                        progressBar.RenderProgressText.Text = $"% {percentage:F1}";
                                    });
                                }
                            }
                        }
                    };

                    process.Start();

                    progressBar.Show();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        progressBar.RenderProgressBar.Value = 100;
                        progressBar.RenderProgressText.Text = "% 100";
                        progressBar.RenderResultText.Visibility = Visibility.Visible;
                        progressBar.RenderResultText.Content = "Export Completed!";
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{exportPath}\"");
                    }
                    else
                    {
                        string errorLog = await process.StandardError.ReadToEndAsync();
                        progressBar.RenderResultText.Visibility = Visibility.Visible;
                        progressBar.RenderResultText.Content = "Export Failed!";
                        MessageBox.Show($"Render failed!\n\nError Code: {process.ExitCode}\nDetails:\n{errorLog}", "Render Error");
                    }

                    this.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Render couldn't be started! ffmpeg.exe might not be found.\n\nError:\n{ex.Message}", "Critical Error");
            }
        }
    }
}
