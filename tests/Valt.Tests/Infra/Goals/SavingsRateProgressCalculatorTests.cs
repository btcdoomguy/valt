using NSubstitute;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.Tests.Infrastructure.Goals;

[TestFixture]
public class SavingsRateProgressCalculatorTests
{
    private IGoalTransactionReader _transactionReader = null!;
    private SavingsRateProgressCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionReader = Substitute.For<IGoalTransactionReader>();
        _calculator = new SavingsRateProgressCalculator(_transactionReader);
    }

    [Test]
    public void SupportedType_IsSavingsRate()
    {
        Assert.That(_calculator.SupportedType, Is.EqualTo(GoalTypeNames.SavingsRate));
    }

    [Test]
    public async Task CalculateProgress_WhenSavingsRateMatchesTarget_Returns100Percent()
    {
        var goalType = new SavingsRateGoalType(20m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SavingsRate, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(5000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(4000m);
        // Savings rate = (5000 - 4000) / 5000 * 100 = 20%

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(100m));
        var updated = (SavingsRateGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedPercentage, Is.EqualTo(20m));
    }

    [Test]
    public async Task CalculateProgress_WhenSavingsRateIsHalfOfTarget_Returns50Percent()
    {
        var goalType = new SavingsRateGoalType(20m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SavingsRate, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(5000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(4500m);
        // Savings rate = (5000 - 4500) / 5000 * 100 = 10%, progress = 10/20 * 100 = 50%

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(50m));
        var updated = (SavingsRateGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedPercentage, Is.EqualTo(10m));
    }

    [Test]
    public async Task CalculateProgress_WhenNoIncome_ReturnsZeroPercent()
    {
        var goalType = new SavingsRateGoalType(20m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SavingsRate, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(0m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(500m);

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(0m));
        var updated = (SavingsRateGoalType)result.UpdatedGoalType;
        Assert.That(updated.CalculatedPercentage, Is.EqualTo(0m));
    }

    [Test]
    public async Task CalculateProgress_CapsAt100Percent()
    {
        var goalType = new SavingsRateGoalType(20m);
        var json = GoalTypeSerializer.Serialize(goalType);
        var input = new GoalProgressInput(GoalTypeNames.SavingsRate, json, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        _transactionReader.CalculateTotalIncome(input.From, input.To).Returns(5000m);
        _transactionReader.CalculateTotalExpenses(input.From, input.To).Returns(1000m);
        // Savings rate = (5000 - 1000) / 5000 * 100 = 80%, progress = 80/20 * 100 = capped at 100%

        var result = await _calculator.CalculateProgressAsync(input);

        Assert.That(result.Progress, Is.EqualTo(100m));
    }
}
