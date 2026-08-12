using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.App.Kernel.Notifications;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.LoanReports.Queries;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Settings;
using Valt.Tests.Builders;

namespace Valt.Tests.Reports;

[TestFixture]
public class LoanReportsQueriesTests : DatabaseTest
{
    protected override async Task SeedDatabase()
    {
        // Dummy transaction is required so ReportDataProvider loads BTC/fiat rates.
        var account = FiatAccountBuilder.AnAccount()
            .WithName("Dummy")
            .WithFiatCurrency(FiatCurrency.Usd)
            .WithValue(0m)
            .Build();
        _localDatabase.GetAccounts().Insert(account);

        var category = CategoryBuilder.ACategory().Build();
        _localDatabase.GetCategories().Insert(category);

        var transaction = TransactionBuilder.ATransaction()
            .WithDate(new DateOnly(2024, 1, 1))
            .WithCategoryId(new CategoryId(category.Id.ToString()))
            .AsFiatIncome(new AccountId(account.Id.ToString()), 1m)
            .BuildDomainObject();

        await _transactionRepository.SaveTransactionAsync(transaction);

        var initialDate = new DateTime(2024, 01, 01);
        var finalDate = new DateTime(2025, 12, 31);
        var currentDate = initialDate;
        while (currentDate <= finalDate)
        {
            _priceDatabase.GetBitcoinData().Insert(new BitcoinDataEntity() { Date = currentDate, Price = 100000m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Brl.Code, Price = 5.5m });
            _priceDatabase.GetFiatData().Insert(new FiatDataEntity() { Date = currentDate, Currency = FiatCurrency.Usd.Code, Price = 1m });
            currentDate = currentDate.AddDays(1);
        }

        await base.SeedDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _localDatabase.GetAssets().DeleteAll();
    }

