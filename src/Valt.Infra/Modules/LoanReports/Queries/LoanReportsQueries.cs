using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.LoanReports.Contracts;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Assets;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.LoanReports.Queries;

public class LoanReportsQueries : ILoanReportsQueries
{
    private const decimal SatoshisPerBitcoin = 100_000_000m;

    private readonly IAssetQueries _assetQueries;
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly IClock _clock;
    private readonly CurrencySettings _currencySettings;

    public LoanReportsQueries(
        IAssetQueries assetQueries,
        IReportDataProviderFactory reportDataProviderFactory,
        IClock clock,
        CurrencySettings currencySettings)
    {
        _assetQueries = assetQueries;
        _reportDataProviderFactory = reportDataProviderFactory;
        _clock = clock;
        _currencySettings = currencySettings;
    }

    public async Task<LoanReportsDataDto> GetLoanReportsAsync(GetLoanReportsQuery query)
    {
        var mainCurrencyCode = _currencySettings.MainFiatCurrency;
        var today = DateOnly.FromDateTime(_clock.GetCurrentDateTimeUtc());
        var provider = await _reportDataProviderFactory.CreateAsync();

        var assets = await _assetQueries.GetAllAsync();
        var loanAssets = assets.Where(a => a.AssetTypeId == (int)AssetTypes.BtcLoan).ToList();

        if (loanAssets.Count == 0)
        {
            return EmptyResult(mainCurrencyCode);
        }

        var loanTimelines = new List<LoanTimeline>();
        foreach (var loan in loanAssets)
        {
            var timeline = await _assetQueries.GetLoanStateTimelineAsync(loan.Id);
            if (timeline.Count == 0)
                continue;

            loanTimelines.Add(new LoanTimeline(loan.Name, timeline));
        }

        if (loanTimelines.Count == 0)
        {
            return EmptyResult(mainCurrencyCode);
        }

        var startMonth = new DateOnly(query.From.Year, query.From.Month, 1);
        var endMonth = new DateOnly(query.To.Year, query.To.Month, 1);

        var costMonths = new List<LoanCostMonthDto>();
        var distanceMonths = new List<LiquidationDistanceMonthDto>();

        for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var effectiveToday = today < monthEnd ? today : monthEnd;

            var combinedCost = 0m;
            var interest = 0m;
            var fees = 0m;

            decimal? minDistance = null;
            string closestLoanName = string.Empty;

            foreach (var loanTimeline in loanTimelines)
            {
                var effectiveSnapshot = loanTimeline.GetEffectiveSnapshot(monthEnd);
                if (effectiveSnapshot is null || effectiveSnapshot.StatusId != (int)LoanStatus.Active)
                    continue;

                var nextSnapshot = loanTimeline.GetNextSnapshot(effectiveSnapshot.EffectiveDate);

                // Interest accrual for the month, capped by next snapshot and today
                var accrualStart = effectiveSnapshot.EffectiveDate > month
                    ? effectiveSnapshot.EffectiveDate
                    : month;
                var accrualEndCandidate = nextSnapshot?.EffectiveDate ?? monthEnd;
                var accrualEnd = accrualEndCandidate < effectiveToday ? accrualEndCandidate : effectiveToday;

                if (accrualEnd >= accrualStart && !effectiveSnapshot.FixedTotalDebt.HasValue)
                {
                    var days = accrualEnd.DayNumber - accrualStart.DayNumber + 1;
                    interest += Math.Round(effectiveSnapshot.TotalBorrowed * effectiveSnapshot.Apr / 365 * days, 2);
                }

                // Fees from all snapshots effective inside this month (only if loan active at month-end)
                foreach (var snapshot in loanTimeline.Snapshots)
                {
                    if (snapshot.EffectiveDate.Year == month.Year && snapshot.EffectiveDate.Month == month.Month)
                    {
                        fees += snapshot.Fees;
                    }
                }

                // Convert interest and fees from the loan currency to the main currency
                try
                {
                    interest = ConvertToMainCurrency(interest, effectiveSnapshot.CurrencyCode, monthEnd, provider);
                    fees = ConvertToMainCurrency(fees, effectiveSnapshot.CurrencyCode, monthEnd, provider);
                }
                catch
                {
                    // Missing conversion rate for this loan/month: zero out its cost contribution
                    interest = 0m;
                    fees = 0m;
                }

                // Distance calculation at month-end
                var distance = CalculateDistanceToLiquidation(effectiveSnapshot, monthEnd, provider, query.CustomBtcPriceUsd);
                if (!minDistance.HasValue || distance < minDistance.Value)
                {
                    minDistance = distance;
                    closestLoanName = loanTimeline.Name;
                }
            }

            combinedCost = interest + fees;

            costMonths.Add(new LoanCostMonthDto
            {
                Month = month,
                CombinedCost = combinedCost,
                Interest = interest,
                Fees = fees
            });

            distanceMonths.Add(new LiquidationDistanceMonthDto
            {
                Month = month,
                DistanceToLiquidation = minDistance ?? 0m,
                ClosestLoanName = closestLoanName
            });
        }

