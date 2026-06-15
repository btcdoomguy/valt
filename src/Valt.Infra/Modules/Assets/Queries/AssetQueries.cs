using Valt.App.Modules.Assets.Contracts;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Assets.Queries;

internal sealed class AssetQueries : IAssetQueries
{
    private readonly ILocalDatabase _localDatabase;

    public AssetQueries(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IReadOnlyList<AssetDTO>> GetAllAsync()
    {
        var entities = _localDatabase.GetAssets()
            .FindAll()
            .OrderByDescending(x => x.Visible)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var dtos = entities.Select(MapToDto).ToList();
        return Task.FromResult<IReadOnlyList<AssetDTO>>(dtos);
    }

    public Task<IReadOnlyList<AssetDTO>> GetVisibleAsync()
    {
        var entities = _localDatabase.GetAssets()
            .Find(x => x.Visible)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var dtos = entities.Select(MapToDto).ToList();
        return Task.FromResult<IReadOnlyList<AssetDTO>>(dtos);
    }

    public Task<AssetDTO?> GetByIdAsync(string id)
    {
        var entity = _localDatabase.GetAssets().FindById(new LiteDB.ObjectId(id));
        return Task.FromResult(entity is null ? null : MapToDto(entity));
    }

    public Task<AssetSummaryDTO> GetSummaryAsync(string mainCurrencyCode, decimal? btcPriceUsd = null, IReadOnlyDictionary<string, decimal>? fiatRates = null, decimal? customBtcPriceUsd = null)
    {
        var entities = _localDatabase.GetAssets().FindAll().ToList();

        var includedAssets = entities
            .Where(x => x.IncludeInNetWorth)
            .Select(e => e.AsDomainObject())
            .ToList();

        var valuesByCurrency = includedAssets
            .GroupBy(a => a.GetCurrencyCode())
            .Select(g => new AssetValueByCurrencyDTO
            {
                CurrencyCode = g.Key,
                TotalValue = g.Sum(GetValueForSummary),
                AssetCount = g.Count()
            })
            .ToList<AssetValueByCurrencyDTO>();

        var (totalValueInMainCurrency, totalAssetsValueInMainCurrency, totalLiabilitiesInMainCurrency, liabilitiesCount, totalValueInSats) =
            CalculateSummaryTotals(includedAssets, mainCurrencyCode, fiatRates, customBtcPriceUsd ?? btcPriceUsd);

        var summary = new AssetSummaryDTO
        {
            TotalAssets = entities.Count,
            VisibleAssets = entities.Count(x => x.Visible),
            AssetsIncludedInNetWorth = includedAssets.Count,
            ValuesByCurrency = valuesByCurrency,
            TotalAssetsValueInMainCurrency = Math.Round(totalAssetsValueInMainCurrency, 2),
            TotalLiabilitiesInMainCurrency = Math.Round(totalLiabilitiesInMainCurrency, 2),
            LiabilitiesCount = liabilitiesCount,
            TotalValueInMainCurrency = Math.Round(totalValueInMainCurrency, 2),
            TotalValueInSats = totalValueInSats
        };

        return Task.FromResult(summary);
    }

    private static (decimal TotalValue, decimal TotalAssetsValue, decimal TotalLiabilitiesValue, int LiabilitiesCount, long TotalSats)
        CalculateSummaryTotals(IReadOnlyList<Asset> includedAssets, string mainCurrencyCode, IReadOnlyDictionary<string, decimal>? fiatRates, decimal? effectiveBtcPriceUsd)
    {
        var totalValueInMainCurrency = 0m;
        var totalAssetsValueInMainCurrency = 0m;
        var totalLiabilitiesInMainCurrency = 0m;
        var liabilitiesCount = 0;
        long totalValueInSats = 0;

        if (!effectiveBtcPriceUsd.HasValue || fiatRates is null)
            return (totalValueInMainCurrency, totalAssetsValueInMainCurrency, totalLiabilitiesInMainCurrency, liabilitiesCount, totalValueInSats);

        foreach (var asset in includedAssets)
        {
            var value = GetValueForSummary(asset);
            var currency = asset.GetCurrencyCode();
            var isLiability = asset.Details is BtcLoanDetails;

            var valueInUsd = ConvertToUsd(value, currency, fiatRates);
            var valueInMainCurrency = ConvertToMainCurrency(value, valueInUsd, currency, mainCurrencyCode, fiatRates);

            totalValueInMainCurrency += valueInMainCurrency;

            if (isLiability)
            {
                var debtValue = ((BtcLoanDetails)asset.Details).CalculateTotalDebt();
                var debtInUsd = ConvertToUsd(debtValue, currency, fiatRates);
                var debtInMainCurrency = ConvertToMainCurrency(debtValue, debtInUsd, currency, mainCurrencyCode, fiatRates);
                totalLiabilitiesInMainCurrency += debtInMainCurrency;
                liabilitiesCount++;
            }
            else
            {
                totalAssetsValueInMainCurrency += valueInMainCurrency;
            }

            if (effectiveBtcPriceUsd.Value > 0)
            {
                var btcAmount = valueInUsd / effectiveBtcPriceUsd.Value;
                totalValueInSats += (long)(btcAmount * 100_000_000);
            }
        }

        return (totalValueInMainCurrency, totalAssetsValueInMainCurrency, totalLiabilitiesInMainCurrency, liabilitiesCount, totalValueInSats);
    }

    private static decimal ConvertToUsd(decimal value, string currency, IReadOnlyDictionary<string, decimal> fiatRates)
    {
        if (currency == FiatCurrency.Usd.Code)
            return value;

        return fiatRates.TryGetValue(currency, out var rate) && rate > 0
            ? value / rate
            : 0m;
    }

    private static decimal ConvertToMainCurrency(decimal valueInOriginalCurrency, decimal valueInUsd, string originalCurrency, string mainCurrencyCode, IReadOnlyDictionary<string, decimal> fiatRates)
    {
        if (originalCurrency == mainCurrencyCode)
            return valueInOriginalCurrency;

        if (fiatRates.TryGetValue(mainCurrencyCode, out var mainRate))
            return valueInUsd * mainRate;

        return 0m;
    }

    /// <summary>
    /// Gets the value to use for portfolio summary calculations.
    /// For leveraged positions, returns only the P&amp;L (not the full position value).
    /// For BTC loans, returns -TotalDebt (pure liability, collateral tracked separately).
    /// For BTC lending, returns amount lent + earned interest.
    /// For other assets, returns the current value.
    /// </summary>
    private static decimal GetValueForSummary(Asset asset)
    {
        return asset.Details switch
        {
            LeveragedPositionDetails leveraged => leveraged.CalculatePnL(leveraged.CurrentPrice),
            BtcLoanDetails btcLoan => btcLoan.CalculateCurrentValue(btcLoan.CurrentBtcPriceInLoanCurrency),
            BtcLendingDetails btcLending => btcLending.CalculateCurrentValue(0),
            _ => asset.GetCurrentValue()
        };
    }

    private static AssetDTO MapToDto(AssetEntity entity)
    {
        var asset = entity.AsDomainObject();

        return asset.Details switch
        {
            BasicAssetDetails basic => MapBasicAsset(entity, asset, basic),
            RealEstateAssetDetails realEstate => MapRealEstateAsset(entity, asset, realEstate),
            LeveragedPositionDetails leveraged => MapLeveragedPosition(entity, asset, leveraged),
            BtcLoanDetails btcLoan => MapBtcLoan(entity, asset, btcLoan),
            BtcLendingDetails btcLending => MapBtcLending(entity, asset, btcLending),
            _ => CreateBaseDto(entity, asset)
        };
    }

    private static AssetDTO CreateBaseDto(AssetEntity entity, Asset asset)
    {
        return new AssetDTO
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            AssetTypeId = entity.AssetTypeId,
            AssetTypeName = ((AssetTypes)entity.AssetTypeId).ToString(),
            Icon = entity.Icon ?? string.Empty,
            IncludeInNetWorth = entity.IncludeInNetWorth,
            Visible = entity.Visible,
            LastPriceUpdateAt = entity.LastPriceUpdateAt,
            CreatedAt = entity.CreatedAt,
            DisplayOrder = entity.DisplayOrder,
            GroupId = entity.GroupId?.ToString(),
            CurrentPrice = asset.GetCurrentPrice(),
            CurrentValue = asset.GetCurrentValue(),
            CurrencyCode = asset.GetCurrencyCode()
        };
    }

