using System.Drawing;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetVisibleAssets;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Assets;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Reports.Panels;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

public sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}

[TestFixture]
public class LeveragePositionsPanelViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private ILocalDatabase _localDatabase = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private CurrencySettings _currencySettings = null!;
    private RatesState _ratesState = null!;
    private CustomBtcPriceState _customBtcPriceState = null!;
    private AccountsTotalState _accountsTotalState = null!;
    private ILogger<AccountsTotalState> _accountsTotalLogger = null!;
    private TestLogger<LeveragePositionsPanelViewModel> _logger = null!;
    private List<AssetDTO> _assets = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        TransactionGridResources.InitializeForTesting();

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
        _logger = new TestLogger<LeveragePositionsPanelViewModel>();
        _assets = new List<AssetDTO>();

        _queryDispatcher.DispatchAsync(Arg.Any<GetVisibleAssetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<IReadOnlyList<AssetDTO>>(_assets.ToList()));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
        _localDatabase?.Dispose();
        _ratesState?.Dispose();
        _accountsTotalState?.Dispose();
    }

    private LeveragePositionsPanelViewModel CreateViewModel()
    {
        return new LeveragePositionsPanelViewModel(
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

    private AssetDTO CreateLeveragedPosition(
        bool isLong,
        decimal? collateral,
        decimal? leverage,
        decimal? entryPrice,
        decimal? contractCount = null,
        decimal? contractSizeUsd = null,
        int? collateralAssetTypeId = null,
        decimal? pnl = null,
        string currencyCode = "USD")
    {
        return new AssetDTO
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Leveraged Position",
            AssetTypeId = (int)AssetTypes.LeveragedPosition,
            AssetTypeName = "Leveraged Position",
            Icon = string.Empty,
            IncludeInNetWorth = true,
            Visible = true,
            LastPriceUpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            DisplayOrder = 0,
            CurrentPrice = 0,
            CurrentValue = 0,
            CurrencyCode = currencyCode,
            IsLong = isLong,
            Collateral = collateral,
            Leverage = leverage,
            EntryPrice = entryPrice,
            CollateralAssetTypeId = collateralAssetTypeId,
            ContractCount = contractCount,
            ContractSizeUsd = contractSizeUsd,
            PnL = pnl
        };
    }

    [Test]
    public async Task Refresh_WhenNoLeveragedPositions_SetsIsVisibleFalse()
    {
        // Arrange
        SeedWealth(100_000_000L);

        // Act
        var vm = CreateViewModel();
        await vm.RefreshAsync();

        // Assert
        Assert.That(vm.IsVisible, Is.False);
        Assert.That(vm.IsLoading, Is.False);
    }

    [Test]
    public async Task Refresh_WithBtcCollateralLongPosition_ProducesExpectedRows()
    {
        // Arrange
        SeedWealth(100_000_000L); // 1 BTC
        _assets.Add(CreateLeveragedPosition(
            isLong: true,
            collateral: 1m,
            leverage: null,
            entryPrice: 50_000m,
            contractCount: 1m,
            contractSizeUsd: 50_000m,
            collateralAssetTypeId: (int)LeveragedPositionCollateralAssetType.Btc,
            pnl: 1_000m));

        // Act
        var vm = CreateViewModel();
        vm.AllTimeHighFiatValue = 200_000m;
        await vm.RefreshAsync();

        // Assert
        Assert.That(vm.IsVisible, Is.True);
        Assert.That(vm.IsLoading, Is.False);
        Assert.That(vm.Data.Rows, Has.Count.EqualTo(7));

        Assert.That(vm.Data.Rows[0].LeftText, Is.EqualTo(language.Reports_LeveragePositions_LeveragedStack));
        Assert.That(vm.Data.Rows[0].RightText, Is.EqualTo("2.00 000 000 BTC"));

        Assert.That(vm.Data.Rows[1].LeftText, Is.EqualTo(language.Reports_LeveragePositions_LeverageExposure));
        Assert.That(vm.Data.Rows[1].RightText, Is.EqualTo("+1 BTC"));

        Assert.That(vm.Data.Rows[2].LeftText, Is.EqualTo(language.Reports_LeveragePositions_LeveragePercentage));
        Assert.That(vm.Data.Rows[2].RightText, Is.EqualTo("50%"));

        Assert.That(vm.Data.Rows[3].LeftText, Is.EqualTo(language.Reports_LeveragePositions_PositionCount));
        Assert.That(vm.Data.Rows[3].RightText, Is.EqualTo("1"));

        Assert.That(vm.Data.Rows[4].LeftText, Is.EqualTo(language.Reports_LeveragePositions_CurrentResult));
        Assert.That(vm.Data.Rows[4].RightText, Is.EqualTo("+$ 1,000.00"));

        Assert.That(vm.Data.Rows[5].LeftText, Is.EqualTo(language.Reports_LeveragePositions_CurrentResultBtc));
        Assert.That(vm.Data.Rows[5].RightText, Is.EqualTo("+1 000 000 BTC"));

        Assert.That(vm.Data.Rows[6].LeftText, Is.EqualTo(language.Reports_LeveragePositions_BtcPriceToHitAth));
        Assert.That(vm.Data.Rows[6].RightText, Is.EqualTo("$ 100,000.00"));
    }

    [Test]
    public async Task Refresh_WithFiatCollateralPosition_FormatsPnlCorrectly()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _assets.Add(CreateLeveragedPosition(
            isLong: true,
            collateral: 10_000m,
            leverage: 2m,
            entryPrice: 50_000m,
            pnl: -500m));

        // Act
        var vm = CreateViewModel();
        await vm.RefreshAsync();

        // Assert
        Assert.That(vm.IsVisible, Is.True);
        var currentResultRow = vm.Data.Rows.First(r => r.LeftText == language.Reports_LeveragePositions_CurrentResult);
        Assert.That(currentResultRow.RightText, Is.EqualTo("$ -500.00"));
    }

    [Test]
    public async Task Refresh_WithCustomBtcPrice_UsesSimulatedPnl()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _assets.Add(CreateLeveragedPosition(
            isLong: true,
            collateral: 1m,
            leverage: null,
            entryPrice: 50_000m,
            contractCount: 1m,
            contractSizeUsd: 50_000m,
            collateralAssetTypeId: (int)LeveragedPositionCollateralAssetType.Btc,
            pnl: 1_000m));

        _customBtcPriceState.CustomBtcPriceUsd = 200_000m;

        // Act
        var vm = CreateViewModel();
        await vm.RefreshAsync();

        // Assert
        var currentResultRow = vm.Data.Rows.First(r => r.LeftText == language.Reports_LeveragePositions_CurrentResult);
        Assert.That(currentResultRow.RightText, Is.EqualTo("+$ 300,000.00"));
    }

    [Test]
    public async Task Refresh_WhenQueryThrows_LogsErrorAndHidesPanel()
    {
        // Arrange
        SeedWealth(100_000_000L);
        _queryDispatcher.DispatchAsync(Arg.Any<GetVisibleAssetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<AssetDTO>>(new InvalidOperationException("boom")));

        // Act
        var vm = CreateViewModel();
        await vm.RefreshAsync();

        // Assert
        Assert.That(vm.IsVisible, Is.False);
        Assert.That(vm.IsLoading, Is.False);
        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        Assert.That(_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(_logger.Entries[0].Exception, Is.InstanceOf<InvalidOperationException>());
        Assert.That(_logger.Entries[0].Message, Does.Contain("Error updating leverage positions data"));
    }
}
