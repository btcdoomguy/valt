using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;
using Valt.App;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Mcp.Tools;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class GoalToolsTests : IntegrationTest
{
    private IQueryDispatcher _queryDispatcher = null!;
    private IGoalRepository _goalRepository = null!;

    [OneTimeSetUp]
    public void AddApplicationLayer()
    {
        _serviceCollection.AddValtApp();
        RebuildServiceProvider();
    }

    [SetUp]
    public new async Task SetUp()
    {
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        _goalRepository = _serviceProvider.GetRequiredService<IGoalRepository>();

        var existingGoals = (await _goalRepository.GetAllAsync()).ToList();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _localDatabase.GetTransactions().DeleteAll();
        _localDatabase.GetAccounts().DeleteAll();
        _localDatabase.GetCategories().DeleteAll();
    }

    [Test]
    public async Task Supported_Parity_With_AppQuery()
    {
        // Arrange: StackBitcoin monthly goal over June 2024, with a fiat account
        // and one Bitcoin income + one Bitcoin expense in a seeded BTC account
        var goal = GoalBuilder.AStackBitcoinGoal(1_000_000)
            .WithRefDate(new DateOnly(2024, 6, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);
        var goalId = goal.Id.Value;

        var fiatAccount = FiatAccountBuilder.AnAccount()
            .WithName("Checking")
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(fiatAccount);

        var btcAccount = BtcAccountBuilder.AnAccount()
            .WithName("Savings BTC")
            .Build();
        _localDatabase.GetAccounts().Insert(btcAccount);
        var btcAccountId = new AccountId(btcAccount.Id.ToString());

        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 10)).WithName("BTC income")
            .AsBitcoinIncome(btcAccountId, 100_000).Build());
        _localDatabase.GetTransactions().Insert(TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 6, 15)).WithName("BTC spend")
            .AsBitcoinExpense(btcAccountId, 25_000).Build());

        // Act: through the MCP tool (direct static invocation, DI-resolved dispatcher)
        var mcpResult = await GoalTools.GetGoalContributingTransactions(_queryDispatcher, goalId);

        // Assert: supported, typed, and structurally complete
        Assert.That(mcpResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(mcpResult!.Supported, Is.True);
            Assert.That(mcpResult.GoalType, Is.EqualTo("StackBitcoin"));
            Assert.That(mcpResult.StrategyUnit, Is.EqualTo("Sats"));
            Assert.That(mcpResult.Rows, Has.Count.EqualTo(2));
            Assert.That(mcpResult.FinalTotal, Is.EqualTo(mcpResult.Rows[^1].RunningTotal));
        });

        // Parity: dispatch the same App query directly through the container and
        // assert field-by-field — never against a hand-built expectation list
        var appResult = await _queryDispatcher.DispatchAsync(
            new GetGoalContributingTransactionsQuery { GoalId = goalId });
        Assert.That(appResult.IsSuccess, Is.True);
        var appRows = ((GoalContributingTransactionsResult.Supported)appResult.Value!).Rows;

        Assert.That(mcpResult!.Rows, Has.Count.EqualTo(appRows.Count));
        decimal previousRunningTotal = 0m;
        for (var i = 0; i < appRows.Count; i++)
        {
            var mcp = mcpResult.Rows[i];
            var app = appRows[i];
            var expectedContribution = i == 0
                ? app.RunningTotal
                : app.RunningTotal - previousRunningTotal;
            previousRunningTotal = app.RunningTotal;

            Assert.Multiple(() =>
            {
                Assert.That(mcp.Date, Is.EqualTo(app.Date));
                Assert.That(mcp.Description, Is.EqualTo(app.Description));
                Assert.That(mcp.Account, Is.EqualTo(app.AccountName));
                Assert.That(mcp.Category, Is.EqualTo(app.CategoryName));
                Assert.That(mcp.FiatAmount, Is.EqualTo(app.FiatAmount.Value));
                Assert.That(mcp.FiatCurrencyCode, Is.EqualTo(app.FiatCurrencyCode));
                Assert.That(mcp.SatsAmount, Is.EqualTo(app.SatsAmount.Sats));
                Assert.That(mcp.RunningTotal, Is.EqualTo(app.RunningTotal));
                Assert.That(mcp.Contribution, Is.EqualTo(expectedContribution));
            });
        }
    }

    [Test]
    public async Task NetWorthBtc_ReturnsSupportedFalse()
    {
        // Arrange: a goal type whose progress is not derived from transactions
        var goal = GoalBuilder.ANetWorthBtcGoal(10_000_000)
            .WithRefDate(new DateOnly(2024, 6, 15))
            .WithPeriod(GoalPeriods.Monthly)
            .Build();
        await _goalRepository.SaveAsync(goal);

        // Act
        var result = await GoalTools.GetGoalContributingTransactions(_queryDispatcher, goal.Id.Value);

        // Assert: typed not-supported, not an error
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Supported, Is.False);
            Assert.That(result.GoalType, Is.EqualTo("NetWorthBtc"));
            Assert.That(result.StrategyUnit, Is.Null);
            Assert.That(result.FinalTotal, Is.Null);
            Assert.That(result.Rows, Is.Empty);
        });
    }

    [Test]
    public async Task UnknownGoalId_ReturnsNull()
    {
        var result = await GoalTools.GetGoalContributingTransactions(
            _queryDispatcher, "000000000000000000000099");

        Assert.That(result, Is.Null);
    }
}