        return new LoanReportsDataDto
        {
            CostMonths = costMonths,
            DistanceMonths = distanceMonths,
            HasActiveLoans = true,
            PrimaryCurrency = mainCurrencyCode
        };
    }

    private decimal CalculateDistanceToLiquidation(LoanStateSnapshotDTO snapshot, DateOnly monthEnd, IReportDataProvider provider, decimal? customBtcPriceUsd)
    {
        try
        {
            var btcPriceUsd = customBtcPriceUsd ?? provider.GetUsdBitcoinPriceAt(monthEnd);
            var loanCurrency = FiatCurrency.GetFromCode(snapshot.CurrencyCode);
            var fiatRate = provider.GetFiatRateAt(monthEnd, loanCurrency);

            var btcPriceInLoanCurrency = btcPriceUsd * fiatRate;
            if (btcPriceInLoanCurrency == 0)
                return 0m;

            var collateralBtc = snapshot.CollateralSats / SatoshisPerBitcoin;
            var collateralValue = collateralBtc * btcPriceInLoanCurrency;
            if (collateralValue == 0)
                return 0m;

            var currentLtv = Math.Round(snapshot.CurrentTotalDebt / collateralValue * 100, 2);
            var distance = Math.Max(0, snapshot.LiquidationLtv - currentLtv);

            return Math.Round(distance, 2);
        }
        catch
        {
            // Missing historical rates: clamp to zero distance so the chart stays safe
            return 0m;
        }
    }

    private decimal ConvertToMainCurrency(decimal amount, string currencyCode, DateOnly date, IReportDataProvider provider)
    {
        if (amount == 0)
            return 0m;

        var mainCurrencyCode = _currencySettings.MainFiatCurrency;
        if (currencyCode == mainCurrencyCode)
            return amount;

        var loanCurrency = FiatCurrency.GetFromCode(currencyCode);
        var loanRate = provider.GetFiatRateAt(date, loanCurrency);
        if (loanRate == 0)
            throw new ApplicationException($"Missing fiat rate for {currencyCode}");

        var usdAmount = amount / loanRate;

        if (mainCurrencyCode == FiatCurrency.Usd.Code)
            return Math.Round(usdAmount, 2);

        var mainCurrency = FiatCurrency.GetFromCode(mainCurrencyCode);
        var mainRate = provider.GetFiatRateAt(date, mainCurrency);
        if (mainRate == 0)
            throw new ApplicationException($"Missing fiat rate for {mainCurrencyCode}");

        return Math.Round(usdAmount * mainRate, 2);
    }

    private static LoanReportsDataDto EmptyResult(string mainCurrencyCode) => new()
    {
        CostMonths = new List<LoanCostMonthDto>(),
        DistanceMonths = new List<LiquidationDistanceMonthDto>(),
        HasActiveLoans = false,
        PrimaryCurrency = mainCurrencyCode
    };

    private sealed class LoanTimeline
    {
        public string Name { get; }
        public IReadOnlyList<LoanStateSnapshotDTO> Snapshots { get; }

        public LoanTimeline(string name, IReadOnlyList<LoanStateSnapshotDTO> snapshots)
        {
            Name = name;
            Snapshots = snapshots.OrderBy(s => s.EffectiveDate).ToList().AsReadOnly();
        }

        public LoanStateSnapshotDTO? GetEffectiveSnapshot(DateOnly asOfDate)
        {
            return Snapshots.LastOrDefault(s => s.EffectiveDate <= asOfDate);
        }

        public LoanStateSnapshotDTO? GetNextSnapshot(DateOnly afterDate)
        {
            return Snapshots.FirstOrDefault(s => s.EffectiveDate > afterDate);
        }
    }
}
