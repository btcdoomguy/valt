using System.Drawing;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.ViewModels;

[TestFixture]
public class AccountViewModelTests
{
    #region IsHidden Property Tests

    [Test]
    public void Should_Return_IsHidden_False_When_Account_Is_Visible()
    {
        // Arrange
        var viewModel = new AccountViewModel(
            id: "test-id",
            type: "Fiat",
            name: "Test Account",
            visible: true,
            icon: null,
            currency: "USD",
            currencyDisplayName: "USD",
            isBtcAccount: false,
            fiatTotal: 1000m,
            satsTotal: null,
            hasFutureTotal: false,
            futureFiatTotal: null,
            futureSatsTotal: null);

        // Act & Assert
        Assert.That(viewModel.IsHidden, Is.False);
        Assert.That(viewModel.Visible, Is.True);
    }

    [Test]
    public void Should_Return_IsHidden_True_When_Account_Is_Not_Visible()
    {
        // Arrange
        var viewModel = new AccountViewModel(
            id: "test-id",
            type: "Fiat",
            name: "Test Account",
            visible: false,
            icon: null,
            currency: "USD",
            currencyDisplayName: "USD",
            isBtcAccount: false,
            fiatTotal: 1000m,
            satsTotal: null,
            hasFutureTotal: false,
            futureFiatTotal: null,
            futureSatsTotal: null);

        // Act & Assert
        Assert.That(viewModel.IsHidden, Is.True);
        Assert.That(viewModel.Visible, Is.False);
    }

    [Test]
    public void Should_Return_IsHidden_True_From_DTO_When_Account_Is_Not_Visible()
    {
        // Arrange
        var dto = new AccountSummaryDTO(
            Id: "test-id",
            Type: "Fiat",
            Name: "Test Account",
            Visible: false,
            IconId: null,
            Unicode: '\0',
            Color: Color.White,
            Currency: "USD",
            CurrencyDisplayName: "USD",
            IsBtcAccount: false,
            FiatTotal: 1000m,
            SatsTotal: null,
            HasFutureTotal: false,
            FutureFiatTotal: null,
            FutureSatsTotal: null,
            GroupId: null,
            GroupName: null);

        var viewModel = new AccountViewModel(dto);

        // Act & Assert
        Assert.That(viewModel.IsHidden, Is.True);
        Assert.That(viewModel.Visible, Is.False);
    }

    [Test]
    public void Should_Return_IsHidden_False_From_DTO_When_Account_Is_Visible()
    {
        // Arrange
        var dto = new AccountSummaryDTO(
            Id: "test-id",
            Type: "Fiat",
            Name: "Test Account",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: Color.White,
            Currency: "USD",
            CurrencyDisplayName: "USD",
            IsBtcAccount: false,
            FiatTotal: 1000m,
            SatsTotal: null,
            HasFutureTotal: false,
            FutureFiatTotal: null,
            FutureSatsTotal: null,
            GroupId: null,
            GroupName: null);

        var viewModel = new AccountViewModel(dto);

        // Act & Assert
        Assert.That(viewModel.IsHidden, Is.False);
        Assert.That(viewModel.Visible, Is.True);
    }

    #endregion
}
