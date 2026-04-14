using System.Reflection;
using Avalonia.Media;
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Settings;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.FixedExpenseOverview;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class FixedExpenseOverviewViewModelTests : DatabaseTest
{
    private IFixedExpenseProvider _fixedExpenseProvider = null!;
    private RatesState _ratesState = null!;
    private CurrencySettings _currencySettings = null!;
    private IClock _clock = null!;
    private SecureModeState _secureModeState = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
        InitializeFixedExpenseListResources();
    }

    private static void InitializeFixedExpenseListResources()
    {
        var type = typeof(FixedExpenseListResources);
        var flags = BindingFlags.Static | BindingFlags.NonPublic;

        type.GetField("_defaultForegroundResource", flags)!.SetValue(null, new SolidColorBrush(Colors.Gray));
        type.GetField("_paidForegroundResource", flags)!.SetValue(null, new SolidColorBrush(Colors.Green));
        type.GetField("_ignoredForegroundResource", flags)!.SetValue(null, new SolidColorBrush(Colors.DarkGray));
        type.GetField("_lateForegroundResource", flags)!.SetValue(null, new SolidColorBrush(Colors.Red));
        type.GetField("_warningForegroundResource", flags)!.SetValue(null, new SolidColorBrush(Colors.Yellow));
    }

    [SetUp]
    public new void SetUp()
    {
        _fixedExpenseProvider = Substitute.For<IFixedExpenseProvider>();
        _ratesState = new RatesState();
        _ratesState.FiatRates = new Dictionary<string, decimal> { { "BRL", 5.0m }, { "USD", 1.0m } };
        _ratesState.BitcoinPrice = 100000m;
        _currencySettings = new CurrencySettings(_localDatabase, null!);
        _clock = Substitute.For<IClock>();
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2025, 6, 15));
        _secureModeState = new SecureModeState();

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(Arg.Any<DateOnly>())
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new List<FixedExpenseProviderEntry>()));
    }

    [TearDown]
    public void TearDown()
    {
        _ratesState?.Dispose();
    }

    private FixedExpenseOverviewViewModel CreateViewModel()
    {
        var vm = new FixedExpenseOverviewViewModel(
            _fixedExpenseProvider,
            _ratesState,
            _currencySettings,
            _clock,
            _secureModeState);
        return vm;
    }

    #region Initialization Tests

    [Test]
    public async Task Should_Initialize_With_CurrentYear()
    {
        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.SelectedYear, Is.EqualTo(2025));
    }

    [Test]
    public async Task Should_Initialize_With_YearRange()
    {
        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - should have years from current-2 to current+1
        Assert.That(vm.AvailableYears, Does.Contain(2023));
        Assert.That(vm.AvailableYears, Does.Contain(2024));
        Assert.That(vm.AvailableYears, Does.Contain(2025));
        Assert.That(vm.AvailableYears, Does.Contain(2026));
    }

    [Test]
    public async Task Should_Initialize_With_12_MonthGroups()
    {
        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups, Has.Count.EqualTo(12));
    }

    [Test]
    public async Task Should_Call_Provider_For_Each_Month()
    {
        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - should have called GetFixedExpensesOfMonthAsync for all 12 months of 2025
        for (var month = 1; month <= 12; month++)
        {
            await _fixedExpenseProvider.Received().GetFixedExpensesOfMonthAsync(
                new DateOnly(2025, month, 1));
        }
    }

    #endregion

    #region Status Mapping Tests

    [Test]
    public async Task Should_Map_Paid_Status_Correctly()
    {
        // Arrange
        var entries = new List<FixedExpenseProviderEntry>
        {
            new(new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
                null, 1000m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Paid, new TransactionId())
        };
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(entries));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        var januaryGroup = vm.MonthGroups[0];
        Assert.That(januaryGroup.Entries, Has.Count.EqualTo(1));
        Assert.That(januaryGroup.Entries[0].StatusColor.Color, Is.EqualTo(FixedExpenseListResources.PaidForeground.Color));
    }

    [Test]
    public async Task Should_Map_Ignored_Status_Correctly()
    {
        // Arrange
        var entries = new List<FixedExpenseProviderEntry>
        {
            new(new FixedExpenseId().Value, "Insurance", new CategoryId(), new DateOnly(2025, 3, 10),
                null, 200m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Ignored, null)
        };
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 3, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(entries));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        var marchGroup = vm.MonthGroups[2];
        Assert.That(marchGroup.Entries, Has.Count.EqualTo(1));
        Assert.That(marchGroup.Entries[0].StatusColor.Color, Is.EqualTo(FixedExpenseListResources.IgnoredForeground.Color));
    }

    [Test]
    public async Task Should_Map_Pending_Status_Correctly()
    {
        // Arrange
        var entries = new List<FixedExpenseProviderEntry>
        {
            new(new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 7, 5),
                null, 1000m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null)
        };
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 7, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(entries));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        var julyGroup = vm.MonthGroups[6];
        Assert.That(julyGroup.Entries, Has.Count.EqualTo(1));
        Assert.That(julyGroup.Entries[0].StatusColor.Color, Is.EqualTo(FixedExpenseListResources.DefaultForeground.Color));
    }

    #endregion

    #region Out-of-Range Detection Tests

    [Test]
    public async Task Should_Detect_OutOfRange_When_Actual_Exceeds_FixedAmount()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        entry.Pay("tx1", -1200m); // Actual is different from fixed amount (negative = outflow)

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        var januaryEntry = vm.MonthGroups[0].Entries[0];
        Assert.That(januaryEntry.IsOutOfRange, Is.True);
        Assert.That(januaryEntry.ActualAmountColor.Color, Is.EqualTo(FixedExpenseListResources.WarningForeground.Color));
    }

    [Test]
    public async Task Should_Not_Flag_OutOfRange_When_Actual_Matches_FixedAmount()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        entry.Pay("tx1", -1000m);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        var januaryEntry = vm.MonthGroups[0].Entries[0];
        Assert.That(januaryEntry.IsOutOfRange, Is.False);
        Assert.That(januaryEntry.ActualAmountColor.Color, Is.EqualTo(FixedExpenseListResources.DefaultForeground.Color));
    }

    [Test]
    public async Task Should_Detect_OutOfRange_When_Actual_Below_RangedMin()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Electricity", new CategoryId(), new DateOnly(2025, 1, 30),
            null, null, 100m, 200m, FiatCurrency.Usd.Code);
        entry.Pay("tx1", -50m); // Below min

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].Entries[0].IsOutOfRange, Is.True);
    }

    [Test]
    public async Task Should_Detect_OutOfRange_When_Actual_Above_RangedMax()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Electricity", new CategoryId(), new DateOnly(2025, 1, 30),
            null, null, 100m, 200m, FiatCurrency.Usd.Code);
        entry.Pay("tx1", -250m); // Above max

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].Entries[0].IsOutOfRange, Is.True);
    }

    [Test]
    public async Task Should_Not_Flag_OutOfRange_When_Actual_Within_Range()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Electricity", new CategoryId(), new DateOnly(2025, 1, 30),
            null, null, 100m, 200m, FiatCurrency.Usd.Code);
        entry.Pay("tx1", -150m); // Within range

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].Entries[0].IsOutOfRange, Is.False);
    }

    #endregion

    #region Per-Month Totals Tests

    [Test]
    public async Task Should_Calculate_MonthPaidTotal_From_Paid_Entries()
    {
        // Arrange
        var entry1 = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        entry1.Pay("tx1", -1000m);

        var entry2 = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Internet", new CategoryId(), new DateOnly(2025, 1, 10),
            null, 80m, null, null, FiatCurrency.Usd.Code);
        entry2.Pay("tx2", -80m);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry1, entry2 }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - MonthPaidTotal should be formatted for $1080
        var januaryGroup = vm.MonthGroups[0];
        Assert.That(januaryGroup.MonthPaidTotal, Does.Contain("1"));
        Assert.That(januaryGroup.MonthPaidTotal, Does.Contain("080"));
    }

    [Test]
    public async Task Should_Calculate_MonthExpectedRange_Excluding_Ignored()
    {
        // Arrange
        var entry1 = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);

        var ignoredEntry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Insurance", new CategoryId(), new DateOnly(2025, 1, 15),
            null, 500m, null, null, FiatCurrency.Usd.Code);
        ignoredEntry.Ignore();

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry1, ignoredEntry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - MonthExpectedRange should only include the non-ignored entry ($1000)
        var januaryGroup = vm.MonthGroups[0];
        Assert.That(januaryGroup.MonthExpectedRange, Does.Contain("1"));
        Assert.That(januaryGroup.MonthExpectedRange, Does.Contain("000"));
        Assert.That(januaryGroup.MonthExpectedRange, Does.Not.Contain("500"));
    }

    [Test]
    public async Task Should_Show_EmptyMonth_HasEntries_False()
    {
        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - all months should have HasEntries = false
        foreach (var group in vm.MonthGroups)
            Assert.That(group.HasEntries, Is.False);
    }

    #endregion

    #region Footer Totals Tests

    [Test]
    public async Task Should_Calculate_FutureExpensesTotal_Excluding_Ignored()
    {
        // Arrange - current date is June 15, 2025
        // Future entry (empty, in July)
        var futureEntry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 7, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);

        // Ignored future entry (should NOT count)
        var ignoredFutureEntry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Insurance", new CategoryId(), new DateOnly(2025, 7, 10),
            null, 500m, null, null, FiatCurrency.Usd.Code);
        ignoredFutureEntry.Ignore();

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 7, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(
                new[] { futureEntry, ignoredFutureEntry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - future total should be $1000, not $1500
        Assert.That(vm.FutureExpensesTotal, Does.Contain("1"));
        Assert.That(vm.FutureExpensesTotal, Does.Contain("000"));
    }

    [Test]
    public async Task Should_Calculate_PaidTotal_From_All_Paid_Entries()
    {
        // Arrange
        var paidEntry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        paidEntry.Pay("tx1", 1000m);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { paidEntry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.PaidTotal, Does.Contain("1"));
        Assert.That(vm.PaidTotal, Does.Contain("000"));
    }

    #endregion

    #region Secure Mode Tests

    [Test]
    public async Task Should_Mask_Totals_When_SecureMode_Enabled()
    {
        // Arrange
        _secureModeState.IsEnabled = true;

        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        entry.Pay("tx1", 1000m);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.PaidTotal, Is.EqualTo("---"));
        Assert.That(vm.FutureExpensesTotal, Is.EqualTo("---"));
    }

    [Test]
    public async Task Should_Mask_MonthTotals_When_SecureMode_Enabled()
    {
        // Arrange
        _secureModeState.IsEnabled = true;

        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);
        entry.Pay("tx1", 1000m);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].MonthPaidTotal, Is.EqualTo("---"));
        Assert.That(vm.MonthGroups[0].MonthExpectedRange, Is.EqualTo("---"));
    }

    #endregion

    #region Year Change Tests

    [Test]
    public async Task Should_Reload_Data_When_Year_Changes()
    {
        // Arrange
        var vm = CreateViewModel();
        await Task.Delay(200);

        _fixedExpenseProvider.ClearReceivedCalls();

        // Act
        vm.SelectedYear = 2024;
        await Task.Delay(200);

        // Assert - should have called provider for all 12 months of 2024
        for (var month = 1; month <= 12; month++)
        {
            await _fixedExpenseProvider.Received().GetFixedExpensesOfMonthAsync(
                new DateOnly(2024, month, 1));
        }
    }

    #endregion

    #region Entry Display Tests

    [Test]
    public async Task Should_Format_DayFormatted_With_Leading_Zero()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].Entries[0].DayFormatted, Is.EqualTo("05"));
    }

    [Test]
    public async Task Should_Show_Dash_For_Unpaid_ActualAmount()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5),
            null, 1000m, null, null, FiatCurrency.Usd.Code);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert
        Assert.That(vm.MonthGroups[0].Entries[0].ActualAmount, Is.EqualTo("-"));
    }

    [Test]
    public async Task Should_Format_ExpectedAmount_For_RangedEntry()
    {
        // Arrange
        var entry = new FixedExpenseProviderEntry(
            new FixedExpenseId().Value, "Electricity", new CategoryId(), new DateOnly(2025, 1, 30),
            null, null, 100m, 200m, FiatCurrency.Usd.Code);

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1))
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new[] { entry }));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - should contain both min and max formatted values with separator
        var expectedAmount = vm.MonthGroups[0].Entries[0].ExpectedAmount;
        Assert.That(expectedAmount, Does.Contain("100"));
        Assert.That(expectedAmount, Does.Contain("200"));
        Assert.That(expectedAmount, Does.Contain("-"));
    }

    #endregion
}
