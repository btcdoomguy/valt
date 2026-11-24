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
}
