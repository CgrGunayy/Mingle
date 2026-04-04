using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MingleWPF
{
    public class TimelineControl : FrameworkElement
    {
        private readonly SolidColorBrush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E0E0E"));
        private readonly Pen _tickPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70594043")), 1);
        private readonly Pen _playheadPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA")), 2);
        private readonly SolidColorBrush _playheadRectBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB2BA"));
        private readonly SolidColorBrush _textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70E0BEC1"));

        public double PixelsPerSecond { get; set; } = 50;
        public double PlayheadPositionSeconds { get; set; } = 2.5;

        public void Redraw()
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            Rect bounds = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            dc.DrawRectangle(_backgroundBrush, null, bounds);

            int visibleSeconds = (int)(this.ActualWidth / PixelsPerSecond) + 1;

            for (int i = 0; i <= visibleSeconds; i++)
            {
                double xPos = i * PixelsPerSecond;

                dc.DrawLine(_tickPen, new Point(xPos, 15), new Point(xPos, 27));

                TimeSpan time = TimeSpan.FromSeconds(i);
                string timeString = time.ToString(@"mm\:ss");

                FormattedText text = new FormattedText(
                    timeString,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    10,
                    _textBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(text, new Point(xPos + 5, 15));
            }


            double playheadX = PlayheadPositionSeconds * PixelsPerSecond;
            dc.DrawLine(_playheadPen, new Point(playheadX, 0), new Point(playheadX, this.ActualHeight));

            Rect playheadRect = new Rect(playheadX - 5, 0, 10, 10);
            dc.DrawRectangle(_playheadRectBrush, null, playheadRect);
        }
    }
}