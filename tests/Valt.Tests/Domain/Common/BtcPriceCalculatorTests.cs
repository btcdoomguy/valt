using Valt.Core.Common;

namespace Valt.Tests.Domain.Common;

[TestFixture]
public class BtcPriceCalculatorTests
{
    [Test]
    public void Should_Calculate_BtcAmountOfFiat()
    {
        var sats = BtcPriceCalculator.CalculateBtcAmountOfFiat(200000, 5.45m, 105000);

        Assert.That(sats, Is.EqualTo(34949760));
    }
}