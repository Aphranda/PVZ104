using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace RZDemoWpf
{
    public sealed class CalibrationPlot : FrameworkElement
    {
        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(
                nameof(Series),
                typeof(IEnumerable<CalibrationPlotSeries>),
                typeof(CalibrationPlot),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty XAxisTitleProperty =
            DependencyProperty.Register(
                nameof(XAxisTitle),
                typeof(string),
                typeof(CalibrationPlot),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty YAxisTitleProperty =
            DependencyProperty.Register(
                nameof(YAxisTitle),
                typeof(string),
                typeof(CalibrationPlot),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(
                nameof(EmptyText),
                typeof(string),
                typeof(CalibrationPlot),
                new FrameworkPropertyMetadata("暂无数据", FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable<CalibrationPlotSeries> Series
        {
            get { return (IEnumerable<CalibrationPlotSeries>)GetValue(SeriesProperty); }
            set { SetValue(SeriesProperty, value); }
        }

        public string XAxisTitle
        {
            get { return (string)GetValue(XAxisTitleProperty); }
            set { SetValue(XAxisTitleProperty, value); }
        }

        public string YAxisTitle
        {
            get { return (string)GetValue(YAxisTitleProperty); }
            set { SetValue(YAxisTitleProperty, value); }
        }

        public string EmptyText
        {
            get { return (string)GetValue(EmptyTextProperty); }
            set { SetValue(EmptyTextProperty, value); }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.DrawRectangle(Brushes.White, new Pen(new SolidColorBrush(Color.FromRgb(215, 221, 229)), 1), bounds);

            Rect plot = new Rect(54, 18, Math.Max(1, ActualWidth - 72), Math.Max(1, ActualHeight - 50));
            Pen gridPen = new Pen(new SolidColorBrush(Color.FromRgb(231, 236, 242)), 1);
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromRgb(102, 112, 133)), 1);

            for (int i = 0; i <= 4; i++)
            {
                double x = plot.Left + plot.Width * i / 4.0;
                double y = plot.Top + plot.Height * i / 4.0;
                dc.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
                dc.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            }

            dc.DrawLine(axisPen, new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));
            dc.DrawLine(axisPen, new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom));

            List<CalibrationPlotSeries> activeSeries = (Series ?? Enumerable.Empty<CalibrationPlotSeries>())
                .Where(item => item != null && item.Points != null && item.Points.Length > 0)
                .ToList();

            if (activeSeries.Count == 0)
            {
                DrawText(dc, EmptyText, 13, Brushes.Gray, new Point(plot.Left + plot.Width / 2 - 28, plot.Top + plot.Height / 2 - 10), pixelsPerDip);
                DrawAxisTitles(dc, plot, pixelsPerDip);
                return;
            }

            double minX = activeSeries.Min(item => item.Points.Min(point => point.X));
            double maxX = activeSeries.Max(item => item.Points.Max(point => point.X));
            double minY = activeSeries.Min(item => item.Points.Min(point => point.Y));
            double maxY = activeSeries.Max(item => item.Points.Max(point => point.Y));
            ExpandRange(ref minX, ref maxX);
            ExpandRange(ref minY, ref maxY);

            DrawText(dc, FormatValue(maxY), 11, Brushes.Gray, new Point(6, plot.Top - 6), pixelsPerDip);
            DrawText(dc, FormatValue(minY), 11, Brushes.Gray, new Point(6, plot.Bottom - 14), pixelsPerDip);
            DrawText(dc, FormatValue(minX), 11, Brushes.Gray, new Point(plot.Left, plot.Bottom + 4), pixelsPerDip);
            DrawText(dc, FormatValue(maxX), 11, Brushes.Gray, new Point(plot.Right - 44, plot.Bottom + 4), pixelsPerDip);

            foreach (CalibrationPlotSeries series in activeSeries)
            {
                Pen pen = new Pen(series.Stroke ?? Brushes.SteelBlue, series.StrokeThickness <= 0 ? 1.6 : series.StrokeThickness);
                Point? previous = null;
                foreach (Point point in series.Points)
                {
                    Point mapped = Map(point, plot, minX, maxX, minY, maxY);
                    if (previous.HasValue)
                    {
                        dc.DrawLine(pen, previous.Value, mapped);
                    }
                    else
                    {
                        dc.DrawEllipse(series.Stroke ?? Brushes.SteelBlue, null, mapped, 2.2, 2.2);
                    }

                    previous = mapped;
                }
            }

            DrawLegend(dc, activeSeries, plot, pixelsPerDip);
            DrawAxisTitles(dc, plot, pixelsPerDip);
        }

        private void DrawAxisTitles(DrawingContext dc, Rect plot, double pixelsPerDip)
        {
            if (!string.IsNullOrWhiteSpace(XAxisTitle))
            {
                DrawText(dc, XAxisTitle, 11, Brushes.Gray, new Point(plot.Left + plot.Width / 2 - 24, plot.Bottom + 22), pixelsPerDip);
            }

            if (!string.IsNullOrWhiteSpace(YAxisTitle))
            {
                DrawText(dc, YAxisTitle, 11, Brushes.Gray, new Point(6, 4), pixelsPerDip);
            }
        }

        private static void DrawLegend(DrawingContext dc, List<CalibrationPlotSeries> series, Rect plot, double pixelsPerDip)
        {
            double x = plot.Left + 8;
            double y = plot.Top + 8;
            foreach (CalibrationPlotSeries item in series.Take(4))
            {
                Brush brush = item.Stroke ?? Brushes.SteelBlue;
                dc.DrawRectangle(brush, null, new Rect(x, y + 4, 14, 3));
                DrawText(dc, item.Name, 11, Brushes.DimGray, new Point(x + 20, y - 2), pixelsPerDip);
                x += 96;
            }
        }

        private static void DrawText(DrawingContext dc, string text, double size, Brush brush, Point point, double pixelsPerDip)
        {
            FormattedText formatted = new FormattedText(
                text ?? "",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei UI"),
                size,
                brush,
                pixelsPerDip);
            dc.DrawText(formatted, point);
        }

        private static Point Map(Point point, Rect plot, double minX, double maxX, double minY, double maxY)
        {
            double x = plot.Left + (point.X - minX) / (maxX - minX) * plot.Width;
            double y = plot.Bottom - (point.Y - minY) / (maxY - minY) * plot.Height;
            return new Point(x, y);
        }

        private static void ExpandRange(ref double min, ref double max)
        {
            if (Math.Abs(max - min) < 0.000000001)
            {
                min -= 1;
                max += 1;
                return;
            }

            double pad = (max - min) * 0.08;
            min -= pad;
            max += pad;
        }

        private static string FormatValue(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    public sealed class CalibrationPlotSeries
    {
        public string Name { get; set; }
        public Point[] Points { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }
    }
}
