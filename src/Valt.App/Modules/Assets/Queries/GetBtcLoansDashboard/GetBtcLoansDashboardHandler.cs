using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;

namespace Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;

internal sealed class GetBtcLoansDashboardHandler : IQueryHandler<GetBtcLoansDashboardQuery, BtcLoansDashboardDTO>
{
    private const decimal SatsPerBtc = 100_000_000m;

    private readonly IAssetQueries _assetQueries;

    public GetBtcLoansDashboardHandler(IAssetQueries assetQueries)
    {
        _assetQueries = assetQueries;
    }

    public async Task<BtcLoansDashboardDTO> HandleAsync(
        GetBtcLoansDashboardQuery query,
        CancellationToken ct = default)
    {
        var assets = await _assetQueries.GetAllAsync();

        var loans = assets
            .Where(a => a.AssetTypeId == (int)AssetTypes.BtcLoan
                        && a.LoanStatusId == (int)LoanStatus.Active)
            .ToList();

        if (loans.Count == 0)
            return BtcLoansDashboardDTO.Empty(query.TotalBtcStackSats);

        var mainCurrencyCode = query.MainCurrencyCode;
        var btcPriceUsd = query.CustomBtcPriceUsd ?? query.BtcPriceUsd;
        var fiatRates = query.FiatRates;

        decimal ConvertToMain(decimal value, string currency)
        {
            if (currency == mainCurrencyCode)
                return value;
            if (fiatRates is null)
                return 0m;
            var valueInUsd = currency == FiatCurrency.Usd.Code
                ? value
                : fiatRates.TryGetValue(currency, out var rate) && rate > 0
                    ? value / rate
                    : 0m;
            if (mainCurrencyCode == FiatCurrency.Usd.Code)
                return valueInUsd;
            return fiatRates.TryGetValue(mainCurrencyCode, out var mainRate)
                ? valueInUsd * mainRate
                : 0m;
        }

        // Per-loan precomputed values
        var loanData = loans.Select(loan => new
        {
            Loan = loan,
            DebtInMain = ConvertToMain(loan.TotalDebt ?? 0m, loan.CurrencyCode),
            AccruedInMain = ConvertToMain(loan.AccruedInterest ?? 0m, loan.CurrencyCode),
            FeesInMain = ConvertToMain(loan.Fees ?? 0m, loan.CurrencyCode)
        }).ToList();

        var totalDebtMain = loanData.Sum(x => x.DebtInMain);
        var totalAccruedMain = loanData.Sum(x => x.AccruedInMain);
        var totalFeesMain = loanData.Sum(x => x.FeesInMain);

        // Helper to calculate LTV at simulated price
        decimal? CalculateSimulatedLtv(AssetDTO loan)
        {
            if (!btcPriceUsd.HasValue || btcPriceUsd.Value <= 0 || !loan.CollateralSats.HasValue || loan.CollateralSats.Value <= 0)
                return loan.CurrentLtv;

            var collateralBtc = loan.CollateralSats.Value / SatsPerBtc;
            var collateralUsd = collateralBtc * btcPriceUsd.Value;
            decimal collateralInMain;
            if (mainCurrencyCode == FiatCurrency.Usd.Code)
                collateralInMain = collateralUsd;
            else if (fiatRates is not null && fiatRates.TryGetValue(mainCurrencyCode, out var mainRate))
                collateralInMain = collateralUsd * mainRate;
            else
                return loan.CurrentLtv;

            if (collateralInMain == 0)
                return loan.CurrentLtv;

            var debtInMain = ConvertToMain(loan.TotalDebt ?? 0m, loan.CurrencyCode);
            return Math.Round(debtInMain / collateralInMain * 100, 2);
        }

        // Debt-weighted averages (skip loans without CurrentLtv when computing LTV avg)
        decimal weightedLtv = 0m;
        var loansWithLtv = loanData.Where(x => x.Loan.CurrentLtv.HasValue && x.DebtInMain > 0).ToList();
        var debtWithLtv = loansWithLtv.Sum(x => x.DebtInMain);
        if (debtWithLtv > 0)
            weightedLtv = loansWithLtv.Sum(x => (CalculateSimulatedLtv(x.Loan) ?? x.Loan.CurrentLtv!.Value) * x.DebtInMain) / debtWithLtv;

        decimal weightedApr = 0m;
        if (totalDebtMain > 0)
            weightedApr = loanData.Sum(x => (x.Loan.Apr ?? 0m) * 100m * x.DebtInMain) / totalDebtMain;

        // Collateral
        var totalCollateralSats = loans.Sum(l => l.CollateralSats ?? 0L);
        var collateralPercent = query.TotalBtcStackSats > 0
            ? (decimal)totalCollateralSats / query.TotalBtcStackSats * 100m
            : 0m;
        var freeBtcSats = query.TotalBtcStackSats - totalCollateralSats;

        decimal totalCollateralFiatMain = 0m;
        if (btcPriceUsd.HasValue && btcPriceUsd.Value > 0)
        {
            var collateralBtc = totalCollateralSats / SatsPerBtc;
            var collateralUsd = collateralBtc * btcPriceUsd.Value;
            totalCollateralFiatMain = mainCurrencyCode == FiatCurrency.Usd.Code
                ? collateralUsd
                : fiatRates is not null && fiatRates.TryGetValue(mainCurrencyCode, out var mainRate)
                    ? collateralUsd * mainRate
                    : 0m;
        }

        // Risk (use simulated LTV when custom price is active)
        var loansWithLtvForRisk = loans.Where(l => l.CurrentLtv.HasValue && l.LiquidationLtv.HasValue).ToList();
        decimal highestLtv = 0m;
        decimal closestDistance = 0m;
        string closestLoanName = string.Empty;
        if (loansWithLtvForRisk.Count > 0)
        {
            var simulatedLtvs = loansWithLtvForRisk
                .Select(l => new { Loan = l, SimulatedLtv = CalculateSimulatedLtv(l) ?? l.CurrentLtv!.Value })
                .ToList();

            highestLtv = simulatedLtvs.Max(x => x.SimulatedLtv);
            var closest = simulatedLtvs
                .OrderBy(x => x.Loan.LiquidationLtv!.Value - x.SimulatedLtv)
                .First();
            closestDistance = closest.Loan.LiquidationLtv!.Value - closest.SimulatedLtv;
            closestLoanName = closest.Loan.Name;
        }

        // Worst-case liquidation BTC price (USD): per loan = LoanAmount / (LiquidationLtv/100 * CollateralBtc),
        // converted to USD via fiatRates[loan.CurrencyCode]. Pick the max — the highest BTC price at which any loan still triggers.
        decimal worstCaseLiqUsd = 0m;
        if (fiatRates is not null)
        {
            foreach (var loan in loans)
            {
                if (!loan.LoanAmount.HasValue || !loan.LiquidationLtv.HasValue || !loan.CollateralSats.HasValue)
                    continue;
                if (loan.LiquidationLtv.Value <= 0 || loan.CollateralSats.Value <= 0)
                    continue;
                var collateralBtc = loan.CollateralSats.Value / SatsPerBtc;
                var liqPriceLoanCcy = loan.LoanAmount.Value / (loan.LiquidationLtv.Value / 100m * collateralBtc);
                decimal liqPriceUsd;
                if (loan.CurrencyCode == FiatCurrency.Usd.Code)
                    liqPriceUsd = liqPriceLoanCcy;
                else if (fiatRates.TryGetValue(loan.CurrencyCode, out var rate) && rate > 0)
                    liqPriceUsd = liqPriceLoanCcy / rate;
                else
                    continue;
                if (liqPriceUsd > worstCaseLiqUsd)
                    worstCaseLiqUsd = liqPriceUsd;
            }
        }

        // Health buckets (recalculate when custom price is active)
        int healthy, warning, danger;
        if (query.CustomBtcPriceUsd.HasValue)
        {
            healthy = 0;
            warning = 0;
            danger = 0;
            foreach (var loan in loans)
            {
                var simulatedLtv = CalculateSimulatedLtv(loan);
                if (!simulatedLtv.HasValue || !loan.LiquidationLtv.HasValue)
                    continue;

                if (simulatedLtv.Value >= loan.LiquidationLtv.Value)
                    danger++;
                else if (loan.MarginCallLtv.HasValue && simulatedLtv.Value >= loan.MarginCallLtv.Value)
                    warning++;
                else
                    healthy++;
            }
        }
        else
        {
            healthy = loans.Count(l => l.LoanHealthStatusId == (int)LoanHealthStatus.Healthy);
            warning = loans.Count(l => l.LoanHealthStatusId == (int)LoanHealthStatus.Warning);
            danger = loans.Count(l => l.LoanHealthStatusId == (int)LoanHealthStatus.Danger);
        }

        // Total debt in BTC
        decimal totalDebtInBtc = 0m;
        if (btcPriceUsd.HasValue && btcPriceUsd.Value > 0 && totalDebtMain > 0)
        {
            decimal totalDebtInUsd;
            if (mainCurrencyCode == FiatCurrency.Usd.Code)
                totalDebtInUsd = totalDebtMain;
            else if (fiatRates is not null && fiatRates.TryGetValue(mainCurrencyCode, out var mainRate) && mainRate > 0)
                totalDebtInUsd = totalDebtMain / mainRate;
            else
                totalDebtInUsd = 0m;

            if (totalDebtInUsd > 0)
                totalDebtInBtc = totalDebtInUsd / btcPriceUsd.Value;
        }

        // Time
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var avgLoanAgeDays = loans
            .Where(l => l.LoanStartDate.HasValue)
            .Select(l => (decimal)(today.DayNumber - l.LoanStartDate!.Value.DayNumber))
            .DefaultIfEmpty(0m)
            .Average();

        var nextRepayment = loans
            .Where(l => l.RepaymentDate.HasValue)
            .OrderBy(l => l.RepaymentDate!.Value)
            .FirstOrDefault();

        return new BtcLoansDashboardDTO
        {
            HasActiveLoans = true,
            ActiveLoansCount = loans.Count,
            CollateralPercentOfStack = Math.Round(collateralPercent, 2),
            DebtWeightedAvgLtv = Math.Round(weightedLtv, 2),
            DebtWeightedAvgApr = Math.Round(weightedApr, 2),
            TotalDebtInMainCurrency = Math.Round(totalDebtMain, 2),
            TotalDebtInBtc = Math.Round(totalDebtInBtc, 8),
            HighestLtv = Math.Round(highestLtv, 2),
            ClosestDistanceToLiquidationLtv = Math.Round(closestDistance, 2),
            ClosestLoanName = closestLoanName,
            WorstCaseLiquidationBtcPriceUsd = Math.Round(worstCaseLiqUsd, 2),
            HealthyCount = healthy,
            WarningCount = warning,
            DangerCount = danger,
            TotalCollateralSats = totalCollateralSats,
            TotalCollateralFiatInMainCurrency = Math.Round(totalCollateralFiatMain, 2),
            FreeBtcSats = freeBtcSats,
            TotalBtcStackSats = query.TotalBtcStackSats,
            TotalAccruedInterestInMainCurrency = Math.Round(totalAccruedMain, 2),
            TotalFeesPaidInMainCurrency = Math.Round(totalFeesMain, 2),
            NextRepaymentDate = nextRepayment?.RepaymentDate,
            DaysUntilNextRepayment = nextRepayment?.DaysUntilRepayment,
            NextRepaymentLoanName = nextRepayment?.Name,
            AverageLoanAgeDays = Math.Round(avgLoanAgeDays, 2)
        };
    }
}