    [Test]
    public async Task Should_Return_Empty_When_No_Active_Loans()
    {
        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31), new DateTime(2025, 3, 15));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasActiveLoans, Is.False);
            Assert.That(result.CostMonths, Is.Empty);
            Assert.That(result.DistanceMonths, Is.Empty);
        }
    }

    [Test]
    public async Task Should_Accrue_Interest_Per_Month_For_Apr_Loan()
    {
        // Active APR loan from 2025-01-01, USD, 50k borrowed at 12% APR
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 50_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));
        // 50_000 * 0.12 / 365 * 31 = 509.59 USD; converted to BRL at 5.5
        var expectedInterest = Math.Round(Math.Round(50_000m * 0.12m / 365m * 31m, 2) * 5.5m, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.Interest, Is.EqualTo(expectedInterest));
            Assert.That(january.Fees, Is.EqualTo(0m));
            Assert.That(january.CombinedCost, Is.EqualTo(expectedInterest));
        }
    }

    [Test]
    public async Task Should_Assign_Fees_To_Snapshot_Effective_Month()
    {
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 250m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 2, 15), totalBorrowed: 50_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31), new DateTime(2025, 3, 31));

        var february = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 2, 1));
        var march = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 3, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(february.Fees, Is.EqualTo(1375.00m)); // 250 USD fee converted to BRL at 5.5
            Assert.That(march.Fees, Is.EqualTo(0m));
        }
    }

    [Test]
    public async Task Should_Not_Accrue_Interest_For_Fixed_Debt_Loan()
    {
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 100m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 50_000m, interestAccruedUntilDate: 0m)
            .Build();

        // Apply a fixed total debt by adding a snapshot with FixedTotalDebt set
        // The builder currently does not expose FixedTotalDebt on snapshot, so we rely on the
        // pre-existing BtcLoanDetails FixedTotalDebt constructor overload. Build with fixed debt.
        asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithDetails(new BtcLoanDetails(
                "Test",
                100_000_000,
                50_000m,
                FiatCurrency.Usd.Code,
                0.12m,
                50m,
                80m,
                70m,
                100m,
                new DateOnly(2025, 1, 1),
                new DateOnly(2026, 1, 1),
                LoanStatus.Active,
                100_000m,
                fixedTotalDebt: 52_000m))
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.Interest, Is.EqualTo(0m));
            Assert.That(january.Fees, Is.EqualTo(550.00m)); // 100 USD fee converted to BRL at 5.5
            Assert.That(january.CombinedCost, Is.EqualTo(550.00m));
        }
    }

    [Test]
    public async Task Should_Skip_Loan_Created_After_Month_End()
    {
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 2, 15), totalBorrowed: 50_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));
        var januaryDistance = result.DistanceMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.CombinedCost, Is.EqualTo(0m));
            Assert.That(januaryDistance.DistanceToLiquidation, Is.EqualTo(0m));
        }
    }

    [Test]
    public async Task Should_Skip_Repaid_Loan_Before_Month_End()
    {
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 50_000m)
            .WithSnapshot(new DateOnly(2025, 1, 15), totalBorrowed: 50_000m, status: LoanStatus.Repaid)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));
        var januaryDistance = result.DistanceMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.CombinedCost, Is.EqualTo(0m));
            Assert.That(januaryDistance.DistanceToLiquidation, Is.EqualTo(0m));
        }
    }

    [Test]
    public async Task Should_Convert_Cost_From_Brl_To_Main_Currency()
    {
        var mainCurrency = new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Usd.Code
        };

        // BRL loan: 275_000 BRL = 50_000 USD at 5.5 rate
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 275_000m,
                currentBtcPrice: 550_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active,
                currencyCode: FiatCurrency.Brl.Code)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 275_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 31),
            new DateTime(2025, 1, 31),
            mainCurrency);

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));
        var expectedInterestUsd = Math.Round(50_000m * 0.12m / 365m * 31m, 2);

        Assert.That(january.CombinedCost, Is.EqualTo(expectedInterestUsd));
    }

    [Test]
    public async Task Should_Compute_Worst_Case_Distance_Per_Month_End()
    {
        // Two loans: one riskier than the other. Worst case = closest to liquidation.
        var safeLoan = AssetBuilder.ABtcLoan(
                platformName: "Safe",
                collateralSats: 100_000_000,
                loanAmount: 30_000m,
                currentBtcPrice: 100_000m)
            .WithName("Safe")
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 30_000m)
            .Build();

        var riskyLoan = AssetBuilder.ABtcLoan(
                platformName: "Risky",
                collateralSats: 100_000_000,
                loanAmount: 70_000m,
                currentBtcPrice: 100_000m)
            .WithName("Risky")
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 70_000m)
            .Build();

        await _assetRepository.SaveAsync(safeLoan);
        await _assetRepository.SaveAsync(riskyLoan);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.DistanceMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        // Safe: LTV 30%, distance 50pp. Risky: LTV 70%, distance 10pp. Worst case = Risky.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.DistanceToLiquidation, Is.EqualTo(10m));
            Assert.That(january.ClosestLoanName, Is.EqualTo("Risky"));
        }
    }

    [Test]
    public async Task Should_Clamp_Distance_To_Zero_When_Liquidated()
    {
        // LTV 90% > liquidation 80% => distance clamped to 0
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Liquidated",
                collateralSats: 100_000_000,
                loanAmount: 90_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 90_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.DistanceMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        Assert.That(january.DistanceToLiquidation, Is.EqualTo(0m));
    }

    [Test]
    public async Task Should_Include_Current_Incomplete_Month()
    {
        var asset = AssetBuilder.ABtcLoan(
                platformName: "Test",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 50_000m)
            .Build();

        await _assetRepository.SaveAsync(asset);

        // Clock set to 2025-01-15, so March accrues only up to Jan 15
        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 15));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));
        var expectedInterest = Math.Round(Math.Round(50_000m * 0.12m / 365m * 15m, 2) * 5.5m, 2);

        Assert.That(january.Interest, Is.EqualTo(expectedInterest));
    }

    [Test]
    public async Task Should_Not_Compound_Monthly_Cost_For_Multiple_Mixed_Currency_Loans()
    {
        // BRL loan is first alphabetically; the old implementation would re-convert
        // the running BRL total as if it were USD when processing the second loan.
        var brlLoan = AssetBuilder.ABtcLoan(
                platformName: "BRL",
                collateralSats: 100_000_000,
                loanAmount: 275_000m,
                currentBtcPrice: 550_000m)
            .WithName("BRL Loan")
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active,
                currencyCode: FiatCurrency.Brl.Code)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 275_000m)
            .Build();

        var usdLoan = AssetBuilder.ABtcLoan(
                platformName: "USD",
                collateralSats: 100_000_000,
                loanAmount: 50_000m,
                currentBtcPrice: 100_000m)
            .WithName("USD Loan")
            .WithBtcLoanDetails(
                apr: 0.12m,
                liquidationLtv: 80m,
                marginCallLtv: 70m,
                fees: 0m,
                status: LoanStatus.Active)
            .WithSnapshot(new DateOnly(2025, 1, 1), totalBorrowed: 50_000m)
            .Build();

        await _assetRepository.SaveAsync(brlLoan);
        await _assetRepository.SaveAsync(usdLoan);

        var result = await ExecuteQuery(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), new DateTime(2025, 1, 31));

        var january = result.CostMonths.Single(m => m.Month == new DateOnly(2025, 1, 1));

        var usdInterest = Math.Round(50_000m * 0.12m / 365m * 31m, 2);
        var usdInterestInBrl = Math.Round(usdInterest * 5.5m, 2);
        var brlInterest = Math.Round(275_000m * 0.12m / 365m * 31m, 2);
        var expectedCombined = usdInterestInBrl + brlInterest;

        // Old implementation: accumulated BRL interest + USD interest, then converted the whole sum as USD.
        var compoundingBugValue = Math.Round((brlInterest + usdInterest) * 5.5m, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(january.CombinedCost, Is.EqualTo(expectedCombined));
            Assert.That(january.CombinedCost, Is.Not.EqualTo(compoundingBugValue));
        }
    }

    private async Task<LoanReportsDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>())
        {
            MainFiatCurrency = FiatCurrency.Brl.Code
        });
    }

    private async Task<LoanReportsDataDto> ExecuteQuery(DateOnly from, DateOnly to, DateTime clockDate, CurrencySettings currencySettings)
    {
        return await ExecuteQuery(from, to, new FakeClock(clockDate), currencySettings);
    }

    private async Task<LoanReportsDataDto> ExecuteQuery(DateOnly from, DateOnly to, IClock clock, CurrencySettings currencySettings)
    {
        var factory = new ReportDataProviderFactory(_priceDatabase, _localDatabase, clock);
        var sut = new LoanReportsQueries(_assetQueries, factory, clock, currencySettings);

        return await sut.GetLoanReportsAsync(new GetLoanReportsQuery
        {
            From = from,
            To = to
        });
    }
}
