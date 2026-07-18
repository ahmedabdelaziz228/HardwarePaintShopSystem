using HardwarePaintShop.Application.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace HardwarePaintShop.Desktop.Controls;

/// <summary>
/// Lightweight dashboard line chart. It keeps the desktop app free from charting packages.
/// </summary>
public sealed class WeeklyTrendChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable<DashboardTrendPoint>),
        typeof(WeeklyTrendChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<DashboardTrendPoint>? ItemsSource
    {
        get => (IEnumerable<DashboardTrendPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var items = ItemsSource?.OrderBy(i => i.Date).ToList() ?? new List<DashboardTrendPoint>();
        var width = Math.Max(0, ActualWidth);
        var height = Math.Max(0, ActualHeight);
        if (width < 160 || height < 100)
            return;

        const double rightMargin = 58;
        const double leftMargin = 18;
        const double topMargin = 14;
        const double bottomMargin = 34;
        var plotWidth = width - rightMargin - leftMargin;
        var plotHeight = height - topMargin - bottomMargin;
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(226, 232, 240)), 1)
        {
            DashStyle = DashStyles.Dash
        };
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");

        var maximum = items.Count == 0
            ? 1d
            : (double)Math.Max(items.Max(i => i.Sales), items.Max(i => i.Expenses));
        maximum = maximum <= 0 ? 1 : maximum * 1.12;

        for (var index = 0; index <= 4; index++)
        {
            var y = topMargin + plotHeight * index / 4;
            drawingContext.DrawLine(gridPen, new Point(leftMargin, y), new Point(width - rightMargin, y));
            var labelValue = maximum * (4 - index) / 4;
            DrawText(
                drawingContext,
                FormatCompact(labelValue),
                new Point(width - rightMargin + 7, y - 8),
                10,
                new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                typeface,
                dpi,
                FlowDirection.RightToLeft);
        }

        if (items.Count == 0)
        {
            DrawCenteredText(drawingContext, "لا توجد حركة خلال هذا الأسبوع", width / 2, height / 2,
                12, new SolidColorBrush(Color.FromRgb(148, 163, 184)), typeface, dpi);
            return;
        }

        var salesPoints = BuildPoints(items.Select(i => i.Sales).ToList(), maximum, width, plotWidth, plotHeight,
            rightMargin, topMargin);
        var expensePoints = BuildPoints(items.Select(i => i.Expenses).ToList(), maximum, width, plotWidth, plotHeight,
            rightMargin, topMargin);

        DrawArea(drawingContext, salesPoints, topMargin + plotHeight,
            new SolidColorBrush(Color.FromArgb(24, 37, 99, 235)));
        DrawSeries(drawingContext, salesPoints, Color.FromRgb(37, 99, 235));
        DrawSeries(drawingContext, expensePoints, Color.FromRgb(249, 115, 22));

        for (var index = 0; index < items.Count; index++)
        {
            DrawCenteredText(
                drawingContext,
                GetArabicDayName(items[index].Date.DayOfWeek),
                salesPoints[index].X,
                height - bottomMargin + 10,
                10,
                new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                typeface,
                dpi);
        }
    }

    private static List<Point> BuildPoints(
        IReadOnlyList<decimal> values,
        double maximum,
        double width,
        double plotWidth,
        double plotHeight,
        double rightMargin,
        double topMargin)
    {
        var result = new List<Point>(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var ratio = values.Count == 1 ? 0.5 : (double)index / (values.Count - 1);
            var x = width - rightMargin - ratio * plotWidth;
            var y = topMargin + plotHeight - (double)values[index] / maximum * plotHeight;
            result.Add(new Point(x, y));
        }

        return result;
    }

    private static void DrawSeries(DrawingContext drawingContext, IReadOnlyList<Point> points, Color color)
    {
        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, 2.2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        for (var index = 1; index < points.Count; index++)
            drawingContext.DrawLine(pen, points[index - 1], points[index]);

        foreach (var point in points)
        {
            drawingContext.DrawEllipse(Brushes.White, new Pen(brush, 2), point, 3.4, 3.4);
        }
    }

    private static void DrawArea(DrawingContext drawingContext, IReadOnlyList<Point> points, double baseline, Brush brush)
    {
        if (points.Count < 2)
            return;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(points[0].X, baseline), true, true);
            context.LineTo(points[0], true, false);
            for (var index = 1; index < points.Count; index++)
                context.LineTo(points[index], true, false);
            context.LineTo(new Point(points[^1].X, baseline), true, false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(brush, null, geometry);
    }

    private static string FormatCompact(double value)
    {
        if (value >= 1_000_000)
            return $"{value / 1_000_000:0.#} م";
        if (value >= 1_000)
            return $"{value / 1_000:0.#} أ";
        return value.ToString("0", CultureInfo.CurrentCulture);
    }

    private static string GetArabicDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "السبت",
        DayOfWeek.Sunday => "الأحد",
        DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        DayOfWeek.Thursday => "الخميس",
        _ => "الجمعة"
    };

    private static void DrawCenteredText(
        DrawingContext drawingContext,
        string text,
        double centerX,
        double y,
        double fontSize,
        Brush brush,
        Typeface typeface,
        double dpi)
    {
        var formatted = CreateText(text, fontSize, brush, typeface, dpi, FlowDirection.RightToLeft);
        drawingContext.DrawText(formatted, new Point(centerX - formatted.Width / 2, y));
    }

    private static void DrawText(
        DrawingContext drawingContext,
        string text,
        Point point,
        double fontSize,
        Brush brush,
        Typeface typeface,
        double dpi,
        FlowDirection direction)
        => drawingContext.DrawText(CreateText(text, fontSize, brush, typeface, dpi, direction), point);

    private static FormattedText CreateText(
        string text,
        double fontSize,
        Brush brush,
        Typeface typeface,
        double dpi,
        FlowDirection direction)
        => new(
            text,
            CultureInfo.GetCultureInfo("ar-EG"),
            direction,
            typeface,
            fontSize,
            brush,
            dpi);
}
