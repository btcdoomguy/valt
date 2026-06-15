using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.Modules.Assets;

namespace Valt.Tests.Infrastructure.Assets;

[TestFixture]
public class AssetDetailsSerializerTests
{
    private static LoanStateSnapshot CreateSnapshot(
        BtcLoanDetails loan,
        DateOnly effectiveDate,
        decimal currentTotalDebt,
        string? note = null)
    {
        return new LoanStateSnapshot(
            platformName: loan.PlatformName,
            collateralSats: loan.CollateralSats,
            loanAmount: loan.LoanAmount,
            currencyCode: loan.CurrencyCode,
            apr: loan.Apr,
            initialLtv: loan.InitialLtv,
            liquidationLtv: loan.LiquidationLtv,
            marginCallLtv: loan.MarginCallLtv,
            fees: loan.Fees,
            loanStartDate: loan.LoanStartDate,
            repaymentDate: loan.RepaymentDate,
            status: loan.Status,
            currentBtcPriceInLoanCurrency: loan.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: loan.FixedTotalDebt,
            currentTotalDebt: currentTotalDebt,
            effectiveDate: effectiveDate,
            note: note);
    }

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
    public void Should_RoundTrip_BtcLoanDetails_WithFixedTotalDebt()
    {
        var original = new BtcLoanDetails(
            platformName: "HodlHodl",
            collateralSats: 100_000_000,
            loanAmount: 25_000m,
            currencyCode: "USD",
            apr: 0.10m,
            initialLtv: 50m,
            liquidationLtv: 80m,
            marginCallLtv: 70m,
            fees: 100m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m,
            fixedTotalDebt: 27_500m);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.FixedTotalDebt, Is.EqualTo(27_500m));
            Assert.That(deserialized.HasFixedTotalDebt, Is.True);
            Assert.That(deserialized.CalculateTotalDebt(), Is.EqualTo(27_500m));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLoanDetails_WithoutFixedTotalDebt()
    {
        var original = new BtcLoanDetails(
            platformName: "Ledn",
            collateralSats: 100_000_000,
            loanAmount: 25_000m,
            currencyCode: "USD",
            apr: 0.10m,
            initialLtv: 50m,
            liquidationLtv: 80m,
            marginCallLtv: 70m,
            fees: 100m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.FixedTotalDebt, Is.Null);
            Assert.That(deserialized.HasFixedTotalDebt, Is.False);
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

    #region BtcLoan Snapshot Round-Trip Tests

    [Test]
    public void Should_RoundTrip_BtcLoanDetails_WithSingleSnapshot()
    {
        var loan = new BtcLoanDetails(
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
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var original = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            26_000m,
            note: "Half-year update"));

        var json = AssetDetailsSerializer.Serialize(original);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
            Assert.That(deserialized.Snapshots[0].CurrentTotalDebt, Is.EqualTo(26_000m));
            Assert.That(deserialized.Snapshots[0].Note, Is.EqualTo("Half-year update"));
        });
    }

    [Test]
    public void Should_RoundTrip_BtcLoanDetails_WithMultipleSnapshots()
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
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var juneSnapshot = CreateSnapshot(original, new DateOnly(2025, 6, 1), 26_000m, note: "June update");
        var septemberSnapshot = CreateSnapshot(original, new DateOnly(2025, 9, 1), 27_000m, note: null);

        var withSnapshots = original
            .WithAddedSnapshot(septemberSnapshot)
            .WithAddedSnapshot(juneSnapshot);

        var json = AssetDetailsSerializer.Serialize(withSnapshots);
        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(2));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
            Assert.That(deserialized.Snapshots[0].Note, Is.EqualTo("June update"));
            Assert.That(deserialized.Snapshots[1].EffectiveDate, Is.EqualTo(new DateOnly(2025, 9, 1)));
            Assert.That(deserialized.Snapshots[1].Note, Is.Null);
        });
    }

    [Test]
    public void Should_AutoSeed_Snapshot_When_Legacy_BtcLoan_Has_None()
    {
        const decimal loanAmount = 25_000m;
        const decimal fees = 100m;
        var legacyJson = "{\"PlatformName\":\"HodlHodl\",\"CollateralSats\":100000000,\"LoanAmount\":25000,\"CurrencyCode\":\"USD\",\"Apr\":0.12,\"InitialLtv\":50,\"LiquidationLtv\":80,\"MarginCallLtv\":70,\"Fees\":100,\"LoanStartDate\":\"2025-01-01T00:00:00\",\"RepaymentDate\":\"2026-01-01T00:00:00\",\"StatusId\":0,\"CurrentBtcPrice\":50000,\"FixedTotalDebt\":null}";

        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, legacyJson);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(deserialized.Snapshots[0].CurrentTotalDebt, Is.EqualTo(loanAmount + fees));
        });
    }

    [Test]
    public void Should_AutoSeed_FixedDebt_Legacy_Loan()
    {
        const decimal fixedTotalDebt = 27_500m;
        var legacyJson = "{\"PlatformName\":\"HodlHodl\",\"CollateralSats\":100000000,\"LoanAmount\":25000,\"CurrencyCode\":\"USD\",\"Apr\":0.10,\"InitialLtv\":50,\"LiquidationLtv\":80,\"MarginCallLtv\":70,\"Fees\":100,\"LoanStartDate\":\"2025-01-01T00:00:00\",\"RepaymentDate\":\"2026-01-01T00:00:00\",\"StatusId\":0,\"CurrentBtcPrice\":50000,\"FixedTotalDebt\":27500}";

        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, legacyJson);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(deserialized.Snapshots[0].CurrentTotalDebt, Is.EqualTo(fixedTotalDebt));
        });
    }

    [Test]
    public void Should_AutoSeed_When_Snapshots_Array_Is_Explicitly_Empty()
    {
        var legacyJson = "{\"PlatformName\":\"HodlHodl\",\"CollateralSats\":100000000,\"LoanAmount\":25000,\"CurrencyCode\":\"USD\",\"Apr\":0.12,\"InitialLtv\":50,\"LiquidationLtv\":80,\"MarginCallLtv\":70,\"Fees\":100,\"LoanStartDate\":\"2025-01-01T00:00:00\",\"RepaymentDate\":\"2026-01-01T00:00:00\",\"StatusId\":0,\"CurrentBtcPrice\":50000,\"FixedTotalDebt\":null,\"Snapshots\":[]}";

        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, legacyJson);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(deserialized.Snapshots[0].CurrentTotalDebt, Is.EqualTo(25_100m));
        });
    }

    [Test]
    public void Should_Deserialize_Legacy_Loan_Missing_LoanStartDate()
    {
        const decimal loanAmount = 25_000m;
        const decimal fees = 100m;
        var legacyJson = "{\"PlatformName\":\"HodlHodl\",\"CollateralSats\":100000000,\"LoanAmount\":25000,\"CurrencyCode\":\"USD\",\"Apr\":0.12,\"InitialLtv\":50,\"LiquidationLtv\":80,\"MarginCallLtv\":70,\"Fees\":100,\"RepaymentDate\":\"2026-01-01T00:00:00\",\"StatusId\":0,\"CurrentBtcPrice\":50000,\"FixedTotalDebt\":null}";

        var deserialized = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, legacyJson);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized.Snapshots[0].EffectiveDate, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow)));
            Assert.That(deserialized.Snapshots[0].CurrentTotalDebt, Is.EqualTo(loanAmount + fees));
            Assert.That(deserialized.LoanStartDate, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow)));
        });
    }

    [Test]
    public void Should_Preserve_Snapshots_After_Second_RoundTrip()
    {
        var loan = new BtcLoanDetails(
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
            repaymentDate: new DateOnly(2026, 1, 1),
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m);

        var original = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            26_000m,
            note: "Idempotency check"));

        var json1 = AssetDetailsSerializer.Serialize(original);
        var deserialized1 = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json1);
        var json2 = AssetDetailsSerializer.Serialize(deserialized1);
        var deserialized2 = (BtcLoanDetails)AssetDetailsSerializer.DeserializeDetails(AssetTypes.BtcLoan, json2);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized2.Snapshots, Has.Count.EqualTo(1));
            Assert.That(deserialized2.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
            Assert.That(deserialized2.Snapshots[0].CurrentTotalDebt, Is.EqualTo(26_000m));
            Assert.That(deserialized2.Snapshots[0].Note, Is.EqualTo("Idempotency check"));
        });
    }

    #endregion
}
