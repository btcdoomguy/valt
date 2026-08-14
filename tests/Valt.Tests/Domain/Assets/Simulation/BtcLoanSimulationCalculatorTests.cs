using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Core.Modules.Assets.Simulation;

namespace Valt.Tests.Domain.Assets.Simulation;

[TestFixture]
public class BtcLoanSimulationCalculatorTests
{
    [Test]
    public void SimpleInterest_Should_Parity_Match_BtcLoanDetails_CalculateAccruedInterest()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-100);
        var collateralSats = 100_000_000L;
        var principal = 25_000m;
        var apr = 0.12m;
        var fees = 100m;
        var liquidationLtv = 80m;

        var details = new BtcLoanDetails(
            platformName: "HodlHodl",
            collateralSats: collateralSats,
            loanAmount: principal,
            currencyCode: "USD",
            apr: apr,
            initialLtv: 50m,
            liquidationLtv: liquidationLtv,
            marginCallLtv: 70m,
            fees: fees,
            loanStartDate: startDate,
            repaymentDate: today,
            status: LoanStatus.Active,
            currentBtcPriceInLoanCurrency: 50_000m,
            fixedTotalDebt: null,
            snapshots: new[]
            {
                new LoanStateSnapshot(
                    platformName: "HodlHodl",
                    collateralSats: collateralSats,
                    loanAmount: principal,
                    currencyCode: "USD",
                    apr: apr,
                    initialLtv: 50m,
                    liquidationLtv: liquidationLtv,
                    marginCallLtv: 70m,
                    fees: fees,
                    loanStartDate: startDate,
                    repaymentDate: today,
                    status: LoanStatus.Active,
                    currentBtcPriceInLoanCurrency: 50_000m,
                    fixedTotalDebt: null,
                    totalBorrowed: principal,
                    interestAccruedUntilDate: 0m,
                    effectiveDate: startDate)
            });

        var expectedInterest = details.CalculateAccruedInterest();

        var result = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = collateralSats,
            PrincipalAmount = principal,
            CurrencyCode = "USD",
            Apr = apr,
            LiquidationLtv = liquidationLtv,
            Fees = fees,
            StartDate = startDate,
            EndDate = today,
            InterestMode = BtcLoanInterestMode.Simple
        });

        Assert.That(result.Interest, Is.EqualTo(expectedInterest));
    }

    [Test]
    public void SimpleInterest_Should_Calculate_Thirty_Day_Loan_Happy_Path()
    {
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 31);

        var result = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 100m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Simple
        });

        // Interest = 25000 * 0.12 / 365 * 30 = 246.575... -> 246.58
        var expectedInterest = 246.58m;
        var expectedTotalRepay = 25_000m + expectedInterest + 100m;

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalRepay, Is.EqualTo(expectedTotalRepay));
            Assert.That(result.Interest, Is.EqualTo(expectedInterest));
            Assert.That(result.Fees, Is.EqualTo(100m));
            Assert.That(result.Schedule, Has.Count.GreaterThan(0));
            Assert.That(result.Schedule[^1].CumulativeTotal, Is.EqualTo(expectedTotalRepay));
        });
    }

    [Test]
    public void CompoundInterest_Should_Exceed_SimpleInterest_For_Multi_Day_Loan()
    {
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 3, 31); // 90 days

        var simpleResult = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 100m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Simple
        });

        var compoundResult = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 100m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Compound
        });

        Assert.That(compoundResult.TotalRepay, Is.GreaterThan(simpleResult.TotalRepay));
        Assert.That(compoundResult.Interest, Is.GreaterThan(simpleResult.Interest));
    }

    [Test]
    public void Final_Schedule_Row_Should_Equal_TotalRepay_For_Both_Modes()
    {
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 4, 15);

        var simpleResult = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 100m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Simple
        });

        var compoundResult = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 100m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Compound
        });

        Assert.Multiple(() =>
        {
            Assert.That(simpleResult.Schedule[^1].CumulativeTotal, Is.EqualTo(simpleResult.TotalRepay));
            Assert.That(compoundResult.Schedule[^1].CumulativeTotal, Is.EqualTo(compoundResult.TotalRepay));
        });
    }

    [Test]
    public void EffectiveApr_Should_Equal_Nominal_Apr_When_No_Fees()
    {
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 31);

        var result = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0.12m,
            LiquidationLtv = 80m,
            Fees = 0m,
            StartDate = startDate,
            EndDate = endDate,
            InterestMode = BtcLoanInterestMode.Simple
        });

        Assert.That(result.EffectiveApr, Is.EqualTo(0.12m));
    }

    [Test]
    public void LiquidationPrice_Should_Be_Calculated_From_Total_Debt_And_Collateral()
    {
        var result = BtcLoanSimulationCalculator.Calculate(new BtcLoanSimulationInput
        {
            CollateralSats = 100_000_000L,
            PrincipalAmount = 25_000m,
            CurrencyCode = "USD",
            Apr = 0m,
            LiquidationLtv = 80m,
            Fees = 0m,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 31),
            InterestMode = BtcLoanInterestMode.Simple
        });

        // TotalRepay = 25_000, collateral = 1 BTC, LTV = 80%
        // Liquidation price = 25_000 / (1 * 0.8) = 31_250
        Assert.That(result.LiquidationPrice, Is.EqualTo(31_250m));
    }
}
