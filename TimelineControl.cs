using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using FFMpegCore;

namespace MingleWPF
{
    public class TimelineControl : FrameworkElement
    {
        public TimelineData TimelineData { get; private set; }

        private readonly SolidColorBrush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E0E0E"));
        private readonly Pen _tickPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70594043")), 1);
        private readonly Pen _playheadPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA")), 2);
        private readonly Pen trackLinePen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222222")), 1);
        private readonly SolidColorBrush _playheadRectBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA"));
        private readonly SolidColorBrush _textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70E0BEC1"));

        SolidColorBrush clipBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A4133C"));
        SolidColorBrush clipArrowBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA"));
        Pen clipBorderPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35A4133C")), 1);

        public double PixelsPerSecond { get; set; } = 100;
        public double ScrollOffset { get; set; } = 0;
        public double PlayheadSeconds { get; set; } = 0;
        public double LayerHeight { get; set; } = 50;
        public double TimelineTopHeight { get; set; } = 50;

        private bool isDragging;
        private bool isDraggingPlayhead;
        private bool isDraggingClip;

        private enum ClipDragMode { None, Move, TrimLeft, TrimRight }
        private ClipDragMode currentClipDragMode = ClipDragMode.None;

        private Point clipDragStartScreenPoint;
        private TimelineClip? pressedClip;

        private Point dragStartScreenPoint;
        private bool ignoreNextMouseMove;

