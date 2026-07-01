using System.Drawing;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Reports.Panels;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class BtcLoansPanelViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private ILocalDatabase _localDatabase = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private CurrencySettings _currencySettings = null!;
    private RatesState _ratesState = null!;
    private CustomBtcPriceState _customBtcPriceState = null!;
    private AccountsTotalState _accountsTotalState = null!;
    private ILogger<AccountsTotalState> _accountsTotalLogger = null!;
    private TestLogger<BtcLoansPanelViewModel> _logger = null!;
    private BtcLoansDashboardDTO _dashboardDto = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        WeakReferenceMessenger.Default.Reset();

        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _localDatabase = Substitute.For<ILocalDatabase>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _currencySettings = new CurrencySettings(_localDatabase, _notificationPublisher);
        _ratesState = new RatesState();
        _customBtcPriceState = new CustomBtcPriceState();
        _accountsTotalLogger = Substitute.For<ILogger<AccountsTotalState>>();
        _accountsTotalState = new AccountsTotalState(
            _currencySettings, _ratesState, _customBtcPriceState, _queryDispatcher, _accountsTotalLogger);
        _logger = new TestLogger<BtcLoansPanelViewModel>();
        _dashboardDto = BtcLoansDashboardDTO.Empty(100_000_000L);

        _queryDispatcher.DispatchAsync(Arg.Any<GetBtcLoansDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(_dashboardDto));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
        _localDatabase?.Dispose();
        _ratesState?.Dispose();
        _accountsTotalState?.Dispose();
    }

    private BtcLoansPanelViewModel CreateViewModel()
    {
        return new BtcLoansPanelViewModel(
            _queryDispatcher,
            _accountsTotalState,
            _ratesState,
            _customBtcPriceState,
            _currencySettings,
            _logger);
    }

    private void SeedWealth(long btcSats)
    {
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { { FiatCurrency.Usd.Code, 1m } };

        var summaries = new AccountSummariesDTO(new List<AccountSummaryDTO>
        {
            new(
                Id: "btc-account",
                Type: "btc",
                Name: "BTC Wallet",
                Visible: true,
                IconId: null,
                Unicode: '\0',
                Color: Color.Empty,
                Currency: null,
                CurrencyDisplayName: null,
                IsBtcAccount: true,
                FiatTotal: null,
                SatsTotal: btcSats,
                HasFutureTotal: false,
                FutureFiatTotal: null,
                FutureSatsTotal: null,
                GroupId: null,
                GroupName: null)
        });

        WeakReferenceMessenger.Default.Send(summaries);
    }

    [Test]
    public void Refresh_WhenNoActiveLoans_SetsIsVisibleFalse()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _dashboardDto = BtcLoansDashboardDTO.Empty(100_000_000L);

        // Act
        var vm = CreateViewModel();
        vm.Refresh();

        // Assert
        Assert.That(vm.IsVisible, Is.False);
        Assert.That(vm.IsLoading, Is.False);
    }

    [Test]
    public void Refresh_WhenRatesAreNull_HidesPanel()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _ratesState.FiatRates = null;
        _ratesState.BitcoinPrice = null;

        // Act
        var vm = CreateViewModel();
        vm.Refresh();

        // Assert
        Assert.That(vm.IsVisible, Is.False);
        Assert.That(vm.IsLoading, Is.False);
    }

    [Test]
    public void Refresh_WithActiveLoan_ProducesExpectedRows()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _dashboardDto = new BtcLoansDashboardDTO
        {
            HasActiveLoans = true,
            ActiveLoansCount = 1,
            CollateralPercentOfStack = 50m,
            DebtWeightedAvgLtv = 45m,
            DebtWeightedAvgApr = 8.5m,
            TotalBorrowedInMainCurrency = 20_000m,
            TotalDebtInMainCurrency = 22_000m,
            TotalDebtInBtc = 0.22m,
            HighestLtv = 45m,
            ClosestDistanceToLiquidationLtv = 40m,
            ClosestLoanName = "Loan A",
            WorstCaseLiquidationBtcPriceUsd = 30_000m,
            HealthyCount = 1,
            WarningCount = 0,
            DangerCount = 0,
            TotalCollateralSats = 50_000_000L,
            TotalCollateralFiatInMainCurrency = 50_000m,
            FreeBtcSats = 50_000_000L,
            TotalBtcStackSats = 100_000_000L,
            TotalAccruedInterestInMainCurrency = 500m,
            TotalFeesPaidInMainCurrency = 100m,
            NextRepaymentDate = new DateOnly(2025, 2, 1),
            DaysUntilNextRepayment = 17,
            NextRepaymentLoanName = "Loan A",
            AverageLoanAgeDays = 90m
        };

        // Act
        var vm = CreateViewModel();
        vm.Refresh();

        // Assert
        Assert.That(vm.IsVisible, Is.True);
        Assert.That(vm.IsLoading, Is.False);

        var leftTexts = vm.Data.Rows.Select(r => r.LeftText).ToList();
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_ActiveLoans));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_TotalDebt));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_TotalDebtBtc));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_TotalBorrowed));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_AvgLtv));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_AvgApr));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_CollateralFiat));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_CollateralSats));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_CollateralPercent));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_FreeBtc));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_HealthBreakdown));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_HighestLtv));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_ClosestDistance));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_WorstCaseLiquidationPrice));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_AccruedInterest));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_FeesPaid));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_AvgLoanAge));
        Assert.That(leftTexts, Has.Member(language.Reports_BtcLoans_NextRepayment));

        Assert.That(vm.Data.Rows.Count(r => r.IsSeparator), Is.EqualTo(4));

        var activeLoansRow = vm.Data.Rows.First(r => r.LeftText == language.Reports_BtcLoans_ActiveLoans);
        Assert.That(activeLoansRow.RightText, Is.EqualTo("1"));

        var nextRepaymentRow = vm.Data.Rows.First(r => r.LeftText == language.Reports_BtcLoans_NextRepayment);
        var expectedDate = new DateOnly(2025, 2, 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
        Assert.That(nextRepaymentRow.RightText, Is.EqualTo($"{expectedDate} (17 days)"));
    }

    [Test]
    public void Refresh_WhenQueryThrows_LogsErrorAndHidesPanel()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _queryDispatcher.DispatchAsync(Arg.Any<GetBtcLoansDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BtcLoansDashboardDTO>(new InvalidOperationException("boom")));

        // Act
        var vm = CreateViewModel();
        vm.Refresh();

        // Assert
        Assert.That(vm.IsVisible, Is.False);
        Assert.That(vm.IsLoading, Is.False);
        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        Assert.That(_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(_logger.Entries[0].Exception, Is.InstanceOf<InvalidOperationException>());
        Assert.That(_logger.Entries[0].Message, Does.Contain("Error updating BTC loans data"));
    }
}
