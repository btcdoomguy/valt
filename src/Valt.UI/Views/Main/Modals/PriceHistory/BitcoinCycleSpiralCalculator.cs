using System;
using System.Collections.Generic;
using Avalonia;

namespace Valt.UI.Views.Main.Modals.PriceHistory;

public sealed record SpiralDataPoint(
    DateTime Date,
    decimal Price,
    double AngleRadians,
    double NormalizedRadius,
    int CycleNumber,
    int CycleYearIndex,
    int? HalvingNumber);

public sealed record SpiralScreenPoint(
    Point Position,
    SpiralDataPoint Data);

public static class BitcoinCycleSpiralCalculator
{
    public static readonly DateTime CycleEpoch = new(2009, 1, 1);
    public const double DaysPerFourYearCycle = 1461.0;
    public const double TopAngleOffsetRadians = -Math.PI / 2.0;

    public const double MinLog10Price = -3.0;
    public const double MaxLog10Price = 6.0;
    public const double Log10PriceSpan = MaxLog10Price - MinLog10Price;

    public static readonly IReadOnlyList<(int Number, DateTime Date)> HalvingMilestones =
    [
        (1, new DateTime(2012, 11, 28)),
        (2, new DateTime(2016, 7, 9)),
        (3, new DateTime(2020, 5, 11)),
        (4, new DateTime(2024, 4, 20))
    ];

    public static readonly IReadOnlyList<(double Log10Value, string Label)> PriceRingDefinitions =
    [
        (-3, "$0.001"),
        (-2, "$0.01"),
        (-1, "$0.1"),
        ( 0, "$1"),
        ( 1, "$10"),
        ( 2, "$100"),
        ( 3, "$1000"),
        ( 4, "$10k"),
        ( 5, "$100k"),
        ( 6, "$1M")
    ];

    public static readonly IReadOnlyList<(double AngleRadians, string Label)> CardinalYearLabels =
    [
        (-Math.PI / 2.0, "'09, '13, '17, '21, '25"),
        (0.0,            "'10, '14, '18, '22, '26"),
        (Math.PI / 2.0,  "'11, '15, '19, '23, '27"),
        (Math.PI,        "'12, '16, '20, '24, '28")
    ];

    public static double CalculateAngleRadians(DateTime date)
    {
        var daysSinceEpoch = (date.Date - CycleEpoch).TotalDays;
        return TopAngleOffsetRadians + 2.0 * Math.PI * (daysSinceEpoch / DaysPerFourYearCycle);
    }

    public static double CalculateNormalizedRadius(decimal price)
    {
        if (price <= 0m)
            return 0.0;

        var log10 = Math.Log10((double)price);
        var normalized = (log10 - MinLog10Price) / Log10PriceSpan;
        return Math.Clamp(normalized, 0.0, 1.0);
    }

    public static Point PolarToCartesian(Point center, double maxRadius, double angleRadians, double normalizedRadius)
    {
        var r = Math.Clamp(normalizedRadius, 0.0, 1.0) * maxRadius;
        return new Point(
            center.X + r * Math.Cos(angleRadians),
            center.Y + r * Math.Sin(angleRadians));
    }

    public static int GetCycleNumber(DateTime date)
    {
        if (date.Year < 2009) return 1;
        return ((date.Year - 2009) / 4) + 1;
    }

    public static int GetCycleYearIndex(DateTime date)
    {
        var offset = (date.Year - 2009) % 4;
        return offset < 0 ? offset + 4 + 1 : offset + 1;
    }

    public static int? GetHalvingNumber(DateTime date)
    {
        var d = date.Date;
        foreach (var (number, halvingDate) in HalvingMilestones)
        {
            if (d == halvingDate)
                return number;
        }
        return null;
    }

    public static IReadOnlyList<SpiralDataPoint> BuildPoints(IEnumerable<(DateTime Date, decimal Price)> chronologicalData)
    {
        var result = new List<SpiralDataPoint>();
        foreach (var (date, price) in chronologicalData)
        {
            if (price <= 0m) continue;

            var angle = CalculateAngleRadians(date);
            var normRadius = CalculateNormalizedRadius(price);
            var cycleNumber = GetCycleNumber(date);
            var cycleYearIdx = GetCycleYearIndex(date);
            var halvingNumber = GetHalvingNumber(date);

            result.Add(new SpiralDataPoint(
                date.Date,
                price,
                angle,
                normRadius,
                cycleNumber,
                cycleYearIdx,
                halvingNumber));
        }
        return result;
    }

    public static SpiralScreenPoint? FindNearestPoint(
        IReadOnlyList<SpiralScreenPoint> screenPoints,
        Point pointerPosition,
        double maxDistancePixels = 18.0)
    {
        if (screenPoints.Count == 0)
            return null;

        var maxDistSq = maxDistancePixels * maxDistancePixels;
        SpiralScreenPoint? nearest = null;
        var bestEffectiveDistSq = maxDistSq;

        for (var i = 0; i < screenPoints.Count; i++)
        {
            var pt = screenPoints[i];
            var dx = pt.Position.X - pointerPosition.X;
            var dy = pt.Position.Y - pointerPosition.Y;
            var distSq = dx * dx + dy * dy;

            if (distSq > maxDistSq)
                continue;

            var effectiveDistSq = pt.Data.HalvingNumber.HasValue ? distSq * 0.1 : distSq;
            if (effectiveDistSq <= bestEffectiveDistSq)
            {
                bestEffectiveDistSq = effectiveDistSq;
                nearest = pt;
            }
        }

        return nearest;
    }
}
