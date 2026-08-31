using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.ViewModels;

[TestFixture]
public class TransactionViewModelTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TransactionGridResources.InitializeForTesting();
    }

    [Test]
    public void FiatCurrencyCode_WhenFiatToBitcoin_ReturnsFromCurrency()
    {
        // Arrange - FiatToBitcoin: FromCurrency is the fiat (e.g., "USD")
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "USD",
            toCurrency: "BTC");

        // Assert
        Assert.That(vm.FiatCurrencyCode, Is.EqualTo("USD"));
    }

    [Test]
    public void FiatCurrencyCode_WhenBitcoinToFiat_ReturnsToCurrency()
    {
        // Arrange - BitcoinToFiat: ToCurrency is the fiat (e.g., "BRL")
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.BitcoinToFiat,
            fromCurrency: "BTC",
            toCurrency: "BRL");

        // Assert
        Assert.That(vm.FiatCurrencyCode, Is.EqualTo("BRL"));
    }

    [Test]
    public void FiatCurrencyCode_WhenFiatToFiat_ReturnsNull()
    {
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.FiatToFiat,
            fromCurrency: "USD",
            toCurrency: "EUR");

        Assert.That(vm.FiatCurrencyCode, Is.Null);
    }

    [Test]
    public void FiatCurrencyCode_WhenFiatOnly_ReturnsNull()
    {
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.Fiat,
            fromCurrency: "USD",
            toCurrency: null);

        Assert.That(vm.FiatCurrencyCode, Is.Null);
    }

    [Test]
    public void FiatCurrencyCode_WhenBitcoinOnly_ReturnsNull()
    {
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.Bitcoin,
            fromCurrency: "BTC",
            toCurrency: null);

        Assert.That(vm.FiatCurrencyCode, Is.Null);
    }

    [Test]
    public void FiatCurrencyCode_WhenBitcoinToBitcoin_ReturnsNull()
    {
        var vm = CreateTransactionViewModel(
            transferType: TransactionTransferTypes.BitcoinToBitcoin,
            fromCurrency: "BTC",
            toCurrency: "BTC");

        Assert.That(vm.FiatCurrencyCode, Is.Null);
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenValueIncreased_AppendsPositiveVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "USD",
            toCurrency: "BTC",
            fromAmountFiat: -1000m,
            toAmountSats: 10_000_000L);

        vm.RefreshCurrentAutoSatValue(currentUsdFiatRate: 1m, currentBtcRate: 50_000m, mainFiatCurrency: "USD");

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 5,000.00 (+400%)"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenValueDecreased_AppendsNegativeVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "USD",
            toCurrency: "BTC",
            fromAmountFiat: -1000m,
            toAmountSats: 10_000_000L);

        vm.RefreshCurrentAutoSatValue(currentUsdFiatRate: 1m, currentBtcRate: 5_000m, mainFiatCurrency: "USD");

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 500.00 (-50%)"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenNoFiatAmount_DoesNotAppendVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "USD",
            toCurrency: "BTC",
            fromAmountFiat: 0m,
            toAmountSats: 10_000_000L);

        vm.RefreshCurrentAutoSatValue(currentUsdFiatRate: 1m, currentBtcRate: 50_000m, mainFiatCurrency: "USD");

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 5,000.00"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenVarianceIsZero_DoesNotAppendVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "USD",
            toCurrency: "BTC",
            fromAmountFiat: -1000m,
            toAmountSats: 2_000_000L);

        vm.RefreshCurrentAutoSatValue(currentUsdFiatRate: 1m, currentBtcRate: 50_000m, mainFiatCurrency: "USD");

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 1,000.00"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenOriginalInDifferentCurrency_ConvertsBeforeCalculatingVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "EUR",
            toCurrency: "BTC",
            fromAmountFiat: -1000m,
            toAmountSats: 10_000_000L);

        vm.RefreshCurrentAutoSatValue(
            currentUsdFiatRate: 1m,
            currentBtcRate: 50_000m,
            mainFiatCurrency: "USD",
            fiatRates: new Dictionary<string, decimal> { ["EUR"] = 0.9m });

        // Converted original: 1000 EUR -> 1111.11 USD
        // Current value: 0.1 BTC * 50000 USD = 5000 USD
        // Variance: (5000 - 1111.11) / 1111.11 * 100 = +350%
        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 5,000.00 (+350%)"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenOriginalCurrencyConversionNotAvailable_DoesNotAppendVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.FiatToBitcoin,
            fromCurrency: "EUR",
            toCurrency: "BTC",
            fromAmountFiat: -1000m,
            toAmountSats: 10_000_000L);

        vm.RefreshCurrentAutoSatValue(
            currentUsdFiatRate: 1m,
            currentBtcRate: 50_000m,
            mainFiatCurrency: "USD",
            fiatRates: new Dictionary<string, decimal>());

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.EqualTo("$ 5,000.00"));
    }

    [Test]
    public void RefreshCurrentAutoSatValue_WhenProvisionalSats_DoesNotAppendVariance()
    {
        var vm = CreateTransactionViewModelForAutoSat(
            transferType: TransactionTransferTypes.Fiat,
            fromCurrency: "USD",
            toCurrency: null,
            fromAmountFiat: -1000m,
            toAmountSats: null);

        vm.RefreshCurrentAutoSatValue(
            currentUsdFiatRate: 1m,
            currentBtcRate: 50_000m,
            mainFiatCurrency: "USD",
            fiatRates: new Dictionary<string, decimal> { ["USD"] = 1m });

        Assert.That(vm.AutoSatAmountCurrentPrice, Is.Not.StartWith("$ 0"));
        Assert.That(vm.AutoSatAmountCurrentPrice, Does.Not.Contain("%"));
    }

    private static TransactionViewModel CreateTransactionViewModelForAutoSat(
        TransactionTransferTypes transferType,
        string? fromCurrency,
        string? toCurrency,
        decimal? fromAmountFiat,
        long? toAmountSats)
    {
        return new TransactionViewModel(
            id: "test-id",
            date: new DateOnly(2024, 1, 15),
            name: "Test Transaction",
            categoryId: "cat-1",
            categoryName: "Test Category",
            categoryIcon: Icon.Empty,
            fromAccountId: "acc-1",
            fromAccountName: "From Account",
            fromAccountIcon: Icon.Empty,
            toAccountId: "acc-2",
            toAccountName: "To Account",
            toAccountIcon: Icon.Empty,
            formattedFromAmount: fromAmountFiat.HasValue ? $"-{fromAmountFiat.Value:F2}" : null,
            fromAmountSats: null,
            fromAmountFiat: fromAmountFiat,
            formattedToAmount: toAmountSats.HasValue ? $"{toAmountSats.Value / 100_000_000m:F8} BTC" : null,
            toAmountSats: toAmountSats,
            toAmountFiat: null,
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

    private static TransactionViewModel CreateTransactionViewModel(
        TransactionTransferTypes transferType,
        string? fromCurrency,
        string? toCurrency)
    {
        return new TransactionViewModel(
            id: "test-id",
            date: new DateOnly(2024, 1, 15),
            name: "Test Transaction",
            categoryId: "cat-1",
            categoryName: "Test Category",
            categoryIcon: Icon.Empty,
            fromAccountId: "acc-1",
            fromAccountName: "From Account",
            fromAccountIcon: Icon.Empty,
            toAccountId: "acc-2",
            toAccountName: "To Account",
            toAccountIcon: Icon.Empty,
            formattedFromAmount: "-$100",
            fromAmountSats: null,
            fromAmountFiat: -100m,
            formattedToAmount: "$100",
            toAmountSats: null,
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
}
