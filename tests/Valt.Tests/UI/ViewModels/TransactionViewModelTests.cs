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
