using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Goals.Queries;

[TestFixture]
public class GetGoalContributingTransactionsHandlerTests : DatabaseTest
{
    private GetGoalContributingTransactionsHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetAccounts().DeleteAll();
        _localDatabase.GetCategories().DeleteAll();

        _handler = new GetGoalContributingTransactionsHandler(_goalQueries);
    }

    [Test]
    public async Task SpendingLimit_Reconciles()
    {
        // Arrange
        var goal = GoalBuilder.ASpendingLimitGoal(1000m)
            .WithRefDate(new DateOnly(2024, 6, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);

        var account = FiatAccountBuilder.AnAccount()
            .WithName("Checking")
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(account);

        PriceDataBuilder.SeedRange(_priceDatabase,
            new DateTime(2024, 5, 24), new DateTime(2024, 7, 7), 50_000m);

        var transaction = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15))
            .WithName("Grocery")
            .AsFiatExpense(new Valt.Core.Modules.Budget.Accounts.AccountId(account.Id.ToString()), 300m)
            .Build();
        _localDatabase.GetTransactions().Insert(transaction);

        var entity = _localDatabase.GetGoals().FindAll().Single();
        var calculator = new SpendingLimitProgressCalculator(
            new GoalTransactionReader(_localDatabase, _priceDatabase,
                new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())));
        var input = new GoalProgressInput(
            GoalTypeNames.SpendingLimit,
            entity.GoalTypeJson,
            new DateOnly(2024, 6, 1),
            new DateOnly(2024, 6, 30));
        var progress = await calculator.CalculateProgressAsync(input);
        var calculatedSpending = ((SpendingLimitGoalType)progress.UpdatedGoalType).CalculatedSpending;

        // Act
        var result = await _handler.HandleAsync(new GetGoalContributingTransactionsQuery { GoalId = goal.Id.Value });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var supported = (GoalContributingTransactionsResult.Supported)result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(supported.Rows, Has.Count.EqualTo(1));
            var row = supported.Rows[0];
            Assert.That(row.Date, Is.EqualTo(new DateOnly(2024, 6, 15)));
            Assert.That(row.Description, Is.EqualTo("Grocery"));
            Assert.That(row.AccountName, Is.EqualTo("Checking"));
            Assert.That(row.CategoryName, Is.Null);
            Assert.That(row.FiatAmount.Value, Is.EqualTo(300m));
            Assert.That(row.FiatCurrencyCode, Is.EqualTo("USD"));
            Assert.That(row.SatsAmount.Sats, Is.EqualTo(600_000));
            Assert.That(row.RunningTotal, Is.EqualTo(300m));
            Assert.That(row.RunningTotal, Is.EqualTo(calculatedSpending));
        });
    }
}
