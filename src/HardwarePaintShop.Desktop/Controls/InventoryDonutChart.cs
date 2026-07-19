using HardwarePaintShop.Application.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace HardwarePaintShop.Desktop.Controls;

/// <summary>
/// Draws the inventory value distribution as a compact donut chart.
/// </summary>
public sealed class InventoryDonutChart : FrameworkElement
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(37, 99, 235),
        Color.FromRgb(22, 163, 74),
        Color.FromRgb(249, 115, 22),
        Color.FromRgb(124, 58, 237)
    };

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable<InventoryCategorySummary>),
        typeof(InventoryDonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TotalValueProperty = DependencyProperty.Register(
        nameof(TotalValue),
        typeof(decimal),
        typeof(InventoryDonutChart),
        new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<InventoryCategorySummary>? ItemsSource
    {
        get => (IEnumerable<InventoryCategorySummary>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public decimal TotalValue
    {
        get => (decimal)GetValue(TotalValueProperty);
        set => SetValue(TotalValueProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = Math.Max(0, ActualWidth);
        var height = Math.Max(0, ActualHeight);
        if (width < 80 || height < 80)
            return;

        var center = new Point(width / 2, height / 2);
        var strokeWidth = Math.Clamp(Math.Min(width, height) * 0.15, 18, 30);
        var radius = Math.Max(10, Math.Min(width, height) / 2 - strokeWidth / 2 - 3);
        var items = ItemsSource?.Where(i => i.Percentage > 0).Take(Palette.Length).ToList()
                    ?? new List<InventoryCategorySummary>();

        drawingContext.DrawEllipse(null,
            new Pen(new SolidColorBrush(Color.FromRgb(241, 245, 249)), strokeWidth),
            center, radius, radius);

        var startAngle = -90d;
        for (var index = 0; index < items.Count; index++)
        {
            var sweep = Math.Min(360, (double)items[index].Percentage / 100 * 360);
            if (sweep >= 359.5)
            {
                drawingContext.DrawEllipse(null, new Pen(new SolidColorBrush(Palette[index]), strokeWidth),
                    center, radius, radius);
            }
            else if (sweep > 1)
            {
                DrawArc(drawingContext, center, radius, startAngle + 1, Math.Max(0.5, sweep - 2),
                    new Pen(new SolidColorBrush(Palette[index]), strokeWidth));
            }
            startAngle += sweep;
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        var valueText = FormatCompact((double)TotalValue);
        DrawCenteredText(drawingContext, valueText, center.X, center.Y - 16, 19,
            new SolidColorBrush(Color.FromRgb(15, 23, 42)), typeface, dpi, FontWeights.Bold);
        DrawCenteredText(drawingContext, "قيمة المخزون", center.X, center.Y + 8, 10,
            new SolidColorBrush(Color.FromRgb(100, 116, 139)), typeface, dpi, FontWeights.Normal);
    }

    private static void DrawArc(
        DrawingContext drawingContext,
        Point center,
        double radius,
        double startAngle,
        double sweepAngle,
        Pen pen)
    {
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweepAngle);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, sweepAngle > 180,
                SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static string FormatCompact(double value)
    {
        if (value >= 1_000_000)
            return $"{value / 1_000_000:0.##} مليون";
        if (value >= 1_000)
            return $"{value / 1_000:0.#} ألف";
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static void DrawCenteredText(
        DrawingContext drawingContext,
        string text,
        double centerX,
        double y,
        double fontSize,
        Brush brush,
        Typeface typeface,
        double dpi,
        FontWeight weight)
    {
        var weightedTypeface = new Typeface(typeface.FontFamily, FontStyles.Normal, weight, FontStretches.Normal);
        var formatted = new FormattedText(
            text,
            CultureInfo.GetCultureInfo("ar-EG"),
            FlowDirection.RightToLeft,
            weightedTypeface,
            fontSize,
            brush,
            dpi);
        drawingContext.DrawText(formatted, new Point(centerX - formatted.Width / 2, y));
    }
}
