using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Modules.Assets;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class AssetDetailsSerializerTests
{
    #region BtcLoan Round-Trip Tests

    [Test]
    public void Should_RoundTrip_BtcLoanDetails()
    {
        var original = new BtcLoanDetails(
            platformName: "HodlHodl",
            collateralSats: 100_000_000,
            loanAmount: 25_000m,
            currencyCode: "USD",
            apr: 0.12m,
            initialLtv: 50m,
            liquidationLtv: 80m,
            marginCallLtv: 70m,
            fees: 150m,
            loanStartDate: new DateOnly(2025, 3, 15),
            repaymentDate: new DateOnly(2026, 3, 15),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.PlatformName, Is.EqualTo(original.PlatformName));
            Assert.That(deserialized.CollateralSats, Is.EqualTo(original.CollateralSats));
            Assert.That(deserialized.LoanAmount, Is.EqualTo(original.LoanAmount));
            Assert.That(deserialized.CurrencyCode, Is.EqualTo(original.CurrencyCode));
            Assert.That(deserialized.Apr, Is.EqualTo(original.Apr));
            Assert.That(deserialized.InitialLtv, Is.EqualTo(original.InitialLtv));
            Assert.That(deserialized.LiquidationLtv, Is.EqualTo(original.LiquidationLtv));
            Assert.That(deserialized.MarginCallLtv, Is.EqualTo(original.MarginCallLtv));
            Assert.That(deserialized.Fees, Is.EqualTo(original.Fees));
            Assert.That(deserialized.LoanStartDate, Is.EqualTo(original.LoanStartDate));
            Assert.That(deserialized.RepaymentDate, Is.EqualTo(original.RepaymentDate));
            Assert.That(deserialized.Status, Is.EqualTo(original.Status));
            Assert.That(deserialized.CurrentBtcPriceInLoanCurrency, Is.EqualTo(original.CurrentBtcPriceInLoanCurrency));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLoanDetails_WithNullRepaymentDate()
    {
        var original = new BtcLoanDetails(
            platformName: "Ledn",
            collateralSats: 50_000_000,
            loanAmount: 10_000m,
            currencyCode: "BRL",
            apr: 0.08m,
            initialLtv: 40m,
            liquidationLtv: 75m,
            marginCallLtv: 65m,
            fees: 0m,
            loanStartDate: new DateOnly(2025, 6, 1),
            repaymentDate: null,
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 300_000m);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.RepaymentDate, Is.Null);
            Assert.That(deserialized.PlatformName, Is.EqualTo("Ledn"));
            Assert.That(deserialized.CurrencyCode, Is.EqualTo("BRL"));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLoanDetails_WithRepaidStatus()
    {
        var original = new BtcLoanDetails(
            platformName: "HodlHodl",
            collateralSats: 100_000_000,
            loanAmount: 25_000m,
            currencyCode: "USD",
            apr: 0.12m,
            initialLtv: 50m,
            liquidationLtv: 80m,
            marginCallLtv: 70m,
            fees: 100m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2025, 12, 31),
            status: LoanStatus.Repaid,
            currentBtcPriceInLoanCurrency: 60_000m);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.That(deserialized.Status, Is.EqualTo(LoanStatus.Repaid));
    }

    #endregion

    #region BtcLending Round-Trip Tests

    [Test]
    public void Should_RoundTrip_BtcLendingDetails()
    {
        var original = new BtcLendingDetails(
            amountLent: 10_000m,
            currencyCode: "USD",
            apr: 0.05m,
            expectedRepaymentDate: new DateOnly(2026, 6, 15),
            borrowerOrPlatformName: "Ledn",
            lendingStartDate: new DateOnly(2025, 6, 15),
            status: LoanStatus.Active);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLendingDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLending, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.AmountLent, Is.EqualTo(original.AmountLent));
            Assert.That(deserialized.CurrencyCode, Is.EqualTo(original.CurrencyCode));
            Assert.That(deserialized.Apr, Is.EqualTo(original.Apr));
            Assert.That(deserialized.ExpectedRepaymentDate, Is.EqualTo(original.ExpectedRepaymentDate));
            Assert.That(deserialized.BorrowerOrPlatformName, Is.EqualTo(original.BorrowerOrPlatformName));
            Assert.That(deserialized.LendingStartDate, Is.EqualTo(original.LendingStartDate));
            Assert.That(deserialized.Status, Is.EqualTo(original.Status));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLendingDetails_WithNullRepaymentDate()
    {
        var original = new BtcLendingDetails(
            amountLent: 5_000m,
            currencyCode: "EUR",
            apr: 0.03m,
            expectedRepaymentDate: null,
            borrowerOrPlatformName: "Friend",
            lendingStartDate: new DateOnly(2025, 1, 1),
            status: LoanStatus.Active);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLendingDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLending, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.ExpectedRepaymentDate, Is.Null);
            Assert.That(deserialized.BorrowerOrPlatformName, Is.EqualTo("Friend"));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLendingDetails_WithRepaidStatus()
    {
        var original = new BtcLendingDetails(
            amountLent: 10_000m,
            currencyCode: "USD",
            apr: 0.05m,
            expectedRepaymentDate: new DateOnly(2025, 12, 31),
            borrowerOrPlatformName: "Ledn",
            lendingStartDate: new DateOnly(2025, 1, 1),
            status: LoanStatus.Repaid);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLendingDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLending, json);

        Assert.That(deserialized.Status, Is.EqualTo(LoanStatus.Repaid));
    }

    #endregion
}
