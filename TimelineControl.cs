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
        private readonly SolidColorBrush _playheadRectBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA"));
        private readonly SolidColorBrush _textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70E0BEC1"));

        SolidColorBrush clipBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A4133C"));
        Pen clipBorderPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35A4133C")), 1);

        public double PixelsPerSecond { get; set; } = 100;
        public double ScrollOffset { get; set; } = 0;
        public double PlayheadSeconds { get; set; } = 0;

        private bool isDragging;
        private bool isDraggingPlayhead;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        private Point dragStartScreenPoint;
        private bool ignoreNextMouseMove;

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
            int stepSeconds = 1;

            if (PixelsPerSecond * 1 < minPixelGap) stepSeconds = 2;
            if (PixelsPerSecond * 2 < minPixelGap) stepSeconds = 5;
            if (PixelsPerSecond * 5 < minPixelGap) stepSeconds = 10;
            if (PixelsPerSecond * 10 < minPixelGap) stepSeconds = 30;
            if (PixelsPerSecond * 30 < minPixelGap) stepSeconds = 60;
            if (PixelsPerSecond * 60 < minPixelGap) stepSeconds = 300;
            if (PixelsPerSecond * 300 < minPixelGap) stepSeconds = 600;
            if (PixelsPerSecond * 600 < minPixelGap) stepSeconds = 1800;
            if (PixelsPerSecond * 1800 < minPixelGap) stepSeconds = 3600;
            if (PixelsPerSecond * 3600 < minPixelGap) stepSeconds = 18000;

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

            foreach (var clip in TimelineData.Clips)
            {
                double startX = (clip.ClipStartSecond * PixelsPerSecond) - ScrollOffset;
                double width = clip.ClipDuration * PixelsPerSecond;

                Rect clipRect = new Rect(startX, 30, width, 40);

                if (clipRect.Right > 0 && clipRect.Left < ActualWidth)
                {
                    dc.DrawRectangle(clipBrush, clipBorderPen, clipRect);

                    FormattedText clipText = new FormattedText(
                        clip.ClipTitle,
                        System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Consolas"), 10, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    dc.DrawText(clipText, new Point(startX + 5, 42));
                }
            }

            double playheadX = (PlayheadSeconds * PixelsPerSecond) - ScrollOffset;
            if (playheadX >= 0 && playheadX <= ActualWidth)
            {
                dc.DrawLine(_playheadPen, new Point(playheadX, 0), new Point(playheadX, ActualHeight));

                Rect playheadRect = new Rect(playheadX - 5, 0, 10, 10);
                dc.DrawRectangle(_playheadRectBrush, null, playheadRect);
            }
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

                try
                {
                    TimelineClip clip = new TimelineClip();
                    clip.ClipTitle = file.Name;
                    clip.ClipStartSecond = dropSecond;
                    clip.FileData = file;

                    switch (file.Type)
                    {
                        case FileType.Video:
                            IMediaAnalysis videoInfo = await FFProbe.AnalyseAsync(file.Path);
                            clip.ClipEndSecond = clip.ClipStartSecond + videoInfo.Duration.TotalSeconds;
                            break;
                        case FileType.Image:
                            clip.ClipEndSecond = clip.ClipStartSecond + 5.0;
                            break;
                    }

                    clip.ClipDuration = clip.ClipEndSecond - clip.ClipStartSecond;

                    TimelineData.AddClip(clip);

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

                isDragging = true;
                if (playheadRect.Contains(e.GetPosition(this)))
                    isDraggingPlayhead = true;

                dragStartScreenPoint = this.PointToScreen(e.GetPosition(this));

                this.CaptureMouse();
                this.Cursor = Cursors.None;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
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
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            isDragging = false;
            isDraggingPlayhead = false;
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