using NUnit.Framework;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

namespace Valt.Tests.UI.Screens.ChildViewModels;

[TestFixture]
public class DebtTransactionEditorViewModelTests
{
    private DebtTransactionEditorViewModel _viewModel;

    [SetUp]
    public void SetUp()
    {
        _viewModel = new DebtTransactionEditorViewModel();
        _viewModel.AvailableAccounts.Add(CreateFiatAccount("1", "Fiat", FiatCurrency.Brl));
        _viewModel.AvailableAccounts.Add(CreateBtcAccount("2", "BTC"));
    }

    [Test]
    public void ShowInstallmentsOption_ShouldBeTrue_WhenAdding()
    {
        _viewModel.IsEditing = false;
        Assert.That(_viewModel.ShowInstallmentsOption, Is.True);
    }

    [Test]
    public void ShowInstallmentsOption_ShouldBeFalse_WhenEditing()
    {
        _viewModel.IsEditing = true;
        Assert.That(_viewModel.ShowInstallmentsOption, Is.False);
    }

    [Test]
    public void ShowInstallmentCount_ShouldReflectUseInstallmentsAndAddingMode()
    {
        _viewModel.IsEditing = false;
        _viewModel.UseInstallments = true;
        Assert.That(_viewModel.ShowInstallmentCount, Is.True);
    }

    [Test]
    public void InstallmentValueText_ShouldCalculateForFiat()
    {
        _viewModel.IsEditing = false;
        _viewModel.UseInstallments = true;
        _viewModel.InstallmentCount = 3;
        _viewModel.FromAccount = CreateFiatAccount("1", "Fiat", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(300M);

        Assert.That(_viewModel.InstallmentValueText, Is.EqualTo("3x of 300.00"));
    }

    [Test]
    public void InstallmentValueText_ShouldCalculateForBtc()
    {
        _viewModel.IsEditing = false;
        _viewModel.UseInstallments = true;
        _viewModel.InstallmentCount = 2;
        _viewModel.FromAccount = CreateBtcAccount("2", "BTC");
        _viewModel.FromAccountBtcValue = BtcValue.New(1000);

        Assert.That(_viewModel.InstallmentValueText, Is.EqualTo("2x of 1000"));
    }

    [Test]
    public void Validate_ShouldFail_WhenFromAccountMissing()
    {
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.True);
    }

    [Test]
    public void Validate_ShouldFail_WhenFromFiatValueMissing()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "Fiat", FiatCurrency.Brl);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.True);
    }

    [Test]
    public void Validate_ShouldFail_WhenFromBtcValueMissing()
    {
        _viewModel.FromAccount = CreateBtcAccount("2", "BTC");
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.True);
    }

    [Test]
    public void Validate_ShouldPass_WhenFromFiatValueProvided()
    {
        _viewModel.FromAccount = CreateFiatAccount("1", "Fiat", FiatCurrency.Brl);
        _viewModel.FromAccountFiatValue = FiatValue.New(100M);
        _viewModel.ValidateAllProperties();

        Assert.That(_viewModel.HasErrors, Is.False);
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
