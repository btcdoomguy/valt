using Valt.Core.Common;
using Valt.Core.Common.Exceptions;

namespace Valt.Tests.Domain.Common;

[TestFixture]
public class BtcValueTests
{
    [Test]
    public void Should_Throw_InvalidBtcValueException_When_Sats_Are_Negative()
    {
        Assert.Throws<InvalidBtcValueException>(() => BtcValue.New(-1));
    }

    [Test]
    public void Should_Create_BtcValue_With_Given_Sats()
    {
        var btcValue = BtcValue.New(100000);
        Assert.That(btcValue.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Create_BtcValue_From_Another_BtcValue()
    {
        var initialBtcValue = BtcValue.New(100000);
        var btcValue = BtcValue.New(initialBtcValue);
        Assert.That(btcValue.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Return_Empty_BtcValue_As_Zero()
    {
        var btcValue = BtcValue.Empty;
        Assert.That(btcValue.ToString(), Is.EqualTo("0"));
    }

    [Test]
    public void Should_Convert_Sats_To_Bitcoin_String()
    {
        var btcValue = BtcValue.New(100000000); // 1 BTC
        Assert.That(btcValue.ToBitcoinString(), Is.EqualTo("1"));
    }

    [Test]
    public void Should_Add_Two_BtcValues()
    {
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(200000);
        var result = btcValue1 + btcValue2;
        Assert.That(result.ToString(), Is.EqualTo("300000"));
    }

    [Test]
    public void Should_Subtract_Two_BtcValues()
    {
        var btcValue1 = BtcValue.New(300000);
        var btcValue2 = BtcValue.New(200000);
        var result = btcValue1 - btcValue2;
        Assert.That(result.ToString(), Is.EqualTo("100000"));
    }

    [Test]
    public void Should_Multiply_Two_BtcValues()
    {
        var btcValue1 = BtcValue.New(2000);
        var btcValue2 = BtcValue.New(3000);
        var result = btcValue1 * btcValue2;
        Assert.That(result.ToString(), Is.EqualTo("6000000"));
    }

    [Test]
    public void Should_Divide_Two_BtcValues()
    {
        var btcValue1 = BtcValue.New(6000000);
        var btcValue2 = BtcValue.New(2000);
        var result = btcValue1 / btcValue2;
        Assert.That(result.ToString(), Is.EqualTo("3000"));
    }

    [Test]
    public void Should_Check_If_Two_BtcValues_Are_Equal()
    {
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(100000);
        Assert.That(btcValue1 == btcValue2);
    }

    [Test]
    public void Should_Check_If_Two_BtcValues_Are_Not_Equal()
    {
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(200000);
        Assert.That(btcValue1 != btcValue2);
    }

    [Test]
    public void Should_Check_If_One_BtcValue_Is_Less_Than_Another()
    {
        var btcValue1 = BtcValue.New(100000);
        var btcValue2 = BtcValue.New(200000);
        Assert.That(btcValue1 < btcValue2);
    }

    [Test]
    public void Should_Check_If_One_BtcValue_Is_Greater_Than_Another()
    {
        var btcValue1 = BtcValue.New(300000);
        var btcValue2 = BtcValue.New(200000);
        Assert.That(btcValue1 > btcValue2);
    }

    [Test]
    public void Should_Check_If_One_BtcValue_Is_Less_Than_Or_Equal_To_Another()
    {
        var btcValue1 = BtcValue.New(200000);
        var btcValue2 = BtcValue.New(200000);
        Assert.That(btcValue1 <= btcValue2);
    }

    [Test]
    public void Should_Check_If_One_BtcValue_Is_Greater_Than_Or_Equal_To_Another()
    {
        var btcValue1 = BtcValue.New(300000);
        var btcValue2 = BtcValue.New(300000);
        Assert.That(btcValue1 >= btcValue2);
    }
}