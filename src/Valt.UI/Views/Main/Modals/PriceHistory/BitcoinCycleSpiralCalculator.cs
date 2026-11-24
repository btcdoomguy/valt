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
    int? HalvingNumber,
    bool IsCycleTop = false,
    bool IsCycleBottom = false);

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

    public static readonly IReadOnlyList<(int Cycle, DateTime PeakWindowStart, DateTime PeakWindowEnd, DateTime CycleEnd)> CycleBullBearWindows =
    [
        (1, new DateTime(2009, 1, 1),  new DateTime(2011, 12, 31), new DateTime(2012, 11, 28)),
        (2, new DateTime(2012, 11, 29), new DateTime(2014, 6, 30),  new DateTime(2016, 7, 9)),
        (3, new DateTime(2016, 7, 10),  new DateTime(2018, 6, 30),  new DateTime(2020, 5, 11)),
        (4, new DateTime(2020, 5, 12),  new DateTime(2022, 6, 30),  new DateTime(2024, 4, 20)),
        (5, new DateTime(2024, 4, 21),  new DateTime(2026, 3, 31),  new DateTime(2028, 4, 20))
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

        MarkCycleTopsAndBottoms(result);

        return result;
    }

    private static (DateTime PeakWindowStart, DateTime PeakWindowEnd, DateTime CycleEnd) GetCycleWindow(int cycleNumber)
    {
        for (var i = 0; i < CycleBullBearWindows.Count; i++)
        {
            if (CycleBullBearWindows[i].Cycle == cycleNumber)
            {
                var w = CycleBullBearWindows[i];
                return (w.PeakWindowStart, w.PeakWindowEnd, w.CycleEnd);
            }
        }

        var startYear = 2009 + (cycleNumber - 1) * 4;
        return (
            new DateTime(startYear - 1, 4, 21),
            new DateTime(startYear + 1, 6, 30),
            new DateTime(startYear + 3, 4, 20));
    }

    private static void MarkCycleTopsAndBottoms(List<SpiralDataPoint> points)
    {
        if (points.Count < 2)
            return;

        var maxCycle = 1;
        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].CycleNumber > maxCycle)
                maxCycle = points[i].CycleNumber;
        }

        for (var cycle = 1; cycle <= maxCycle; cycle++)
        {
            var (peakStart, peakEnd, cycleEnd) = GetCycleWindow(cycle);

            var topIdx = -1;
            var topPrice = decimal.MinValue;
            var peakCandidateCount = 0;

            for (var i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                if (pt.Date >= peakStart && pt.Date <= peakEnd)
                {
                    peakCandidateCount++;
                    if (pt.Price > topPrice)
                    {
                        topPrice = pt.Price;
                        topIdx = i;
                    }
                }
            }

            if (topIdx < 0 || peakCandidateCount < 2)
                continue;

            var topDate = points[topIdx].Date;
            points[topIdx] = points[topIdx] with { IsCycleTop = true };

            var bottomIdx = -1;
            var bottomPrice = topPrice;

            for (var i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                if (pt.Date > topDate && pt.Date <= cycleEnd && pt.Price < bottomPrice)
                {
                    bottomPrice = pt.Price;
                    bottomIdx = i;
                }
            }

            if (bottomIdx >= 0)
            {
                points[bottomIdx] = points[bottomIdx] with { IsCycleBottom = true };
            }
        }
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

            var isMilestone = pt.Data.HalvingNumber.HasValue || pt.Data.IsCycleTop || pt.Data.IsCycleBottom;
            var effectiveDistSq = isMilestone ? distSq * 0.1 : distSq;
            if (effectiveDistSq <= bestEffectiveDistSq)
            {
                bestEffectiveDistSq = effectiveDistSq;
                nearest = pt;
            }
        }

        return nearest;
    }
}
