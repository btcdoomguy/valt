using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.Tests.Domain.Goals;

[TestFixture]
public class GoalTypeTests
{
    #region StackBitcoinGoalType Tests

    [Test]
    public void StackBitcoinGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new StackBitcoinGoalType(1_000_000L, 500_000L);
        var type2 = new StackBitcoinGoalType(1_000_000L, 250_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void StackBitcoinGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new StackBitcoinGoalType(1_000_000L, 500_000L);
        var type2 = new StackBitcoinGoalType(2_000_000L, 500_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void StackBitcoinGoalType_HasSameTargetAs_ReturnsFalseForDifferentType()
    {
        var stackType = new StackBitcoinGoalType(1_000_000L, 500_000L);
        var dcaType = new DcaGoalType(10, 5);

        Assert.That(stackType.HasSameTargetAs(dcaType), Is.False);
    }

    [Test]
    public void StackBitcoinGoalType_WithResetProgress_ResetsCalculatedSatsToZero()
    {
        var original = new StackBitcoinGoalType(1_000_000L, 750_000L);

        var reset = (StackBitcoinGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedSats, Is.EqualTo(0));
    }

    [Test]
    public void StackBitcoinGoalType_WithResetProgress_PreservesTargetSats()
    {
        var original = new StackBitcoinGoalType(1_000_000L, 750_000L);

        var reset = (StackBitcoinGoalType)original.WithResetProgress();

        Assert.That(reset.TargetSats, Is.EqualTo(1_000_000L));
    }

    #endregion

    #region DcaGoalType Tests

    [Test]
    public void DcaGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new DcaGoalType(10, 5);
        var type2 = new DcaGoalType(10, 3);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void DcaGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new DcaGoalType(10, 5);
        var type2 = new DcaGoalType(20, 5);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void DcaGoalType_WithResetProgress_ResetsCalculatedCountToZero()
    {
        var original = new DcaGoalType(10, 7);

        var reset = (DcaGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedPurchaseCount, Is.EqualTo(0));
    }

    [Test]
    public void DcaGoalType_WithResetProgress_PreservesTargetPurchaseCount()
    {
        var original = new DcaGoalType(10, 7);

        var reset = (DcaGoalType)original.WithResetProgress();

        Assert.That(reset.TargetPurchaseCount, Is.EqualTo(10));
    }

    #endregion

    #region IncomeBtcGoalType Tests

    [Test]
    public void IncomeBtcGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new IncomeBtcGoalType(500_000L, 250_000L);
        var type2 = new IncomeBtcGoalType(500_000L, 100_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void IncomeBtcGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new IncomeBtcGoalType(500_000L, 250_000L);
        var type2 = new IncomeBtcGoalType(750_000L, 250_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void IncomeBtcGoalType_WithResetProgress_ResetsCalculatedSatsToZero()
    {
        var original = new IncomeBtcGoalType(500_000L, 300_000L);

        var reset = (IncomeBtcGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedSats, Is.EqualTo(0));
    }

    [Test]
    public void IncomeBtcGoalType_WithResetProgress_PreservesTargetSats()
    {
        var original = new IncomeBtcGoalType(500_000L, 300_000L);

        var reset = (IncomeBtcGoalType)original.WithResetProgress();

        Assert.That(reset.TargetSats, Is.EqualTo(500_000L));
    }

    #endregion

    #region IncomeFiatGoalType Tests

    [Test]
    public void IncomeFiatGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new IncomeFiatGoalType(5000m, 2500m);
        var type2 = new IncomeFiatGoalType(5000m, 1000m);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void IncomeFiatGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new IncomeFiatGoalType(5000m, 2500m);
        var type2 = new IncomeFiatGoalType(7500m, 2500m);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void IncomeFiatGoalType_WithResetProgress_ResetsCalculatedIncomeToZero()
    {
        var original = new IncomeFiatGoalType(5000m, 3500m);

        var reset = (IncomeFiatGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedIncome, Is.EqualTo(0m));
    }

    [Test]
    public void IncomeFiatGoalType_WithResetProgress_PreservesTargetAmount()
    {
        var original = new IncomeFiatGoalType(5000m, 3500m);

        var reset = (IncomeFiatGoalType)original.WithResetProgress();

        Assert.That(reset.TargetAmount, Is.EqualTo(5000m));
    }

    #endregion

    #region SpendingLimitGoalType Tests

    [Test]
    public void SpendingLimitGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new SpendingLimitGoalType(1000m, 500m);
        var type2 = new SpendingLimitGoalType(1000m, 250m);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void SpendingLimitGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new SpendingLimitGoalType(1000m, 500m);
        var type2 = new SpendingLimitGoalType(2000m, 500m);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void SpendingLimitGoalType_WithResetProgress_ResetsCalculatedSpendingToZero()
    {
        var original = new SpendingLimitGoalType(1000m, 800m);

        var reset = (SpendingLimitGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedSpending, Is.EqualTo(0m));
    }

    [Test]
    public void SpendingLimitGoalType_WithResetProgress_PreservesTargetAmount()
    {
        var original = new SpendingLimitGoalType(1000m, 800m);

        var reset = (SpendingLimitGoalType)original.WithResetProgress();

        Assert.That(reset.TargetAmount, Is.EqualTo(1000m));
    }

    #endregion

    #region ReduceExpenseCategoryGoalType Tests

    [Test]
    public void ReduceExpenseCategoryGoalType_HasSameTargetAs_RequiresSameCategoryAndAmount()
    {
        var type1 = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 250m);
        var type2 = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 100m);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void ReduceExpenseCategoryGoalType_HasSameTargetAs_ReturnsFalseForDifferentCategory()
    {
        var type1 = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 250m);
        var type2 = new ReduceExpenseCategoryGoalType(500m, "cat-456", "Transport", 250m);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void ReduceExpenseCategoryGoalType_HasSameTargetAs_ReturnsFalseForDifferentAmount()
    {
        var type1 = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 250m);
        var type2 = new ReduceExpenseCategoryGoalType(750m, "cat-123", "Food", 250m);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void ReduceExpenseCategoryGoalType_WithResetProgress_ResetsCalculatedSpendingToZero()
    {
        var original = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 400m);

        var reset = (ReduceExpenseCategoryGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedSpending, Is.EqualTo(0m));
    }

    [Test]
    public void ReduceExpenseCategoryGoalType_WithResetProgress_PreservesCategoryInfo()
    {
        var original = new ReduceExpenseCategoryGoalType(500m, "cat-123", "Food", 400m);

        var reset = (ReduceExpenseCategoryGoalType)original.WithResetProgress();

        Assert.That(reset.TargetAmount, Is.EqualTo(500m));
        Assert.That(reset.CategoryId, Is.EqualTo("cat-123"));
        Assert.That(reset.CategoryName, Is.EqualTo("Food"));
    }

    #endregion

    #region BitcoinHodlGoalType Tests

    [Test]
    public void BitcoinHodlGoalType_HasSameTargetAs_ReturnsTrueForSameTarget()
    {
        var type1 = new BitcoinHodlGoalType(100_000L, 50_000L);
        var type2 = new BitcoinHodlGoalType(100_000L, 25_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.True);
    }

    [Test]
    public void BitcoinHodlGoalType_HasSameTargetAs_ReturnsFalseForDifferentTarget()
    {
        var type1 = new BitcoinHodlGoalType(100_000L, 50_000L);
        var type2 = new BitcoinHodlGoalType(200_000L, 50_000L);

        Assert.That(type1.HasSameTargetAs(type2), Is.False);
    }

    [Test]
    public void BitcoinHodlGoalType_WithResetProgress_ResetsCalculatedSoldSatsToZero()
    {
        var original = new BitcoinHodlGoalType(100_000L, 75_000L);

        var reset = (BitcoinHodlGoalType)original.WithResetProgress();

        Assert.That(reset.CalculatedSoldSats, Is.EqualTo(0));
    }

    [Test]
    public void BitcoinHodlGoalType_WithResetProgress_PreservesMaxSellableSats()
    {
        var original = new BitcoinHodlGoalType(100_000L, 75_000L);

        var reset = (BitcoinHodlGoalType)original.WithResetProgress();

        Assert.That(reset.MaxSellableSats, Is.EqualTo(100_000L));
    }

    #endregion
}
