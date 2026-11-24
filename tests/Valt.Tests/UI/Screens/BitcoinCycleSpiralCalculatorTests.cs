using System;
using System.Collections.Generic;
using Avalonia;
using Valt.UI.Views.Main.Modals.PriceHistory;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class BitcoinCycleSpiralCalculatorTests
{
    [Test]
    public void CalculateAngleRadians_ShouldMapCardinalTopYearsToExactFullRevolutionsWith1461DayPeriod()
    {
        var angle2009 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2009, 1, 1));
        var angle2013 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2013, 1, 1));
        var angle2017 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2017, 1, 1));
        var angle2021 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2021, 1, 1));
        var angle2025 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2025, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(BitcoinCycleSpiralCalculator.DaysPerFourYearCycle, Is.EqualTo(1461.0));
            Assert.That(angle2009, Is.EqualTo(-Math.PI / 2.0).Within(1e-9));
            Assert.That(angle2013, Is.EqualTo(-Math.PI / 2.0 + 2.0 * Math.PI).Within(1e-9));
            Assert.That(angle2017, Is.EqualTo(-Math.PI / 2.0 + 4.0 * Math.PI).Within(1e-9));
            Assert.That(angle2021, Is.EqualTo(-Math.PI / 2.0 + 6.0 * Math.PI).Within(1e-9));
            Assert.That(angle2025, Is.EqualTo(-Math.PI / 2.0 + 8.0 * Math.PI).Within(1e-9));
        });
    }

    [Test]
    public void CalculateAngleRadians_ShouldMapRightBottomAndLeftCardinalYearsClockwise()
    {
        var angle2010 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2010, 1, 1));
        var angle2011 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2011, 1, 1));
        var angle2012 = BitcoinCycleSpiralCalculator.CalculateAngleRadians(new DateTime(2012, 1, 1));

        // 365 / 1461, 730 / 1461, 1095 / 1461 turns from -pi/2
        Assert.Multiple(() =>
        {
            Assert.That(angle2010, Is.EqualTo(-Math.PI / 2.0 + 2.0 * Math.PI * (365.0 / 1461.0)).Within(1e-9));
            Assert.That(angle2010, Is.EqualTo(0.0).Within(0.005));

            Assert.That(angle2011, Is.EqualTo(-Math.PI / 2.0 + 2.0 * Math.PI * (730.0 / 1461.0)).Within(1e-9));
            Assert.That(angle2011, Is.EqualTo(Math.PI / 2.0).Within(0.005));

            Assert.That(angle2012, Is.EqualTo(-Math.PI / 2.0 + 2.0 * Math.PI * (1095.0 / 1461.0)).Within(1e-9));
            Assert.That(angle2012, Is.EqualTo(Math.PI).Within(0.005));
        });
    }

    [Test]
    public void CalculateNormalizedRadius_ShouldMapAllTenDecadesAndGuardInvalidOrOutOfRangePrices()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(0.001m), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(0.01m), Is.EqualTo(1.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(0.1m), Is.EqualTo(2.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(1m), Is.EqualTo(3.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(10m), Is.EqualTo(4.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(100m), Is.EqualTo(5.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(1000m), Is.EqualTo(6.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(10000m), Is.EqualTo(7.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(100000m), Is.EqualTo(8.0 / 9.0).Within(1e-9));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(1000000m), Is.EqualTo(1.0).Within(1e-9));

            // Guard against non-positive and out-of-range prices
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(0m), Is.EqualTo(0.0));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(-50m), Is.EqualTo(0.0));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(0.0001m), Is.EqualTo(0.0));
            Assert.That(BitcoinCycleSpiralCalculator.CalculateNormalizedRadius(5_000_000m), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void GetHalvingNumber_And_BuildPoints_ShouldDetectFourHalvingsAndPopulateCyclesWhileFilteringNonPositivePrices()
    {
        var input = new List<(DateTime Date, decimal Price)>
        {
            (new DateTime(2010, 7, 17), 0m),
            (new DateTime(2010, 7, 18), -1m),
            (new DateTime(2010, 7, 19), 0.08m),
            (new DateTime(2012, 11, 28), 12.35m),
            (new DateTime(2016, 7, 9), 650.63m),
            (new DateTime(2020, 5, 11), 8601.80m),
            (new DateTime(2024, 4, 20), 63850.00m),
            (new DateTime(2025, 3, 15), 84000.00m)
        };

        var points = BitcoinCycleSpiralCalculator.BuildPoints(input);

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Count.EqualTo(6));

            Assert.That(points[0].Date, Is.EqualTo(new DateTime(2010, 7, 19)));
            Assert.That(points[0].CycleNumber, Is.EqualTo(1));
            Assert.That(points[0].CycleYearIndex, Is.EqualTo(2));
            Assert.That(points[0].HalvingNumber, Is.Null);

            Assert.That(points[1].HalvingNumber, Is.EqualTo(1));
            Assert.That(points[1].CycleNumber, Is.EqualTo(1));
            Assert.That(points[1].CycleYearIndex, Is.EqualTo(4));

            Assert.That(points[2].HalvingNumber, Is.EqualTo(2));
            Assert.That(points[2].CycleNumber, Is.EqualTo(2));
            Assert.That(points[2].CycleYearIndex, Is.EqualTo(4));

            Assert.That(points[3].HalvingNumber, Is.EqualTo(3));
            Assert.That(points[3].CycleNumber, Is.EqualTo(3));
            Assert.That(points[3].CycleYearIndex, Is.EqualTo(4));

            Assert.That(points[4].HalvingNumber, Is.EqualTo(4));
            Assert.That(points[4].CycleNumber, Is.EqualTo(4));
            Assert.That(points[4].CycleYearIndex, Is.EqualTo(4));

            Assert.That(points[5].CycleNumber, Is.EqualTo(5));
            Assert.That(points[5].CycleYearIndex, Is.EqualTo(1));
            Assert.That(points[5].HalvingNumber, Is.Null);
        });
    }

    [Test]
    public void FindNearestPoint_ShouldReturnClosestPointWithinThreshold_PrioritizeHalvingMarkers_AndReturnNullWhenOutside()
    {
        var normalData = new SpiralDataPoint(new DateTime(2020, 5, 10), 8700m, 0.0, 0.7, 3, 4, null);
        var halvingData = new SpiralDataPoint(new DateTime(2020, 5, 11), 8600m, 0.01, 0.7, 3, 4, 3);
        var farData = new SpiralDataPoint(new DateTime(2021, 1, 1), 29000m, 1.5, 0.8, 4, 1, null);

        var screenPoints = new List<SpiralScreenPoint>
        {
            new(new Point(100, 100), normalData),
            new(new Point(104, 100), halvingData),
            new(new Point(250, 250), farData)
        };

        var foundHalving = BitcoinCycleSpiralCalculator.FindNearestPoint(screenPoints, new Point(101, 100), maxDistancePixels: 18.0);
        var foundFar = BitcoinCycleSpiralCalculator.FindNearestPoint(screenPoints, new Point(245, 248), maxDistancePixels: 18.0);
        var foundNone = BitcoinCycleSpiralCalculator.FindNearestPoint(screenPoints, new Point(400, 400), maxDistancePixels: 18.0);
        var foundEmpty = BitcoinCycleSpiralCalculator.FindNearestPoint(Array.Empty<SpiralScreenPoint>(), new Point(100, 100), maxDistancePixels: 18.0);

        Assert.Multiple(() =>
        {
            Assert.That(foundHalving, Is.Not.Null);
            Assert.That(foundHalving!.Data.HalvingNumber, Is.EqualTo(3));

            Assert.That(foundFar, Is.Not.Null);
            Assert.That(foundFar!.Data.Date, Is.EqualTo(new DateTime(2021, 1, 1)));

            Assert.That(foundNone, Is.Null);
            Assert.That(foundEmpty, Is.Null);
        });
    }

    [Test]
    public void BuildPoints_ShouldDetectBullMarketTopsAndBearMarketBottomsAcrossAllFiveCycles()
    {
        var input = new List<(DateTime Date, decimal Price)>
        {
            // Cycle 1 (2009-01-01 to Halving #1 2012-11-28)
            (new DateTime(2010, 7, 17), 0.05m),
            (new DateTime(2011, 6, 8), 29.60m),     // Cycle 1 Top
            (new DateTime(2011, 9, 15), 5.10m),
            (new DateTime(2011, 11, 18), 2.05m),    // Cycle 1 Bottom
            (new DateTime(2012, 11, 28), 12.35m),   // Halving #1

            // Cycle 2 (2012-11-29 to Halving #2 2016-07-09)
            (new DateTime(2013, 4, 10), 230.00m),
            (new DateTime(2013, 12, 4), 1147.25m),  // Cycle 2 Top
            (new DateTime(2014, 6, 15), 580.00m),
            (new DateTime(2015, 1, 14), 172.15m),   // Cycle 2 Bottom
            (new DateTime(2016, 7, 9), 650.63m),    // Halving #2

            // Cycle 3 (2016-07-10 to Halving #3 2020-05-11)
            (new DateTime(2017, 6, 11), 2950.00m),
            (new DateTime(2017, 12, 16), 19665.40m), // Cycle 3 Top
            (new DateTime(2018, 6, 20), 6700.00m),
            (new DateTime(2018, 12, 15), 3216.63m),  // Cycle 3 Bottom
            (new DateTime(2020, 5, 11), 8601.80m),   // Halving #3

            // Cycle 4 (2020-05-12 to Halving #4 2024-04-20)
            (new DateTime(2021, 4, 14), 63503.46m),
            (new DateTime(2021, 11, 9), 67617.02m),  // Cycle 4 Top
            (new DateTime(2022, 6, 18), 19017.64m),
            (new DateTime(2022, 11, 21), 15742.44m), // Cycle 4 Bottom
            (new DateTime(2024, 4, 20), 63850.00m),  // Halving #4

            // Cycle 5 (2024-04-21 to 2028-04-20)
            (new DateTime(2024, 12, 17), 106140.00m),
            (new DateTime(2025, 10, 6), 124766.50m), // Cycle 5 Top
            (new DateTime(2026, 2, 15), 75000.00m),
            (new DateTime(2026, 6, 30), 58531.90m),  // Cycle 5 Bottom
            (new DateTime(2026, 10, 1), 68000.00m)
        };

        var points = BitcoinCycleSpiralCalculator.BuildPoints(input);

        var tops = new List<SpiralDataPoint>();
        var bottoms = new List<SpiralDataPoint>();
        foreach (var pt in points)
        {
            if (pt.IsCycleTop) tops.Add(pt);
            if (pt.IsCycleBottom) bottoms.Add(pt);
        }

        Assert.Multiple(() =>
        {
            Assert.That(tops, Has.Count.EqualTo(5));
            Assert.That(bottoms, Has.Count.EqualTo(5));

            Assert.That(tops[0].Date, Is.EqualTo(new DateTime(2011, 6, 8)));
            Assert.That(tops[0].Price, Is.EqualTo(29.60m));
            Assert.That(bottoms[0].Date, Is.EqualTo(new DateTime(2011, 11, 18)));
            Assert.That(bottoms[0].Price, Is.EqualTo(2.05m));

            Assert.That(tops[1].Date, Is.EqualTo(new DateTime(2013, 12, 4)));
            Assert.That(tops[1].Price, Is.EqualTo(1147.25m));
            Assert.That(bottoms[1].Date, Is.EqualTo(new DateTime(2015, 1, 14)));
            Assert.That(bottoms[1].Price, Is.EqualTo(172.15m));

            Assert.That(tops[2].Date, Is.EqualTo(new DateTime(2017, 12, 16)));
            Assert.That(tops[2].Price, Is.EqualTo(19665.40m));
            Assert.That(bottoms[2].Date, Is.EqualTo(new DateTime(2018, 12, 15)));
            Assert.That(bottoms[2].Price, Is.EqualTo(3216.63m));

            Assert.That(tops[3].Date, Is.EqualTo(new DateTime(2021, 11, 9)));
            Assert.That(tops[3].Price, Is.EqualTo(67617.02m));
            Assert.That(bottoms[3].Date, Is.EqualTo(new DateTime(2022, 11, 21)));
            Assert.That(bottoms[3].Price, Is.EqualTo(15742.44m));

            Assert.That(tops[4].Date, Is.EqualTo(new DateTime(2025, 10, 6)));
            Assert.That(tops[4].Price, Is.EqualTo(124766.50m));
            Assert.That(bottoms[4].Date, Is.EqualTo(new DateTime(2026, 6, 30)));
            Assert.That(bottoms[4].Price, Is.EqualTo(58531.90m));
        });
    }

    [Test]
    public void FindNearestPoint_ShouldPrioritizeCycleTopAndCycleBottomMarkers()
    {
        var normalNearTop = new SpiralDataPoint(new DateTime(2021, 11, 8), 66000m, 0.0, 0.85, 4, 1, null);
        var cycleTop = new SpiralDataPoint(new DateTime(2021, 11, 9), 67617m, 0.01, 0.86, 4, 1, null, IsCycleTop: true);
        var normalNearBottom = new SpiralDataPoint(new DateTime(2022, 11, 20), 16200m, 1.5, 0.75, 4, 2, null);
        var cycleBottom = new SpiralDataPoint(new DateTime(2022, 11, 21), 15742m, 1.51, 0.74, 4, 2, null, IsCycleBottom: true);

        var screenPoints = new List<SpiralScreenPoint>
        {
            new(new Point(100, 100), normalNearTop),
            new(new Point(104, 100), cycleTop),
            new(new Point(200, 200), normalNearBottom),
            new(new Point(204, 200), cycleBottom)
        };

        var foundTop = BitcoinCycleSpiralCalculator.FindNearestPoint(screenPoints, new Point(101, 100), maxDistancePixels: 18.0);
        var foundBottom = BitcoinCycleSpiralCalculator.FindNearestPoint(screenPoints, new Point(201, 200), maxDistancePixels: 18.0);

        Assert.Multiple(() =>
        {
            Assert.That(foundTop, Is.Not.Null);
            Assert.That(foundTop!.Data.IsCycleTop, Is.True);
            Assert.That(foundTop.Data.Date, Is.EqualTo(new DateTime(2021, 11, 9)));

            Assert.That(foundBottom, Is.Not.Null);
            Assert.That(foundBottom!.Data.IsCycleBottom, Is.True);
            Assert.That(foundBottom.Data.Date, Is.EqualTo(new DateTime(2022, 11, 21)));
        });
    }
}
