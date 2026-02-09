using NSubstitute;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class SaveFiatProgressCalculatorTests
{
    private IGoalTransactionReader _transactionReader = null!;
    private SaveFiatProgressCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionReader = Substitute.For<IGoalTransactionReader>();
        _calculator = new SaveFiatProgressCalculator(_transactionReader);
    }

    [Test]
    public void SupportedType_IsSaveFiat()
    {
        Assert.That(_calculator.SupportedType, Is.EqualTo(GoalTypeNames.SaveFiat));
    }

    [Test]
    public async Task CalculateProgress_WhenSavingsMatchTarget_Returns100Percent()
    {
        var goalType = new SaveFiatGoalType(1000m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SaveFiat, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(2000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(1000m);

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(100m));
        var updated = (SaveFiatGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedSavings, Is.EqualTo(1000m));
    }

    [Test]
    public async Task CalculateProgress_WhenSavingsAreHalfOfTarget_Returns50Percent()
    {
        var goalType = new SaveFiatGoalType(1000m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SaveFiat, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(2000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(1500m);

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(50m));
        var updated = (SaveFiatGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedSavings, Is.EqualTo(500m));
    }

    [Test]
    public async Task CalculateProgress_WhenExpensesExceedIncome_ReturnsZeroPercent()
    {
        var goalType = new SaveFiatGoalType(1000m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SaveFiat, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(500m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(800m);

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(0m));
        var updated = (SaveFiatGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedSavings, Is.EqualTo(-300m));
    }

    [Test]
    public async Task CalculateProgress_CapsAt100Percent()
    {
        var goalType = new SaveFiatGoalType(1000m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SaveFiat, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(5000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(1000m);

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(100m));
    }
}
