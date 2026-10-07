using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Modals.GoalSummary;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class GoalSummaryViewModelTests
{
    private static GoalSummaryViewModel.Request CreateRequest(
        GoalStrategyUnit unit,
        params ContributingTransactionRow[] rows)
    {
        return new GoalSummaryViewModel.Request
        {
            GoalId = "goal-1",
            GoalName = "Stack bitcoin",
            PeriodLabel = "(01/25)",
            MainCurrencyCode = "USD",
            Result = new GoalContributingTransactionsResult.Supported(rows),
            StrategyUnit = unit
        };
    }

    private static ContributingTransactionRow CreateRow(
        decimal fiatAmount,
        long satsAmount,
        decimal runningTotal,
        DateOnly? date = null)
    {
        return new ContributingTransactionRow
        {
            Date = date ?? new DateOnly(2025, 1, 1),
            Description = "Contribution",
            AccountName = "Checking",
            CategoryName = null,
            FiatAmount = FiatValue.New(fiatAmount),
            FiatCurrencyCode = "USD",
            SatsAmount = BtcValue.ParseSats(satsAmount),
            RunningTotal = runningTotal
        };
    }

    private static async Task<GoalSummaryViewModel> BindAsync(GoalSummaryViewModel.Request request)
    {
        var vm = new GoalSummaryViewModel();
        vm.Parameter = request;
        await vm.OnBindParameterAsync();
        return vm;
    }

    [Test]
    public async Task FiatUnit_PositiveDeltas_FormatsRunningTotalsWithMainCurrency()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Fiat,
            CreateRow(100m, 0, 100m),
            CreateRow(150m, 0, 250m, date: new DateOnly(2025, 1, 2)));

        // Act
        var vm = await BindAsync(request);

        // Assert - 2dp fiat with the main currency, sign flags from RunningTotal deltas
        Assert.That(vm.HasRows, Is.True);
        Assert.That(vm.Rows, Has.Count.EqualTo(2));
        Assert.That(vm.Rows[0].IsContributionPositive, Is.True);
        Assert.That(vm.Rows[0].IsContributionNegative, Is.False);
        Assert.That(vm.Rows[1].IsContributionPositive, Is.True);
        Assert.That(vm.Rows[1].IsContributionNegative, Is.False);
        Assert.That(vm.Rows[0].RunningTotalFormatted, Is.EqualTo("$ 100.00"));
        Assert.That(vm.Rows[1].RunningTotalFormatted, Is.EqualTo("$ 250.00"));
        Assert.That(vm.FinalTotalFormatted, Is.EqualTo(vm.Rows[1].RunningTotalFormatted));
    }

    [Test]
    public async Task SatsUnit_NegativeDelta_FlagsNegativeAndFormatsGroupedSats()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Sats,
            CreateRow(0m, 5000, 5000m),
            CreateRow(0m, 0, 3000m, date: new DateOnly(2025, 1, 2)));

        // Act
        var vm = await BindAsync(request);

        // Assert - grouped sats ("3 000"), second-row delta is negative
        Assert.That(vm.Rows[0].IsContributionPositive, Is.True);
        Assert.That(vm.Rows[1].IsContributionNegative, Is.True);
        Assert.That(vm.Rows[1].IsContributionPositive, Is.False);
        Assert.That(vm.Rows[0].RunningTotalFormatted, Is.EqualTo("5 000"));
        Assert.That(vm.Rows[1].RunningTotalFormatted, Is.EqualTo("3 000"));
        Assert.That(vm.FinalTotalFormatted, Is.EqualTo("3 000"));
    }

    [Test]
    public async Task CountUnit_FormatsAsGroupedInteger()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Count,
            CreateRow(0m, 0, 1m),
            CreateRow(0m, 0, 2m, date: new DateOnly(2025, 1, 2)));

        // Act
        var vm = await BindAsync(request);

        // Assert
        Assert.That(vm.Rows[0].RunningTotalFormatted, Is.EqualTo(string.Format("{0:N0}", 1m)));
        Assert.That(vm.Rows[1].RunningTotalFormatted, Is.EqualTo(string.Format("{0:N0}", 2m)));
    }

    [Test]
    public async Task PercentageUnit_FormatsWithOneDecimal()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Percentage,
            CreateRow(0m, 0, 25.5m));

        // Act
        var vm = await BindAsync(request);

        // Assert
        Assert.That(vm.Rows[0].RunningTotalFormatted, Is.EqualTo(string.Format("{0:N1}%", 25.5m)));
        Assert.That(vm.FinalTotalFormatted, Is.EqualTo(vm.Rows[0].RunningTotalFormatted));
    }

    [Test]
    public async Task SatsOnlyRow_KeepsFiatCellEmpty()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Sats,
            CreateRow(0m, 5000, 5000m));

        // Act
        var vm = await BindAsync(request);

        // Assert - never a fabricated "0" in the fiat cell; sats cell shows grouped sats
        Assert.That(vm.Rows[0].FiatFormatted, Is.EqualTo(string.Empty));
        Assert.That(vm.Rows[0].SatsFormatted, Is.EqualTo($"{CurrencyDisplay.FormatSatsAsNumber(5000)} {language.SatsLabel}"));
    }

    [Test]
    public async Task FiatOnlyRow_KeepsSatsCellEmpty()
    {
        // Arrange
        var request = CreateRequest(
            GoalStrategyUnit.Fiat,
            CreateRow(100m, 0, 100m));

        // Act
        var vm = await BindAsync(request);

        // Assert - never a fabricated "0" in the sats cell
        Assert.That(vm.Rows[0].SatsFormatted, Is.EqualTo(string.Empty));
        Assert.That(vm.Rows[0].FiatFormatted, Is.EqualTo("$ 100.00"));
    }

    [Test]
    public async Task SupportedWithEmptyRows_HasNoRowsAndLeavesFinalTotalEmpty()
    {
        // Arrange
        var request = CreateRequest(GoalStrategyUnit.Fiat);

        // Act
        var vm = await BindAsync(request);

        // Assert
        Assert.That(vm.HasRows, Is.False);
        Assert.That(vm.Rows, Is.Empty);
        Assert.That(vm.FinalTotalFormatted, Is.EqualTo(string.Empty));
    }
}
