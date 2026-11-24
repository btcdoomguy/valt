using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.PriceHistory;

public sealed class BitcoinCycleSpiralChartControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<SpiralDataPoint>?> PointsProperty =
        AvaloniaProperty.Register<BitcoinCycleSpiralChartControl, IReadOnlyList<SpiralDataPoint>?>(nameof(Points));

    public IReadOnlyList<SpiralDataPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    // Static immutable drawing resources per D-05 matching Valt's dark theme and PriceHistoryChartData palette
    private static readonly IImmutableSolidColorBrush BtcPrimaryBrush = new ImmutableSolidColorBrush(Color.Parse("#ffa122"));
    private static readonly IImmutableSolidColorBrush BtcLightBrush = new ImmutableSolidColorBrush(Color.Parse("#ffcc88"));
    private static readonly IImmutableSolidColorBrush BtcDarkBrush = new ImmutableSolidColorBrush(Color.Parse("#e98805"));
    private static readonly IImmutableSolidColorBrush GridBrush = new ImmutableSolidColorBrush(Color.Parse("#4d4d4d"), 0.55);
    private static readonly IImmutableSolidColorBrush OuterRingBrush = new ImmutableSolidColorBrush(Color.Parse("#a8a6a4"), 0.85);
    private static readonly IImmutableSolidColorBrush AxisLineBrush = new ImmutableSolidColorBrush(Color.Parse("#a8a6a4"), 0.9);
    private static readonly IImmutableSolidColorBrush TextBrush = new ImmutableSolidColorBrush(Color.Parse("#cfccc9"));
    private static readonly IImmutableSolidColorBrush MutedTextBrush = new ImmutableSolidColorBrush(Color.Parse("#a8a6a4"));
    private static readonly IImmutableSolidColorBrush HalvingFillBrush = new ImmutableSolidColorBrush(Color.Parse("#0566e9"));
    private static readonly IImmutableSolidColorBrush HalvingStrokeBrush = new ImmutableSolidColorBrush(Color.Parse("#ffffff"));
    private static readonly IImmutableSolidColorBrush TooltipBgBrush = new ImmutableSolidColorBrush(Color.Parse("#1a1a1a"), 0.94);
    private static readonly IImmutableSolidColorBrush TooltipBorderBrush = new ImmutableSolidColorBrush(Color.Parse("#ffa122"), 0.8);

    private static readonly IPen GridRingPen = new ImmutablePen(GridBrush, 1.0);
    private static readonly IPen OuterRingPen = new ImmutablePen(OuterRingBrush, 1.25);
    private static readonly IPen SpokePen = new ImmutablePen(GridBrush, 1.0);
    private static readonly IPen PriceAxisPen = new ImmutablePen(AxisLineBrush, 1.5);
    private static readonly IPen SpiralPen = new ImmutablePen(
        BtcPrimaryBrush,
        2.4,
        lineCap: PenLineCap.Round,
        lineJoin: PenLineJoin.Round);
    private static readonly IPen HalvingPen = new ImmutablePen(HalvingStrokeBrush, 1.5);
    private static readonly IPen LatestPointPen = new ImmutablePen(BtcDarkBrush, 1.5);
    private static readonly IPen HoverMarkerPen = new ImmutablePen(HalvingStrokeBrush, 2.0);
    private static readonly IPen TooltipBorderPen = new ImmutablePen(TooltipBorderBrush, 1.0);

    private Size _cachedSize;
    private IReadOnlyList<SpiralDataPoint>? _cachedSource;
    private StreamGeometry? _cachedSpiralGeometry;
    private readonly List<SpiralScreenPoint> _screenPoints = new();
    private readonly List<SpiralScreenPoint> _halvingScreenPoints = new();
    private SpiralScreenPoint? _hoveredPoint;

    static BitcoinCycleSpiralChartControl()
    {
        AffectsRender<BitcoinCycleSpiralChartControl>(PointsProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PointsProperty)
        {
            _cachedSpiralGeometry = null;
            _hoveredPoint = null;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        var nearest = BitcoinCycleSpiralCalculator.FindNearestPoint(_screenPoints, pos, maxDistancePixels: 20.0);
        if (!ReferenceEquals(nearest, _hoveredPoint))
        {
            _hoveredPoint = nearest;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoveredPoint is not null)
        {
            _hoveredPoint = null;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        // Fill transparent hit-test area so PointerMoved events fire across entire surface
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        var maxRadius = Math.Max(40.0, Math.Min(Bounds.Width, Bounds.Height) / 2.0 - 44.0);
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Medium);
        var boldTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

        EnsureGeometryCache(center, maxRadius);

        DrawConcentricLogRings(context, center, maxRadius);
        DrawRadialSpokesAndOuterTicks(context, center, maxRadius);
        DrawCardinalYearLabels(context, center, maxRadius, boldTypeface);
        DrawHorizontalPriceAxisAndRotatedLabels(context, center, maxRadius, typeface);
        DrawSpiralTrajectoryAndMilestones(context, center);
        DrawLegend(context, typeface);

        if (_hoveredPoint is not null)
        {
            DrawHoverOverlayAndTooltip(context, _hoveredPoint, typeface, boldTypeface);
        }
    }

    private void EnsureGeometryCache(Point center, double maxRadius)
    {
        var points = Points;
        if (_cachedSpiralGeometry is not null &&
            _cachedSize == Bounds.Size &&
            ReferenceEquals(_cachedSource, points))
        {
            return;
        }

        _cachedSize = Bounds.Size;
        _cachedSource = points;
        _screenPoints.Clear();
        _halvingScreenPoints.Clear();
        _cachedSpiralGeometry = null;

        if (points is null || points.Count == 0)
            return;

        var geometry = new StreamGeometry();
        using (var geoCtx = geometry.Open())
        {
            for (var i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                var screenPos = BitcoinCycleSpiralCalculator.PolarToCartesian(
                    center,
                    maxRadius,
                    pt.AngleRadians,
                    pt.NormalizedRadius);

                var screenPt = new SpiralScreenPoint(screenPos, pt);
                _screenPoints.Add(screenPt);

                if (pt.HalvingNumber.HasValue)
                {
                    _halvingScreenPoints.Add(screenPt);
                }

                if (i == 0)
                    geoCtx.BeginFigure(screenPos, isFilled: false);
                else
                    geoCtx.LineTo(screenPos);
            }

            geoCtx.EndFigure(isClosed: false);
        }

        _cachedSpiralGeometry = geometry;
    }

    private static void DrawConcentricLogRings(DrawingContext context, Point center, double maxRadius)
    {
        var rings = BitcoinCycleSpiralCalculator.PriceRingDefinitions;
        for (var k = 1; k < rings.Count; k++)
        {
            var normR = (rings[k].Log10Value - BitcoinCycleSpiralCalculator.MinLog10Price)
                        / BitcoinCycleSpiralCalculator.Log10PriceSpan;
            var radius = normR * maxRadius;
            var pen = k == rings.Count - 1 ? OuterRingPen : GridRingPen;
            context.DrawEllipse(null, pen, center, radius, radius);
        }

        // Center origin dot ($0.001 at r = 0)
        context.DrawEllipse(BtcPrimaryBrush, null, center, 2.5, 2.5);
    }

    private static void DrawRadialSpokesAndOuterTicks(DrawingContext context, Point center, double maxRadius)
    {
        for (var m = 0; m < 8; m++)
        {
            var angle = -Math.PI / 2.0 + m * (Math.PI / 4.0);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var outerPoint = new Point(center.X + maxRadius * cos, center.Y + maxRadius * sin);
            context.DrawLine(SpokePen, center, outerPoint);

            var isCardinal = m % 2 == 0;
            var tickLength = isCardinal ? 6.0 : 8.0;
            var tickEnd = new Point(
                center.X + (maxRadius + tickLength) * cos,
                center.Y + (maxRadius + tickLength) * sin);
            context.DrawLine(OuterRingPen, outerPoint, tickEnd);
        }
    }

    private static void DrawCardinalYearLabels(
        DrawingContext context,
        Point center,
        double maxRadius,
        Typeface boldTypeface)
    {
        var cardinalLabels = BitcoinCycleSpiralCalculator.CardinalYearLabels;

        // Top (12 o'clock)
        var topFt = CreateText(cardinalLabels[0].Label, boldTypeface, 11.5, TextBrush);
        context.DrawText(
            topFt,
            new Point(center.X - topFt.Width / 2.0, center.Y - maxRadius - 10.0 - topFt.Height));

        // Right (3 o'clock)
        var rightFt = CreateText(cardinalLabels[1].Label, boldTypeface, 11.5, TextBrush);
        context.DrawText(
            rightFt,
            new Point(center.X + maxRadius + 10.0, center.Y - rightFt.Height / 2.0));

        // Bottom (6 o'clock)
        var bottomFt = CreateText(cardinalLabels[2].Label, boldTypeface, 11.5, TextBrush);
        context.DrawText(
            bottomFt,
            new Point(center.X - bottomFt.Width / 2.0, center.Y + maxRadius + 10.0));

        // Left (9 o'clock)
        var leftFt = CreateText(cardinalLabels[3].Label, boldTypeface, 11.5, TextBrush);
        context.DrawText(
            leftFt,
            new Point(center.X - maxRadius - 10.0 - leftFt.Width, center.Y - leftFt.Height / 2.0));
    }

    private static void DrawHorizontalPriceAxisAndRotatedLabels(
        DrawingContext context,
        Point center,
        double maxRadius,
        Typeface typeface)
    {
        context.DrawLine(PriceAxisPen, center, new Point(center.X + maxRadius, center.Y));

        var rings = BitcoinCycleSpiralCalculator.PriceRingDefinitions;
        for (var k = 0; k < rings.Count; k++)
        {
            var ring = rings[k];
            var normR = (ring.Log10Value - BitcoinCycleSpiralCalculator.MinLog10Price)
                        / BitcoinCycleSpiralCalculator.Log10PriceSpan;
            var tickX = center.X + normR * maxRadius;
            var tickY = center.Y;

            context.DrawLine(PriceAxisPen, new Point(tickX, tickY), new Point(tickX, tickY + 5.0));

            var ft = CreateText(ring.Label, typeface, 10.0, MutedTextBrush);
            var transform = Matrix.CreateTranslation(0, -ft.Height / 2.0)
                            * Matrix.CreateRotation(Math.PI / 2.0)
                            * Matrix.CreateTranslation(tickX, tickY + 7.0);

            using (context.PushTransform(transform))
            {
                context.DrawText(ft, new Point(0, 0));
            }
        }
    }

    private void DrawSpiralTrajectoryAndMilestones(DrawingContext context, Point center)
    {
        if (_cachedSpiralGeometry is not null)
        {
            context.DrawGeometry(null, SpiralPen, _cachedSpiralGeometry);
        }

        if (_screenPoints.Count > 0)
        {
            // Connect center origin ($0.001) to first recorded point with a subtle lead-in line
            context.DrawLine(SpokePen, center, _screenPoints[0].Position);

            // Latest price endpoint marker
            var latest = _screenPoints[^1];
            context.DrawEllipse(BtcLightBrush, LatestPointPen, latest.Position, 4.0, 4.0);
        }

        // Halving milestone markers (2012-11-28, 2016-07-09, 2020-05-11, 2024-04-20)
        for (var i = 0; i < _halvingScreenPoints.Count; i++)
        {
            var halvingPt = _halvingScreenPoints[i];
            context.DrawEllipse(HalvingFillBrush, HalvingPen, halvingPt.Position, 4.5, 4.5);
        }
    }

    private static void DrawLegend(DrawingContext context, Typeface typeface)
    {
        const double startX = 12.0;
        const double startY = 10.0;

        // Halving legend item
        var halvingDotCenter = new Point(startX + 5.0, startY + 7.0);
        context.DrawEllipse(HalvingFillBrush, HalvingPen, halvingDotCenter, 4.5, 4.5);
        var halvingText = CreateText(language.PriceHistory_Spiral_HalvingLegend, typeface, 11.0, MutedTextBrush);
        context.DrawText(halvingText, new Point(startX + 15.0, startY));

        // Latest price legend item
        var row2Y = startY + halvingText.Height + 4.0;
        var latestDotCenter = new Point(startX + 5.0, row2Y + 7.0);
        context.DrawEllipse(BtcLightBrush, LatestPointPen, latestDotCenter, 4.0, 4.0);
        var latestText = CreateText(language.PriceHistory_Spiral_LatestPriceLegend, typeface, 11.0, MutedTextBrush);
        context.DrawText(latestText, new Point(startX + 15.0, row2Y));
    }

    private void DrawHoverOverlayAndTooltip(
        DrawingContext context,
        SpiralScreenPoint hovered,
        Typeface typeface,
        Typeface boldTypeface)
    {
        var fillBrush = hovered.Data.HalvingNumber.HasValue ? HalvingFillBrush : BtcPrimaryBrush;
        context.DrawEllipse(fillBrush, HoverMarkerPen, hovered.Position, 6.0, 6.0);

        var formattedPrice = hovered.Data.Price < 1m
            ? $"${hovered.Data.Price.ToString("N4", CultureInfo.InvariantCulture)}"
            : $"${hovered.Data.Price.ToString("N2", CultureInfo.InvariantCulture)}";

        var line1Str = $"{hovered.Data.Date:yyyy-MM-dd}  •  {formattedPrice}";
        var line2Str = string.Format(
            CultureInfo.CurrentUICulture,
            language.PriceHistory_Spiral_CycleInfo,
            hovered.Data.CycleNumber,
            hovered.Data.CycleYearIndex);

        if (hovered.Data.HalvingNumber.HasValue)
        {
            var halvingStr = string.Format(
                CultureInfo.CurrentUICulture,
                language.PriceHistory_Spiral_HalvingMilestone,
                hovered.Data.HalvingNumber.Value);
            line2Str = $"{line2Str} • {halvingStr}";
        }

        var line1Ft = CreateText(line1Str, boldTypeface, 12.0, TextBrush);
        var line2Ft = CreateText(line2Str, typeface, 11.0, BtcLightBrush);

        const double padX = 10.0;
        const double padY = 7.0;
        var boxWidth = Math.Max(line1Ft.Width, line2Ft.Width) + padX * 2.0;
        var boxHeight = line1Ft.Height + line2Ft.Height + 3.0 + padY * 2.0;

        var boxX = hovered.Position.X + 14.0;
        var boxY = hovered.Position.Y - boxHeight - 10.0;

        if (boxX + boxWidth > Bounds.Width - 8.0)
            boxX = hovered.Position.X - boxWidth - 14.0;
        if (boxY < 8.0)
            boxY = hovered.Position.Y + 14.0;

        boxX = Math.Clamp(boxX, 8.0, Math.Max(8.0, Bounds.Width - boxWidth - 8.0));
        boxY = Math.Clamp(boxY, 8.0, Math.Max(8.0, Bounds.Height - boxHeight - 8.0));

        var boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);
        context.DrawRectangle(TooltipBgBrush, TooltipBorderPen, boxRect, 5.0, 5.0);
        context.DrawText(line1Ft, new Point(boxX + padX, boxY + padY));
        context.DrawText(line2Ft, new Point(boxX + padX, boxY + padY + line1Ft.Height + 3.0));
    }

    private static FormattedText CreateText(string text, Typeface typeface, double fontSize, IBrush foreground)
    {
        return new FormattedText(
            text ?? string.Empty,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            foreground);
    }
}
