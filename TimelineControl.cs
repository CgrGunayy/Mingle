using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace MingleWPF
{
    public class TimelineControl : FrameworkElement
    {
        private readonly SolidColorBrush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E0E0E"));
        private readonly Pen _tickPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70594043")), 1);
        private readonly Pen _playheadPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA")), 2);
        private readonly SolidColorBrush _playheadRectBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA"));
        private readonly SolidColorBrush _textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70E0BEC1"));

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

        private double debugDelta;

        public TimelineControl()
        {
            this.ClipToBounds = true;
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

            double playheadX = (PlayheadSeconds * PixelsPerSecond) - ScrollOffset;
            if (playheadX >= 0 && playheadX <= ActualWidth)
            {
                dc.DrawLine(_playheadPen, new Point(playheadX, 0), new Point(playheadX, ActualHeight));

                Rect playheadRect = new Rect(playheadX - 5, 0, 10, 10);
                dc.DrawRectangle(_playheadRectBrush, null, playheadRect);
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

                debugDelta = playheadX;
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