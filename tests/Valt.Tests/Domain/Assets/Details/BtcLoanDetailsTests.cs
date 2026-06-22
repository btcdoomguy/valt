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

    private static LoanStateSnapshot CreateSnapshot(
        BtcLoanDetails loan,
        DateOnly effectiveDate,
        decimal currentTotalDebt,
        long? collateralSats = null,
        decimal? loanAmount = null,
        decimal? apr = null,
        decimal? liquidationLtv = null,
        decimal? marginCallLtv = null,
        decimal? fees = null,
        DateOnly? repaymentDate = null,
        LoanStatus? status = null,
        decimal? currentBtcPrice = null,
        string? note = null)
    {
        var totalBorrowed = loanAmount ?? loan.LoanAmount;
        var actualFees = fees ?? loan.Fees;
        var interest = Math.Max(0m, currentTotalDebt - totalBorrowed - actualFees);

        return new LoanStateSnapshot(
            platformName: loan.PlatformName,
            collateralSats: collateralSats ?? loan.CollateralSats,
            loanAmount: totalBorrowed,
            currencyCode: loan.CurrencyCode,
            apr: apr ?? loan.Apr,
            initialLtv: loan.InitialLtv,
            liquidationLtv: liquidationLtv ?? loan.LiquidationLtv,
            marginCallLtv: marginCallLtv ?? loan.MarginCallLtv,
            fees: actualFees,
            loanStartDate: loan.LoanStartDate,
            repaymentDate: repaymentDate ?? loan.RepaymentDate,
            status: status ?? loan.Status,
            currentBtcPriceInLoanCurrency: currentBtcPrice ?? loan.CurrentBtcPriceInLoanCurrency,
            fixedTotalDebt: loan.FixedTotalDebt,
            totalBorrowed: totalBorrowed,
            interestAccruedUntilDate: interest,
            effectiveDate: effectiveDate,
            note: note);
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

    #region Fixed Total Debt Tests

    [Test]
    public void Should_Expose_HasFixedTotalDebt_When_Set()
    {
        var details = CreateDefaultDetails();
        var fixedDetails = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0.12m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        Assert.Multiple(() =>
        {
            Assert.That(details.HasFixedTotalDebt, Is.False);
            Assert.That(details.FixedTotalDebt, Is.Null);
            Assert.That(fixedDetails.HasFixedTotalDebt, Is.True);
            Assert.That(fixedDetails.FixedTotalDebt, Is.EqualTo(27_500m));
        });
    }

    [Test]
    public void Should_Validate_FixedTotalDebt_Not_Below_Principal_Plus_Fees()
    {
        // principal = 25000, fees = 100 => minimum allowed = 25100
        Assert.Throws<ArgumentException>(() => new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 25_099m));
    }

    [Test]
    public void Should_Allow_FixedTotalDebt_Equal_To_Principal_Plus_Fees()
    {
        // Zero-interest fixed loan edge case
        var details = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 25_100m);

        Assert.Multiple(() =>
        {
            Assert.That(details.CalculateTotalDebt(), Is.EqualTo(25_100m));
            Assert.That(details.CalculateAccruedInterest(), Is.EqualTo(0m));
        });
    }

    [Test]
    public void Should_Return_FixedTotalDebt_As_TotalDebt_Regardless_Of_Elapsed_Time()
    {
        // Fixed debt = 27500. Today is way before repayment. Total debt must still be 27500.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var repaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(364);
        var details = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 0m,
            startDate, repaymentDate, LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        Assert.That(details.CalculateTotalDebt(), Is.EqualTo(27_500m));
    }

    [Test]
    public void Should_Return_Full_Premium_As_AccruedInterest_For_Fixed_Debt()
    {
        // Fixed debt = 27500, principal = 25000, fees = 100 => premium = 2400
        // Accrued interest for fixed loans is the full premium, not time-prorated.
        var details = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        Assert.That(details.CalculateAccruedInterest(), Is.EqualTo(2_400m));
    }

    [Test]
    public void Should_Calculate_CurrentValue_As_Negative_FixedTotalDebt()
    {
        var details = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 0m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        Assert.That(details.CalculateCurrentValue(50_000m), Is.EqualTo(-27_500m));
    }

    [Test]
    public void Should_Preserve_FixedTotalDebt_When_Updating_Price()
    {
        var original = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        var updated = (BtcLoanDetails)original.WithUpdatedPrice(60_000m);

        Assert.Multiple(() =>
        {
            Assert.That(updated.CurrentBtcPriceInLoanCurrency, Is.EqualTo(60_000m));
            Assert.That(updated.FixedTotalDebt, Is.EqualTo(27_500m));
            Assert.That(updated.HasFixedTotalDebt, Is.True);
        });
    }

    [Test]
    public void Should_Preserve_FixedTotalDebt_When_Changing_Status()
    {
        var original = new BtcLoanDetails(
            "HodlHodl", 100_000_000, 25_000m, "USD", 0m, 50m, 80m, 70m, 100m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), LoanStatus.Active, 50_000m,
            fixedTotalDebt: 27_500m);

        var updated = original.WithStatus(LoanStatus.Repaid);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(LoanStatus.Repaid));
            Assert.That(updated.FixedTotalDebt, Is.EqualTo(27_500m));
        });
    }

    [Test]
    public void DeriveAprFromFixedDebt_Should_Compute_Annualized_Rate_For_One_Year_Loan()
    {
        // 25000 principal, 27500 total over 365 days => 10% APR
        var apr = BtcLoanDetails.DeriveAprFromFixedDebt(
            loanAmount: 25_000m,
            fixedTotalDebt: 27_500m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2026, 1, 1));

        Assert.That(apr, Is.EqualTo(0.1m));
    }

    [Test]
    public void DeriveAprFromFixedDebt_Should_Annualize_Sub_Year_Loans()
    {
        // 10000 principal, 10500 total over ~73 days => interest rate over period = 5%
        // Annualized = 0.05 * 365 / 73 = 0.25 => 25% APR
        var apr = BtcLoanDetails.DeriveAprFromFixedDebt(
            loanAmount: 10_000m,
            fixedTotalDebt: 10_500m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2025, 3, 15));

        Assert.That(apr, Is.EqualTo(0.25m));
    }

    [Test]
    public void DeriveAprFromFixedDebt_Should_Return_Zero_When_RepaymentDate_Missing()
    {
        var apr = BtcLoanDetails.DeriveAprFromFixedDebt(
            loanAmount: 25_000m,
            fixedTotalDebt: 27_500m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: null);

        Assert.That(apr, Is.EqualTo(0m));
    }

    [Test]
    public void DeriveAprFromFixedDebt_Should_Return_Zero_When_RepaymentDate_Not_After_Start()
    {
        var apr = BtcLoanDetails.DeriveAprFromFixedDebt(
            loanAmount: 25_000m,
            fixedTotalDebt: 27_500m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2025, 1, 1));

        Assert.That(apr, Is.EqualTo(0m));
    }

    [Test]
    public void DeriveAprFromFixedDebt_Should_Return_Zero_When_No_Premium()
    {
        var apr = BtcLoanDetails.DeriveAprFromFixedDebt(
            loanAmount: 25_000m,
            fixedTotalDebt: 25_000m,
            loanStartDate: new DateOnly(2025, 1, 1),
            repaymentDate: new DateOnly(2026, 1, 1));

        Assert.That(apr, Is.EqualTo(0m));
    }

    #endregion

    #region Snapshot Tests

    [Test]
    public void Should_Throw_When_Snapshot_Fees_Is_Negative()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        var ex = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            26_000m,
            fees: -1m));

        Assert.That(ex!.ParamName, Is.EqualTo("fees"));
    }

    [Test]
    public void Should_Throw_When_Snapshot_TotalBorrowed_Is_Negative()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        Assert.Throws<ArgumentException>(() => new LoanStateSnapshot(
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
            totalBorrowed: -1m,
            interestAccruedUntilDate: 0m,
            effectiveDate: new DateOnly(2025, 6, 1)));
    }

    [Test]
    public void Should_Throw_When_Snapshot_Interest_Is_Negative()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        Assert.Throws<ArgumentException>(() => new LoanStateSnapshot(
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
            totalBorrowed: loan.LoanAmount,
            interestAccruedUntilDate: -1m,
            effectiveDate: new DateOnly(2025, 6, 1)));
    }

    [Test]
    public void Should_Order_Snapshots_By_EffectiveDate_Ascending()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        var details = loan
            .WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 6, 1), 26_000m))
            .WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 1, 1), 25_100m))
            .WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 12, 1), 27_000m));

        Assert.That(details.Snapshots, Has.Count.EqualTo(3));
        Assert.That(details.Snapshots[0].EffectiveDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
        Assert.That(details.Snapshots[1].EffectiveDate, Is.EqualTo(new DateOnly(2025, 6, 1)));
        Assert.That(details.Snapshots[2].EffectiveDate, Is.EqualTo(new DateOnly(2025, 12, 1)));
    }

    [Test]
    public void Should_Use_Latest_Snapshot_For_Total_Debt()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var details = loan
            .WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 6, 1), 26_000m))
            .WithAddedSnapshot(CreateSnapshot(loan, today, 26_500m));

        Assert.That(details.CalculateTotalDebt(), Is.EqualTo(26_500m));
    }

    [Test]
    public void Should_Recalculate_Total_Debt_From_Past_Snapshot()
    {
        // Snapshot currentTotalDebt = 26_000, fees = 100, apr = 0.12
        // Borrowed = 25_000, interest until date = 900. Over 365 days interest = 25_000 * 0.12 = 3_000.
        // Total = 26_000 + 3_000 = 29_000.
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1), fees: 100m);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);

        var details = loan.WithAddedSnapshot(CreateSnapshot(loan, pastDate, 26_000m));

        Assert.That(details.CalculateTotalDebt(), Is.EqualTo(29_000m));
    }

    [Test]
    public void Should_Calculate_Accrued_Interest_From_Snapshot_Effective_Date()
    {
        // Snapshot currentTotalDebt = 26_000, fees = 100, apr = 0.12
        // Borrowed = 25_000, interest until date = 900. Over 365 days interest = 25_000 * 0.12 = 3_000.
        // Total accrued interest = 900 + 3_000 = 3_900.
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1), fees: 100m);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);

        var details = loan.WithAddedSnapshot(CreateSnapshot(loan, pastDate, 26_000m));

        Assert.That(details.CalculateAccruedInterest(), Is.EqualTo(3_900m));
    }

    [Test]
    public void Should_Use_Latest_Snapshot_For_Current_Ltv()
    {
        var loan = CreateDefaultDetails(currentBtcPrice: 50_000m);

        var details = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            15_000m,
            collateralSats: 50_000_000,
            loanAmount: 15_000m));

        // Collateral value = 0.5 BTC * $50,000 = $25,000
        // LTV = 15,000 / 25,000 * 100 = 60%
        Assert.That(details.CalculateCurrentLtv(50_000m), Is.EqualTo(60m));
    }

    [Test]
    public void Should_Use_Latest_Snapshot_Ltv_Thresholds_For_HealthStatus()
    {
        var loan = CreateDefaultDetails(currentBtcPrice: 50_000m);

        var details = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            25_000m,
            liquidationLtv: 50m,
            marginCallLtv: 45m));

        // Setup LTV = 50%, latest snapshot liquidation = 50% => Danger
        Assert.That(details.CalculateHealthStatus(50_000m), Is.EqualTo(LoanHealthStatus.Danger));
    }

    [Test]
    public void Should_Use_Latest_Snapshot_Ltv_Thresholds_For_DistanceToLiquidation()
    {
        var loan = CreateDefaultDetails(currentBtcPrice: 50_000m);

        var details = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            25_000m,
            liquidationLtv: 60m,
            marginCallLtv: 55m));

        // Setup LTV = 50%, latest snapshot liquidation = 60% => distance = 10
        Assert.That(details.CalculateDistanceToLiquidation(50_000m), Is.EqualTo(10m));
    }

    [Test]
    public void Should_Return_Zero_Accrued_Interest_When_Snapshot_Is_Effective_Today()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var loan = CreateDefaultDetails(loanStartDate: startDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var details = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            today,
            25_000m));

        Assert.That(details.CalculateAccruedInterest(), Is.EqualTo(0m));
    }

    [Test]
    public void Should_Fall_Back_To_Previous_Snapshot_After_Removing_Latest()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var details = loan
            .WithAddedSnapshot(CreateSnapshot(loan, yesterday, 26_000m))
            .WithAddedSnapshot(CreateSnapshot(loan, today, 27_000m))
            .WithoutSnapshot(today);

        // Previous snapshot recalculates one day of interest:
        // borrowed = 25_000; interest until date = 900; additional interest = 25_000 * 0.12 / 365 = 8.22
        // Total = 25_000 + 900 + 100 + 8.22 = 26_008.22
        Assert.That(details.CalculateTotalDebt(), Is.EqualTo(26_008.22m));
    }

    [Test]
    public void Should_Fall_Back_To_Setup_Values_After_Removing_All_Snapshots()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));
        var setupTotalDebt = loan.CalculateTotalDebt();

        var details = loan
            .WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 6, 1), 26_000m))
            .WithoutSnapshot(new DateOnly(2025, 6, 1));

        Assert.That(details.CalculateTotalDebt(), Is.EqualTo(setupTotalDebt));
    }

    [Test]
    public void Should_Keep_Setup_Properties_Unchanged_After_Adding_Snapshot()
    {
        var original = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        var updated = original.WithAddedSnapshot(CreateSnapshot(
            original,
            new DateOnly(2025, 6, 1),
            26_000m,
            loanAmount: 30_000m,
            collateralSats: 50_000_000));

        Assert.Multiple(() =>
        {
            Assert.That(original.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(original.CollateralSats, Is.EqualTo(100_000_000));
            Assert.That(original.Snapshots, Has.Count.EqualTo(0));
            Assert.That(updated.LoanAmount, Is.EqualTo(25_000m));
            Assert.That(updated.CollateralSats, Is.EqualTo(100_000_000));
            Assert.That(updated.Snapshots, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Should_Throw_When_Adding_Snapshot_With_Duplicate_EffectiveDate()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));
        var snapshot = CreateSnapshot(loan, new DateOnly(2025, 6, 1), 26_000m);

        var withSnapshot = loan.WithAddedSnapshot(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(withSnapshot.Snapshots, Has.Count.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                withSnapshot.WithAddedSnapshot(CreateSnapshot(loan, new DateOnly(2025, 6, 1), 27_000m)));
        });
    }

    [Test]
    public void Should_Preserve_Snapshot_Note_And_Allow_Null_Or_Empty()
    {
        var loan = CreateDefaultDetails(loanStartDate: new DateOnly(2025, 1, 1));

        var withNote = loan.WithAddedSnapshot(CreateSnapshot(
            loan,
            new DateOnly(2025, 6, 1),
            26_000m,
            note: "Updated after draw"));

        Assert.Multiple(() =>
        {
            Assert.That(withNote.Snapshots[0].Note, Is.EqualTo("Updated after draw"));
            Assert.DoesNotThrow(() =>
                CreateSnapshot(loan, new DateOnly(2025, 7, 1), 26_500m, note: null));
            Assert.DoesNotThrow(() =>
                CreateSnapshot(loan, new DateOnly(2025, 8, 1), 26_500m, note: string.Empty));
        });
    }

    #endregion
}
