using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Domain.Assets.Details;

[TestFixture]
public class BtcLoanDetailsTests
{
    private static BtcLoanDetails CreateDefaultDetails(
        string platformName = "HodlHodl",
        long collateralSats = 100_000_000, // 1 BTC
        decimal loanAmount = 25_000m,
        string currencyCode = "USD",
        decimal apr = 0.12m,
        decimal initialLtv = 50m,
        decimal liquidationLtv = 80m,
        decimal marginCallLtv = 70m,
        decimal fees = 100m,
        DateOnly? loanStartDate = null,
        DateOnly? repaymentDate = null,
        LoanStatus status = LoanStatus.Active,
        decimal currentBtcPrice = 50_000m)
    {
        return new BtcLoanDetails(
            platformName,
            collateralSats,
            loanAmount,
            currencyCode,
            apr,
            initialLtv,
            liquidationLtv,
            marginCallLtv,
            fees,
            loanStartDate ?? new DateOnly(2025, 1, 1),
            repaymentDate,
            status,
            currentBtcPrice);
    }

    #region Construction Tests

    [Test]
    public void Should_Create_With_Valid_Parameters()
    {
        var details = CreateDefaultDetails();

        Assert.Multiple(() =>
        {
            Assert.That(details.AssetType, Is.EqualTo(AssetTypes.BtcLoan));
            Assert.That(details.PlatformName, Is.EqualTo("HodlHodl"));
            Assert.That(details.CollateralSats, Is.EqualTo(100_000_000));
            Assert.That(details.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(details.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(details.Apr, Is.EqualTo(0.12m));
            Assert.That(details.InitialLtv, Is.EqualTo(50m));
            Assert.That(details.LiquidationLtv, Is.EqualTo(80m));
            Assert.That(details.MarginCallLtv, Is.EqualTo(70m));
            Assert.That(details.Fees, Is.EqualTo(100m));
            Assert.That(details.Status, Is.EqualTo(LoanStatus.Active));
            Assert.That(details.CurrentBtcPriceInLoanCurrency, Is.EqualTo(50_000m));
        });
    }

    [Test]
    public void Should_Validate_CollateralSats_Is_Positive()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(collateralSats: 0));

        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(collateralSats: -1));
    }

    [Test]
    public void Should_Validate_LoanAmount_Is_Positive()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(loanAmount: 0));

        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(loanAmount: -1000m));
    }

    [Test]
    public void Should_Validate_Apr_Not_Negative()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(apr: -0.01m));
    }

    [Test]
    public void Should_Allow_Zero_Apr()
    {
        var details = CreateDefaultDetails(apr: 0m);
        Assert.That(details.Apr, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Validate_MarginCallLtv_Is_Positive()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(marginCallLtv: 0));

        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(marginCallLtv: -10m));
    }

    [Test]
    public void Should_Validate_LiquidationLtv_Greater_Than_MarginCallLtv()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(liquidationLtv: 70m, marginCallLtv: 70m));

        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(liquidationLtv: 60m, marginCallLtv: 70m));
    }

    #endregion

    #region LTV Calculation Tests

    [Test]
    public void Should_Calculate_Current_Ltv()
    {
        // 1 BTC collateral, $25,000 loan, BTC price = $50,000
        // CollateralValue = 1 * 50000 = 50000
        // LTV = 25000 / 50000 * 100 = 50%
        var details = CreateDefaultDetails();

        var ltv = details.CalculateCurrentLtv(50_000m);

        Assert.That(ltv, Is.EqualTo(50m));
    }

    [Test]
    public void Should_Calculate_Higher_Ltv_When_Btc_Price_Drops()
    {
        // 1 BTC collateral, $25,000 loan, BTC price drops to $31,250
        // CollateralValue = 1 * 31250 = 31250
        // LTV = 25000 / 31250 * 100 = 80%
        var details = CreateDefaultDetails();

        var ltv = details.CalculateCurrentLtv(31_250m);

        Assert.That(ltv, Is.EqualTo(80m));
    }

    [Test]
    public void Should_Calculate_Lower_Ltv_When_Btc_Price_Rises()
    {
        // 1 BTC collateral, $25,000 loan, BTC price rises to $100,000
        // CollateralValue = 1 * 100000 = 100000
        // LTV = 25000 / 100000 * 100 = 25%
        var details = CreateDefaultDetails();

        var ltv = details.CalculateCurrentLtv(100_000m);

        Assert.That(ltv, Is.EqualTo(25m));
    }

    [Test]
    public void Should_Return_Zero_Ltv_When_Price_Is_Zero()
    {
        var details = CreateDefaultDetails();

        var ltv = details.CalculateCurrentLtv(0m);

        Assert.That(ltv, Is.EqualTo(0m));
    }

    #endregion

    #region Health Status Tests

    [Test]
    public void Should_Return_Healthy_When_Ltv_Below_MarginCall()
    {
        // LTV at 50% (< 70% margin call)
        var details = CreateDefaultDetails();

        var status = details.CalculateHealthStatus(50_000m);

        Assert.That(status, Is.EqualTo(LoanHealthStatus.Healthy));
    }

    [Test]
    public void Should_Return_Warning_When_Ltv_At_MarginCall()
    {
        // BTC price = ~35714 => LTV = 25000/35714*100 ≈ 70%
        var details = CreateDefaultDetails();

        var status = details.CalculateHealthStatus(35_714.29m);

        Assert.That(status, Is.EqualTo(LoanHealthStatus.Warning));
    }

    [Test]
    public void Should_Return_Warning_When_Ltv_Between_MarginCall_And_Liquidation()
    {
        // BTC price = 33333 => LTV = 25000/33333*100 ≈ 75%
        var details = CreateDefaultDetails();

        var status = details.CalculateHealthStatus(33_333m);

        Assert.That(status, Is.EqualTo(LoanHealthStatus.Warning));
    }

    [Test]
    public void Should_Return_Danger_When_Ltv_At_Liquidation()
    {
        // BTC price = 31250 => LTV = 25000/31250*100 = 80%
        var details = CreateDefaultDetails();

        var status = details.CalculateHealthStatus(31_250m);

        Assert.That(status, Is.EqualTo(LoanHealthStatus.Danger));
    }

    [Test]
    public void Should_Return_Danger_When_Ltv_Above_Liquidation()
    {
        // BTC price = 25000 => LTV = 25000/25000*100 = 100%
        var details = CreateDefaultDetails();

        var status = details.CalculateHealthStatus(25_000m);

        Assert.That(status, Is.EqualTo(LoanHealthStatus.Danger));
    }

    #endregion

    #region Distance To Liquidation Tests

    [Test]
    public void Should_Calculate_Distance_To_Liquidation()
    {
        // LTV = 50%, Liquidation = 80% => distance = 30 percentage points
        var details = CreateDefaultDetails();

        var distance = details.CalculateDistanceToLiquidation(50_000m);

        Assert.That(distance, Is.EqualTo(30m));
    }

    [Test]
    public void Should_Return_Zero_Distance_When_At_Liquidation()
    {
        // LTV = 80%, Liquidation = 80% => distance = 0
        var details = CreateDefaultDetails();

        var distance = details.CalculateDistanceToLiquidation(31_250m);

        Assert.That(distance, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_Zero_Distance_When_Past_Liquidation()
    {
        // LTV > 80% => distance = 0 (not negative)
        var details = CreateDefaultDetails();

        var distance = details.CalculateDistanceToLiquidation(25_000m);

        Assert.That(distance, Is.EqualTo(0m));
    }

    #endregion

    #region Accrued Interest Tests

    [Test]
    public void Should_Calculate_Accrued_Interest()
    {
        // Loan of $25,000 at 12% APR started 365 days ago
        // Interest = 25000 * 0.12 / 365 * 365 = 3000
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(loanStartDate: startDate);

        var interest = details.CalculateAccruedInterest();

        Assert.That(interest, Is.EqualTo(3000m));
    }

    [Test]
    public void Should_Return_Zero_Interest_When_Loan_Just_Started()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(loanStartDate: today);

        var interest = details.CalculateAccruedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_Zero_Interest_For_Future_Start_Date()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var details = CreateDefaultDetails(loanStartDate: futureDate);

        var interest = details.CalculateAccruedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_Zero_Interest_When_Apr_Is_Zero()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(apr: 0m, loanStartDate: startDate);

        var interest = details.CalculateAccruedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    #endregion

    #region Days Until Repayment Tests

    [Test]
    public void Should_Calculate_Days_Until_Repayment()
    {
        var repaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var details = CreateDefaultDetails(repaymentDate: repaymentDate);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.EqualTo(30));
    }

    [Test]
    public void Should_Return_Null_When_No_Repayment_Date()
    {
        var details = CreateDefaultDetails(repaymentDate: null);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.Null);
    }

    [Test]
    public void Should_Return_Zero_When_Repayment_Date_Passed()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var details = CreateDefaultDetails(repaymentDate: pastDate);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.EqualTo(0));
    }

    #endregion

    #region Total Debt Tests

    [Test]
    public void Should_Calculate_Total_Debt_As_Loan_Plus_Interest_Plus_Fees()
    {
        // Loan of $25,000 at 12% APR started 365 days ago, fees = 100
        // Interest = 25000 * 0.12 / 365 * 365 = 3000
        // TotalDebt = 25000 + 3000 + 100 = 28100
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(loanStartDate: startDate, fees: 100m);

        var totalDebt = details.CalculateTotalDebt();

        Assert.That(totalDebt, Is.EqualTo(28_100m));
    }

    [Test]
    public void Should_Calculate_Total_Debt_With_Zero_Fees()
    {
        // Loan of $25,000 at 12% APR started 365 days ago, no fees
        // TotalDebt = 25000 + 3000 + 0 = 28000
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(loanStartDate: startDate, fees: 0m);

        var totalDebt = details.CalculateTotalDebt();

        Assert.That(totalDebt, Is.EqualTo(28_000m));
    }

    [Test]
    public void Should_Calculate_Total_Debt_With_No_Accrued_Interest_When_Just_Started()
    {
        // Loan just started today, no interest yet
        // TotalDebt = 25000 + 0 + 100 = 25100
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(loanStartDate: today, fees: 100m);

        var totalDebt = details.CalculateTotalDebt();

        Assert.That(totalDebt, Is.EqualTo(25_100m));
    }

    [Test]
    public void Should_Calculate_Total_Debt_With_Future_Start_Date()
    {
        // Loan hasn't started yet, no interest accrued
        // TotalDebt = 25000 + 0 + 100 = 25100
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var details = CreateDefaultDetails(loanStartDate: futureDate, fees: 100m);

        var totalDebt = details.CalculateTotalDebt();

        Assert.That(totalDebt, Is.EqualTo(25_100m));
    }

    #endregion

    #region Current Value Tests

    [Test]
    public void Should_Calculate_Current_Value_As_Negative_Total_Debt()
    {
        // Start date = today, so no accrued interest
        // TotalDebt = 25000 + 0 + 100 = 25100
        // CurrentValue = -25100 (pure liability)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(loanStartDate: today, fees: 100m);

        var value = details.CalculateCurrentValue(50_000m);

        Assert.That(value, Is.EqualTo(-25_100m));
    }

    [Test]
    public void Should_Calculate_Current_Value_With_Accrued_Interest()
    {
        // Loan of $25,000 at 12% APR started 365 days ago, fees = 0
        // TotalDebt = 25000 + 3000 + 0 = 28000
        // CurrentValue = -28000
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(loanStartDate: startDate, fees: 0m);

        var value = details.CalculateCurrentValue(50_000m);

        Assert.That(value, Is.EqualTo(-28_000m));
    }

    [Test]
    public void Should_Calculate_Current_Value_Regardless_Of_Btc_Price()
    {
        // BTC price doesn't affect the debt calculation anymore
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(loanStartDate: today, fees: 0m);

        var valueAtHighPrice = details.CalculateCurrentValue(100_000m);
        var valueAtLowPrice = details.CalculateCurrentValue(20_000m);
        var valueAtZeroPrice = details.CalculateCurrentValue(0m);

        Assert.Multiple(() =>
        {
            Assert.That(valueAtHighPrice, Is.EqualTo(-25_000m));
            Assert.That(valueAtLowPrice, Is.EqualTo(-25_000m));
            Assert.That(valueAtZeroPrice, Is.EqualTo(-25_000m));
        });
    }

    #endregion

    #region WithUpdatedPrice Tests

    [Test]
    public void Should_Create_New_Details_With_Updated_Price()
    {
        var original = CreateDefaultDetails();

        var updated = (BtcLoanDetails)original.WithUpdatedPrice(75_000m);

        Assert.Multiple(() =>
        {
            Assert.That(updated.CurrentBtcPriceInLoanCurrency, Is.EqualTo(75_000m));
            Assert.That(updated.PlatformName, Is.EqualTo(original.PlatformName));
            Assert.That(updated.CollateralSats, Is.EqualTo(original.CollateralSats));
            Assert.That(updated.LoanAmount, Is.EqualTo(original.LoanAmount));
            Assert.That(updated.Apr, Is.EqualTo(original.Apr));
            Assert.That(updated.LiquidationLtv, Is.EqualTo(original.LiquidationLtv));
            Assert.That(updated.MarginCallLtv, Is.EqualTo(original.MarginCallLtv));
            Assert.That(updated.Status, Is.EqualTo(original.Status));
        });
    }

    #endregion

    #region WithStatus Tests

    [Test]
    public void Should_Create_New_Details_With_Repaid_Status()
    {
        var original = CreateDefaultDetails();

        var updated = original.WithStatus(LoanStatus.Repaid);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(LoanStatus.Repaid));
            Assert.That(updated.CollateralSats, Is.EqualTo(original.CollateralSats));
            Assert.That(updated.LoanAmount, Is.EqualTo(original.LoanAmount));
            Assert.That(updated.PlatformName, Is.EqualTo(original.PlatformName));
        });
    }

    #endregion
}