    private static AssetDTO MapBasicAsset(AssetEntity entity, Asset asset, BasicAssetDetails basic)
    {
        var dto = CreateBaseDto(entity, asset);
        decimal? pnl = null;
        decimal? pnlPercentage = null;

        if (basic.AcquisitionPrice.HasValue)
        {
            pnl = basic.CalculatePnL();
            pnlPercentage = basic.CalculatePnLPercentage();
        }

        return dto with
        {
            Quantity = basic.Quantity,
            Symbol = basic.Symbol,
            PriceSourceId = (int)basic.PriceSource,
            AcquisitionDate = basic.AcquisitionDate,
            AcquisitionPrice = basic.AcquisitionPrice,
            PnL = pnl,
            PnLPercentage = pnlPercentage
        };
    }

    private static AssetDTO MapRealEstateAsset(AssetEntity entity, Asset asset, RealEstateAssetDetails realEstate)
    {
        var dto = CreateBaseDto(entity, asset);
        decimal? pnl = null;
        decimal? pnlPercentage = null;

        if (realEstate.AcquisitionPrice.HasValue)
        {
            pnl = realEstate.CalculatePnL();
            pnlPercentage = realEstate.CalculatePnLPercentage();
        }

        return dto with
        {
            Address = realEstate.Address,
            MonthlyRentalIncome = realEstate.MonthlyRentalIncome,
            AcquisitionDate = realEstate.AcquisitionDate,
            AcquisitionPrice = realEstate.AcquisitionPrice,
            PnL = pnl,
            PnLPercentage = pnlPercentage
        };
    }

