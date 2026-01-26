using System.Drawing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class TransactionListViewModelAvgPriceTests : DatabaseTest
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TransactionGridResources.InitializeForTesting();
    }

    private ILocalStorageService _localStorageService = null!;
    private IModalFactory _modalFactory = null!;
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private ITransactionTermService _transactionTermService = null!;
    private FilterState _filterState = null!;
    private IClock _clock = null!;
    private ILogger<TransactionListViewModel> _vmLogger = null!;

    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        _localStorageService = Substitute.For<ILocalStorageService>();
        _localStorageService.LoadDataGridSettings().Returns(new DataGridSettings());

        _modalFactory = Substitute.For<IModalFactory>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _transactionTermService = Substitute.For<ITransactionTermService>();
        _filterState = new FilterState();
        _clock = Substitute.For<IClock>();
        _vmLogger = Substitute.For<ILogger<TransactionListViewModel>>();
    }

    private TransactionListViewModel CreateViewModel()
    {
        var currencySettings = new CurrencySettings(_localDatabase, null!);
        var ratesState = new RatesState();
        var liveRateState = new LiveRateState(
            currencySettings,
            _localDatabase,
            _priceDatabase,
            Substitute.For<ILocalHistoricalPriceProvider>(),
            ratesState,
            Substitute.For<ILogger<LiveRateState>>());

        return new TransactionListViewModel(
            _modalFactory,
            _commandDispatcher,
            _queryDispatcher,
            _transactionTermService,
            liveRateState,
            _localDatabase,
            currencySettings,
            _filterState,
            _clock,
            _localStorageService,
            _vmLogger);
    }

    #region CanSendToAvgPrice Tests

    [Test]
    public void CanSendToAvgPrice_WhenNoTransactionSelected_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = null;

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    [Test]
    public void CanSendToAvgPrice_WhenFiatTransaction_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.Fiat, "USD", null);
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    [Test]
    public void CanSendToAvgPrice_WhenBitcoinTransaction_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.Bitcoin, "BTC", null);
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    [Test]
    public void CanSendToAvgPrice_WhenFiatToFiatTransaction_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.FiatToFiat, "USD", "EUR");
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    [Test]
    public void CanSendToAvgPrice_WhenFiatToBitcoin_NoMatchingProfiles_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.FiatToBitcoin, "USD", "BTC");
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>();
        vm.IsSingleItemSelected = true;

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    [Test]
    public void CanSendToAvgPrice_WhenFiatToBitcoin_WithMatchingProfiles_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.FiatToBitcoin, "USD", "BTC");
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>
        {
            CreateProfileDTO("BTC", "USD")
        };
        vm.IsSingleItemSelected = true;

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.True);
    }

    [Test]
    public void CanSendToAvgPrice_WhenBitcoinToFiat_WithMatchingProfiles_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.BitcoinToFiat, "BTC", "BRL");
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>
        {
            CreateProfileDTO("BTC", "BRL")
        };
        vm.IsSingleItemSelected = true;

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.True);
    }

    [Test]
    public void CanSendToAvgPrice_WhenMultipleItemsSelected_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTransaction = CreateTransactionViewModel(TransactionTransferTypes.FiatToBitcoin, "USD", "BTC");
        vm.MatchingAvgPriceProfiles = new List<AvgPriceProfileDTO>
        {
            CreateProfileDTO("BTC", "USD")
        };
        vm.IsSingleItemSelected = false; // Multiple items selected

        // Assert
        Assert.That(vm.CanSendToAvgPrice, Is.False);
    }

    #endregion

    private static TransactionViewModel CreateTransactionViewModel(
        TransactionTransferTypes transferType,
        string? fromCurrency,
        string? toCurrency)
    {
        return new TransactionViewModel(
            id: "test-id",
            date: new DateOnly(2024, 1, 15),
            name: "Test",
            categoryId: "cat-1",
            categoryName: "Test",
            categoryIcon: Icon.Empty,
            fromAccountId: "acc-1",
            fromAccountName: "From",
            fromAccountIcon: Icon.Empty,
            toAccountId: "acc-2",
            toAccountName: "To",
            toAccountIcon: Icon.Empty,
            formattedFromAmount: "-$100",
            fromAmountSats: -5000000,
            fromAmountFiat: -100m,
            formattedToAmount: "$100",
            toAmountSats: 5000000,
            toAmountFiat: 100m,
            fromCurrency: fromCurrency,
            toCurrency: toCurrency,
            transferType: transferType,
            transactionType: TransactionTypes.Transfer,
            autoSatAmount: null,
            fixedExpenseRecordId: null,
            fixedExpenseId: null,
            fixedExpenseName: null,
            fixedExpenseReferenceDate: null,
            notes: null,
            futureTransaction: false);
    }

    private static AvgPriceProfileDTO CreateProfileDTO(string assetName, string currencyCode, string name = "Test Profile")
    {
        return new AvgPriceProfileDTO(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            AssetName: assetName,
            Precision: 8,
            Visible: true,
            Icon: null,
            Unicode: '\uf15a',
            Color: Color.FromArgb(0xFFA500), // Orange
            CurrencyCode: currencyCode,
            AvgPriceCalculationMethodId: 0);
    }
}
