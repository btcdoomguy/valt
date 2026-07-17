using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

namespace Valt.Tests.UI.Screens.ChildViewModels;

[TestFixture]
public class TransferTransactionEditorViewModelTests : DatabaseTest
{
    private TransferTransactionEditorViewModel _viewModel;

    [SetUp]
    public new void SetUp()
    {
        var currencySettings = new CurrencySettings(_localDatabase, null!);
        _viewModel = new TransferTransactionEditorViewModel(
            currencySettings,
            Substitute.For<IFireAndForgetTaskRunner>(),
            Substitute.For<ILogger<TransferTransactionEditorViewModel>>());
        _viewModel.AvailableAccounts.Add(CreateFiatAccount("1", "BRL", FiatCurrency.Brl));
        _viewModel.AvailableAccounts.Add(CreateFiatAccount("2", "USD", FiatCurrency.Usd));
        _viewModel.AvailableAccounts.Add(CreateBtcAccount("3", "BTC"));
    }

    [Test]
    public void ShowTransferValueField_ShouldBeTrue_WhenAccountsHaveDifferentCurrencies()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.ToAccount = CreateFiatAccount("2", "USD", FiatCurrency.Usd);

        Assert.That(_viewModel.ShowTransferValueField, Is.True);
    }

    [Test]
    public void ShowTransferValueField_ShouldBeFalse_WhenAccountsHaveSameCurrency()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.ToAccount = CreateFiatAccount("2", "BRL2", FiatCurrency.Brl);

        Assert.That(_viewModel.ShowTransferValueField, Is.False);
    }

    [Test]
    public void Validate_ShouldFail_WhenToAccountMissing()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.True);
    }

    [Test]
    public void Validate_ShouldFail_WhenToFiatValueMissingForDifferentCurrencies()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ToAccount = CreateFiatAccount("2", "USD", FiatCurrency.Usd);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.True);
    }

    [Test]
    public void Validate_ShouldPass_WhenSameCurrencyTransfer()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ToAccount = CreateFiatAccount("2", "BRL2", FiatCurrency.Brl);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.False);
    }

    [Test]
    public async Task UpdateTransferRateAsync_ShouldCalculateRate_WhenFiatToFiat()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "BRL", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ToAccount = CreateFiatAccount("2", "USD", FiatCurrency.Usd);
        _viewModel.ToAccountFiatValue = FiatValue.New(500M);

        await _viewModel.UpdateTransferRateAsync();

        Assert.That(_viewModel.TransferRate, Is.Not.EqualTo("Calculating..."));
        Assert.That(_viewModel.TransferRate, Is.Not.Empty);
    }

    [Test]
    public async Task UpdateTransferRateAsync_ShouldCalculateRate_WhenBtcToFiat()
    {
        _viewModel.FromAccount = CreateBtcAccount("3", "BTC");
        _viewModel.FromAccountBtcValue = BtcValue.New(100_000_000);
        _viewModel.ToAccount = CreateFiatAccount("2", "USD", FiatCurrency.Usd);
        _viewModel.ToAccountFiatValue = FiatValue.New(50_000M);

        await _viewModel.UpdateTransferRateAsync();

        Assert.That(_viewModel.TransferRate, Is.Not.EqualTo("Calculating..."));
        Assert.That(_viewModel.TransferRate, Is.Not.Empty);
    }

    private static AccountDTO CreateFiatAccount(string id, string name, FiatCurrency currency)
    {
        return new AccountDTO(
            Id: id,
            Type: nameof(AccountTypes.Fiat),
            Name: name,
            CurrencyNickname: "",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: System.Drawing.Color.Empty,
            Currency: currency.Code,
            IsBtcAccount: false,
            InitialAmountFiat: 0,
            InitialAmountSats: null,
            GroupId: null);
    }

    private static AccountDTO CreateBtcAccount(string id, string name)
    {
        return new AccountDTO(
            Id: id,
            Type: nameof(AccountTypes.Bitcoin),
            Name: name,
            CurrencyNickname: "",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: System.Drawing.Color.Empty,
            Currency: null,
            IsBtcAccount: true,
            InitialAmountFiat: null,
            InitialAmountSats: 0,
            GroupId: null);
    }
}