        UC_PreviewControl previewPlayer;
        public void SetPreviewPlayer(UC_PreviewControl preview) { this.previewPlayer = preview; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        public TimelineControl()
        {
            this.ClipToBounds = true;
            this.AllowDrop = true;

            TimelineData = new TimelineData();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            Rect bounds = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            dc.DrawRectangle(_backgroundBrush, null, bounds);

            double minPixelGap = 60;
            int stepSeconds = CalculateStepSeconds(PixelsPerSecond, minPixelGap);

            int startSec = (int)(ScrollOffset / PixelsPerSecond);
            int endSec = startSec + (int)(ActualWidth / PixelsPerSecond) + 2;

            startSec = (startSec / stepSeconds) * stepSeconds;

            for (int i = startSec; i <= endSec; i += stepSeconds)
            {
                double xPos = (i * PixelsPerSecond) - ScrollOffset;

                dc.DrawLine(_tickPen, new Point(xPos, 15), new Point(xPos, 27));

                TimeSpan time = TimeSpan.FromSeconds(i);
                string timeFormat = (time.TotalHours >= 1 || stepSeconds >= 3600) ? @"hh\:mm\:ss" : @"mm\:ss";

                FormattedText text = new FormattedText(
                    time.ToString(timeFormat),
                    CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Consolas"), 10, _textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(text, new Point(xPos + 4, 15));
            }


            int visibleTracks = (int)((ActualHeight - TimelineTopHeight) / LayerHeight) + 1;

            for (int i = 0; i < visibleTracks; i++)
            {
                double y = TimelineTopHeight + i * LayerHeight;
                dc.DrawLine(trackLinePen, new Point(0, y), new Point(ActualWidth, y));
            }

            var layerControls = MainWindow.LayersPanel.Children;

            int layerIndex = 0;
            foreach (UC_LayerControl layerControl in layerControls)
            {
                if (!TimelineData.Layers.Keys.Contains(layerControl.LayerID))
                    continue;

                TimelineLayer layer = TimelineData.Layers[layerControl.LayerID];

                foreach (var clip in layer.Clips)
                {
                    double startX = (clip.StartSecondsOnTimeline * PixelsPerSecond) - ScrollOffset;
                    double width = (clip.EndSecondsOnTimeline - clip.StartSecondsOnTimeline) * PixelsPerSecond;

                    double startY = TimelineTopHeight + (layerIndex * LayerHeight) + 5;
                    double clipHeight = LayerHeight - 10;

                    Rect clipRect = new Rect(startX, startY, width, clipHeight);

                    if (clipRect.Right > 0 && clipRect.Left < ActualWidth)
                    {
                        dc.DrawRectangle(clipBrush, clipBorderPen, clipRect);

                        if (clip.FileData?.Thumbnail != null)
                        {
                            dc.PushClip(new RectangleGeometry(clipRect));

                            double aspectRatio = clip.FileData.Thumbnail.Width / clip.FileData.Thumbnail.Height;
                            double thumbnailWidth = clipHeight * aspectRatio;

                            ImageBrush tileBrush = new ImageBrush(clip.FileData.Thumbnail)
                            {
                                TileMode = TileMode.Tile,
                                Stretch = Stretch.UniformToFill,
                                ViewportUnits = BrushMappingMode.Absolute,

                                Viewport = new Rect(-ScrollOffset, startY, thumbnailWidth, clipHeight)
                            };

                            dc.DrawRectangle(tileBrush, null, clipRect);

                            dc.Pop();
                        }

                        Point mousePosition = Mouse.GetPosition(this);
                        if (clipRect.Contains(mousePosition))
                        {
                            Rect leftArrowRect = new Rect(clipRect.X, clipRect.Y, 10, clipRect.Height);
                            Rect rightArrowRect = new Rect(clipRect.X + clipRect.Width - 10, clipRect.Y, 10, clipRect.Height);
                            dc.DrawRectangle(clipArrowBrush, clipBorderPen, leftArrowRect);
                            dc.DrawRectangle(clipArrowBrush, clipBorderPen, rightArrowRect);
                        }

                        double safeTextWidth = width - 10;

                        if (safeTextWidth > 0)
                        {
                            FormattedText clipText = new FormattedText(
                            clip.ClipTitle,
                            System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            new Typeface("Consolas"), 10, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                            clipText.MaxTextWidth = safeTextWidth;
                            clipText.MaxLineCount = 1;
                            clipText.Trimming = TextTrimming.CharacterEllipsis;

                            Rect textBgRect = new Rect(startX, startY, width, 16);
                            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), null, textBgRect);

                            dc.DrawText(clipText, new Point(startX + 5, startY + 2));
                        }
                    }
                }

                layerIndex++;
            }

            double playheadX = (PlayheadSeconds * PixelsPerSecond) - ScrollOffset;
            if (playheadX >= 0 && playheadX <= ActualWidth)
            {
                dc.DrawLine(_playheadPen, new Point(playheadX, 0), new Point(playheadX, ActualHeight));

                Rect playheadRect = new Rect(playheadX - 5, 0, 10, 10);
                dc.DrawRectangle(_playheadRectBrush, null, playheadRect);
            }
        }

        private int CalculateStepSeconds(double pixelsPerSecond, double minPixelGap)
        {
            int stepSeconds = 1;
            if (pixelsPerSecond * 1 < minPixelGap) stepSeconds = 2;
            if (pixelsPerSecond * 2 < minPixelGap) stepSeconds = 5;
            if (pixelsPerSecond * 5 < minPixelGap) stepSeconds = 10;
            if (pixelsPerSecond * 10 < minPixelGap) stepSeconds = 30;
            if (pixelsPerSecond * 30 < minPixelGap) stepSeconds = 60;
            if (pixelsPerSecond * 60 < minPixelGap) stepSeconds = 300;
            if (pixelsPerSecond * 300 < minPixelGap) stepSeconds = 600;
            if (pixelsPerSecond * 600 < minPixelGap) stepSeconds = 1800;
            if (pixelsPerSecond * 1800 < minPixelGap) stepSeconds = 3600;
            if (pixelsPerSecond * 3600 < minPixelGap) stepSeconds = 18000;

            return stepSeconds;
        }

        protected async override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent("MingleFile"))
            {
                FileData? file = e.Data.GetData("MingleFile") as FileData;
                if (file == null)
                    return;

                Point dropPoint = e.GetPosition(this);
                double dropSecond = (dropPoint.X + ScrollOffset) / PixelsPerSecond;

                int droppedLayer = (int)((dropPoint.Y - TimelineTopHeight) / LayerHeight);
                if (droppedLayer < 0) droppedLayer = 0;

                if (droppedLayer >= TimelineData.Layers.Count)
                    return;

                UC_LayerControl layerControl = (UC_LayerControl)(MainWindow.LayersPanel.Children[droppedLayer]);

                TimelineLayer layer = TimelineData.Layers[layerControl.LayerID];
                switch (layer.LayerType)
                {
                    case LayerType.Video:
                        if (file.Type == FileType.Audio)
                            return;
                        break;
                    case LayerType.Audio:
                        if (file.Type == FileType.Image || file.Type == FileType.Video || file.Type == FileType.Effect)
                            return;
                        break;
                }

                try
                {
                    TimelineClip clip = new TimelineClip();
                    clip.ClipTitle = file.Name;
                    clip.StartSecondsOnTimeline = dropSecond;
                    clip.FileData = file;

                    switch (file.Type)
                    {
                        case FileType.Video:
                            IMediaAnalysis videoInfo = await FFProbe.AnalyseAsync(file.Path);
                            clip.EndSecondsOnTimeline = clip.StartSecondsOnTimeline + videoInfo.Duration.TotalSeconds;
                            clip.VideoDuration = videoInfo.Duration.TotalSeconds;
                            break;
                        case FileType.Image:
                            clip.EndSecondsOnTimeline = clip.StartSecondsOnTimeline + 5.0;
                            break;
                        case FileType.Audio:
                            clip.EndSecondsOnTimeline = clip.StartSecondsOnTimeline + 5.0;
                            break;
                    }

                    clip.ClipDuration = clip.EndSecondsOnTimeline - clip.StartSecondsOnTimeline;

                    TimelineData.AddClip(clip, layer);

                    InvalidateVisual();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't read file information.\nError: " + ex.Message);
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double playheadX = (PlayheadSeconds * PixelsPerSecond) - ScrollOffset;
                Rect playheadRect = new Rect(playheadX - 5, 0, 10, 10);

                Point mousePoint = e.GetPosition(this);
                double mousePointSeconds = (mousePoint.X + ScrollOffset) / PixelsPerSecond;

                isDragging = true;
                if (playheadRect.Contains(mousePoint))
                    isDraggingPlayhead = true;

                if (mousePoint.Y > TimelineTopHeight)
                {
                    int pressedLayer = (int)((mousePoint.Y - TimelineTopHeight) / LayerHeight);

                    if (pressedLayer < MainWindow.LayersPanel.Children.Count)
                    {
                        UC_LayerControl layerControl = (UC_LayerControl)(MainWindow.LayersPanel.Children[pressedLayer]);
                        TimelineLayer layer = TimelineData.Layers[layerControl.LayerID];

                        if (TimelineData.TryToGetClipAtSeconds(mousePointSeconds, out TimelineClip? clip, layer.LayerID))
                        {
                            isDraggingClip = true;
                            pressedClip = clip;

                            double hitTolerance = 10 / PixelsPerSecond;

                            if (Math.Abs(mousePointSeconds - pressedClip.StartSecondsOnTimeline) <= hitTolerance)
                            {
                                currentClipDragMode = ClipDragMode.TrimLeft;
                            }
                            else if (Math.Abs(mousePointSeconds - pressedClip.EndSecondsOnTimeline) <= hitTolerance)
                            {
                                currentClipDragMode = ClipDragMode.TrimRight;
                            }
                            else
                            {
                                currentClipDragMode = ClipDragMode.Move;
                            }
                        }
                    }
                }
                else
                {
                    PlayheadSeconds = mousePointSeconds;
                }

                dragStartScreenPoint = this.PointToScreen(mousePoint);

                this.CaptureMouse();
                this.Cursor = Cursors.None;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                Point mousePoint = e.GetPosition(this);
                double mousePointSeconds = (mousePoint.X + ScrollOffset) / PixelsPerSecond;

                int pressedLayer = (int)((mousePoint.Y - TimelineTopHeight) / LayerHeight);

                if (pressedLayer < MainWindow.LayersPanel.Children.Count)
                {
                    UC_LayerControl layerControl = (UC_LayerControl)(MainWindow.LayersPanel.Children[pressedLayer]);
                    TimelineLayer layer = TimelineData.Layers[layerControl.LayerID];

                    if (TimelineData.TryToGetClipAtSeconds(mousePointSeconds, out TimelineClip? clip, layer.LayerID))
                    {
                        pressedClip = clip;
                        TimelineData.Layers[pressedClip.LayerID].Clips.Remove(pressedClip);
                        TimelineData.Clips.Remove(pressedClip);
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.IsMouseOver)
                InvalidateVisual();

            if (isDraggingPlayhead)
            {
                if (ignoreNextMouseMove)
                {
                    ignoreNextMouseMove = false;
                    return;
                }

                Point currentScreenPos = this.PointToScreen(e.GetPosition(this));

                double deltaX = currentScreenPos.X - dragStartScreenPoint.X;
                PlayheadSeconds += deltaX / PixelsPerSecond;
                double playheadX = (PlayheadSeconds * PixelsPerSecond) - ScrollOffset;

                if (PlayheadSeconds < 0)
                    PlayheadSeconds = 0;

                if (Math.Abs(deltaX) > 0)
                {
                    if (playheadX > ActualWidth || playheadX < 0)
                    {
                        ScrollOffset += deltaX;
                        if (ScrollOffset < 0) ScrollOffset = 0;
                    }

                    ignoreNextMouseMove = true;
                    SetCursorPos((int)dragStartScreenPoint.X, (int)dragStartScreenPoint.Y);

                    InvalidateVisual();
                }
            }
            else if (isDraggingClip && pressedClip != null)
            {
                if (ignoreNextMouseMove)
                {
                    ignoreNextMouseMove = false;
                    return;
                }

                Point currentScreenPos = this.PointToScreen(e.GetPosition(this));
                double deltaX = currentScreenPos.X - dragStartScreenPoint.X;

                if (Math.Abs(deltaX) > 0)
                {
                    double deltaSeconds = deltaX / PixelsPerSecond;
                    double minDurationSeconds = 1;

                    if (currentClipDragMode == ClipDragMode.TrimLeft)
                    {
                        double newStart = pressedClip.StartSecondsOnTimeline + deltaSeconds;

                        if (newStart < 0) newStart = 0;

                        if (newStart > pressedClip.EndSecondsOnTimeline - minDurationSeconds)
                            newStart = pressedClip.EndSecondsOnTimeline - minDurationSeconds;

                        if (pressedClip.FileData?.Type == FileType.Video)
                        {
                            pressedClip.VideoStartSecond += newStart - pressedClip.StartSecondsOnTimeline;
                            if (pressedClip.VideoStartSecond < 0)
                            {
                                double amount = -pressedClip.VideoStartSecond;
                                newStart += amount;
                                pressedClip.VideoStartSecond = 0;
                            }
                        }

                        pressedClip.StartSecondsOnTimeline = newStart;
                        pressedClip.ClipDuration = pressedClip.EndSecondsOnTimeline - pressedClip.StartSecondsOnTimeline;
                    }
                    else if (currentClipDragMode == ClipDragMode.TrimRight)
                    {
                        double newEnd = pressedClip.EndSecondsOnTimeline + deltaSeconds;

                        if (newEnd < pressedClip.StartSecondsOnTimeline + minDurationSeconds)
                            newEnd = pressedClip.StartSecondsOnTimeline + minDurationSeconds;

                        if (pressedClip.FileData?.Type == FileType.Video)
                        {
                            if (newEnd > pressedClip.StartSecondsOnTimeline + pressedClip.VideoDuration)
                                newEnd = pressedClip.StartSecondsOnTimeline + pressedClip.VideoDuration;
                        }


                        pressedClip.EndSecondsOnTimeline = newEnd;
                        pressedClip.ClipDuration = pressedClip.EndSecondsOnTimeline - pressedClip.StartSecondsOnTimeline;
                    }
                    else if (currentClipDragMode == ClipDragMode.Move)
                    {
                        double newStart = pressedClip.StartSecondsOnTimeline + deltaSeconds;

                        if (newStart < 0) newStart = 0;

                        pressedClip.StartSecondsOnTimeline = newStart;
                        pressedClip.EndSecondsOnTimeline = newStart + pressedClip.ClipDuration;
                    }

                    ignoreNextMouseMove = true;
                    SetCursorPos((int)dragStartScreenPoint.X, (int)dragStartScreenPoint.Y);
                    InvalidateVisual();
                }
            }
            else if (isDragging)
            {
                if (ignoreNextMouseMove)
                {
                    ignoreNextMouseMove = false;
                    return;
                }

                Point currentScreenPos = this.PointToScreen(e.GetPosition(this));

                double deltaX = currentScreenPos.X - dragStartScreenPoint.X;

                if (Math.Abs(deltaX) > 0)
                {
                    ScrollOffset -= deltaX;
                    if (ScrollOffset < 0) ScrollOffset = 0;

                    ignoreNextMouseMove = true;
                    SetCursorPos((int)dragStartScreenPoint.X, (int)dragStartScreenPoint.Y);

                    InvalidateVisual();
                }
            }

            previewPlayer.UpdatePreview(TimelineData, PlayheadSeconds);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            isDragging = false;
            isDraggingPlayhead = false;
            isDraggingClip = false;
            pressedClip = null;
            this.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            PixelsPerSecond = Math.Clamp(PixelsPerSecond * zoomFactor, 0.1, 1000);

            InvalidateVisual();
        }
    }
}