    private static AssetDTO MapLeveragedPosition(AssetEntity entity, Asset asset, LeveragedPositionDetails leveraged)
    {
        var dto = CreateBaseDto(entity, asset);

        return dto with
        {
            Collateral = leveraged.Collateral,
            EntryPrice = leveraged.EntryPrice,
            Leverage = leveraged.Leverage,
            LiquidationPrice = leveraged.LiquidationPrice,
            IsLong = leveraged.IsLong,
            Symbol = leveraged.Symbol,
            PriceSourceId = (int)leveraged.PriceSource,
            PnL = leveraged.CalculatePnL(leveraged.CurrentPrice),
            PnLPercentage = leveraged.CalculatePnLPercentage(leveraged.CurrentPrice),
            DistanceToLiquidation = leveraged.CalculateDistanceToLiquidation(leveraged.CurrentPrice),
            IsAtRisk = leveraged.IsAtRisk(leveraged.CurrentPrice),
            PositionSize = leveraged.PositionSize,
            InputModeId = (int)leveraged.InputMode
        };
    }

    private static AssetDTO MapBtcLoan(AssetEntity entity, Asset asset, BtcLoanDetails btcLoan)
    {
        var dto = CreateBaseDto(entity, asset);
        var snapshot = btcLoan.Snapshots.MaxBy(s => s.EffectiveDate);
        decimal? currentLtv = null;
        int? loanHealthStatusId = null;
        string? loanHealthStatusName = null;
        decimal? distanceToLiquidationLtv = null;

        var currentBtcPriceInLoanCurrency = snapshot?.CurrentBtcPriceInLoanCurrency ?? btcLoan.CurrentBtcPriceInLoanCurrency;

        if (currentBtcPriceInLoanCurrency > 0)
        {
            currentLtv = btcLoan.CalculateCurrentLtv(currentBtcPriceInLoanCurrency);
            var healthStatus = btcLoan.CalculateHealthStatus(currentBtcPriceInLoanCurrency);
            loanHealthStatusId = (int)healthStatus;
            loanHealthStatusName = healthStatus.ToString();
            distanceToLiquidationLtv = btcLoan.CalculateDistanceToLiquidation(currentBtcPriceInLoanCurrency);
        }

        return dto with
        {
            PlatformName = btcLoan.PlatformName,
            CollateralSats = snapshot?.CollateralSats ?? btcLoan.CollateralSats,
            LoanAmount = snapshot?.LoanAmount ?? btcLoan.LoanAmount,
            Apr = snapshot?.Apr ?? btcLoan.Apr,
            CurrentLtv = currentLtv,
            InitialLtv = btcLoan.InitialLtv,
            LiquidationLtv = snapshot?.LiquidationLtv ?? btcLoan.LiquidationLtv,
            MarginCallLtv = snapshot?.MarginCallLtv ?? btcLoan.MarginCallLtv,
            Fees = snapshot?.Fees ?? btcLoan.Fees,
            LoanStartDate = btcLoan.LoanStartDate,
            RepaymentDate = snapshot?.RepaymentDate ?? btcLoan.RepaymentDate,
            LoanStatusId = snapshot is not null ? (int)snapshot.Status : (int)btcLoan.Status,
            LoanStatusName = (snapshot?.Status ?? btcLoan.Status).ToString(),
            LoanHealthStatusId = loanHealthStatusId,
            LoanHealthStatusName = loanHealthStatusName,
            AccruedInterest = btcLoan.CalculateAccruedInterest(),
            TotalDebt = btcLoan.CalculateTotalDebt(),
            DistanceToLiquidationLtv = distanceToLiquidationLtv,
            DaysUntilRepayment = btcLoan.CalculateDaysUntilRepayment(),
            FixedTotalDebt = snapshot?.FixedTotalDebt ?? btcLoan.FixedTotalDebt,
            HasFixedTotalDebt = (snapshot?.FixedTotalDebt.HasValue).GetValueOrDefault(btcLoan.HasFixedTotalDebt)
        };
    }

    private static AssetDTO MapBtcLending(AssetEntity entity, Asset asset, BtcLendingDetails btcLending)
    {
        var dto = CreateBaseDto(entity, asset);

        return dto with
        {
            AmountLent = btcLending.AmountLent,
            BorrowerOrPlatformName = btcLending.BorrowerOrPlatformName,
            LendingStartDate = btcLending.LendingStartDate,
            Apr = btcLending.Apr,
            EarnedInterest = btcLending.CalculateEarnedInterest(),
            LoanStatusId = (int)btcLending.Status,
            LoanStatusName = btcLending.Status.ToString(),
            DaysUntilRepayment = btcLending.CalculateDaysUntilRepayment(),
            RepaymentDate = btcLending.ExpectedRepaymentDate
        };
    }

    public Task<IReadOnlyList<AssetGroupDTO>> GetAssetGroupsAsync()
    {
        var entities = _localDatabase.GetAssetGroups()
            .FindAll()
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var dtos = entities.Select(e => new AssetGroupDTO
        {
            Id = e.Id.ToString(),
            Name = e.Name,
            Description = e.Description,
            DisplayOrder = e.DisplayOrder
        }).ToList();

        return Task.FromResult<IReadOnlyList<AssetGroupDTO>>(dtos);
    }
}
