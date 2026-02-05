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

    public Task<AssetSummaryDTO> GetSummaryAsync(string mainCurrencyCode, decimal? btcPriceUsd = null, IReadOnlyDictionary<string, decimal>? fiatRates = null)
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

        var totalValueInMainCurrency = 0m;
        long totalValueInSats = 0;

        if (btcPriceUsd.HasValue && fiatRates != null)
        {
            foreach (var asset in includedAssets)
            {
                var value = GetValueForSummary(asset);
                var currency = asset.GetCurrencyCode();

                // Convert to USD first
                var valueInUsd = currency == FiatCurrency.Usd.Code
                    ? value
                    : fiatRates.TryGetValue(currency, out var rate) && rate > 0
                        ? value / rate
                        : 0m;

                // Convert to main currency
                if (currency == mainCurrencyCode)
                {
                    totalValueInMainCurrency += value;
                }
                else if (fiatRates.TryGetValue(mainCurrencyCode, out var mainRate))
                {
                    totalValueInMainCurrency += valueInUsd * mainRate;
                }

                // Convert to sats
                if (btcPriceUsd.Value > 0)
                {
                    var btcAmount = valueInUsd / btcPriceUsd.Value;
                    totalValueInSats += (long)(btcAmount * 100_000_000);
                }
            }
        }

        var summary = new AssetSummaryDTO
        {
            TotalAssets = entities.Count,
            VisibleAssets = entities.Count(x => x.Visible),
            AssetsIncludedInNetWorth = includedAssets.Count,
            ValuesByCurrency = valuesByCurrency,
            TotalValueInMainCurrency = Math.Round(totalValueInMainCurrency, 2),
            TotalValueInSats = totalValueInSats
        };

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Gets the value to use for portfolio summary calculations.
    /// For leveraged positions, returns only the P&amp;L (not the full position value).
    /// For other assets, returns the current value.
    /// </summary>
    private static decimal GetValueForSummary(Asset asset)
    {
        if (asset.Details is LeveragedPositionDetails leveraged)
        {
            // For leveraged positions, use P&L only (not full position value)
            return leveraged.CalculatePnL(leveraged.CurrentPrice);
        }
        return asset.GetCurrentValue();
    }

    private static AssetDTO MapToDto(AssetEntity entity)
    {
        var asset = entity.AsDomainObject();

        // Extract type-specific fields
        decimal? quantity = null;
        string? symbol = null;
        int? priceSourceId = null;
        string? address = null;
        decimal? monthlyRentalIncome = null;
        decimal? collateral = null;
        decimal? entryPrice = null;
        decimal? leverage = null;
        decimal? liquidationPrice = null;
        bool? isLong = null;
        decimal? pnl = null;
        decimal? pnlPercentage = null;
        decimal? distanceToLiquidation = null;
        bool? isAtRisk = null;
        DateOnly? acquisitionDate = null;
        decimal? acquisitionPrice = null;

        switch (asset.Details)
        {
            case BasicAssetDetails basic:
                quantity = basic.Quantity;
                symbol = basic.Symbol;
                priceSourceId = (int)basic.PriceSource;
                acquisitionDate = basic.AcquisitionDate;
                acquisitionPrice = basic.AcquisitionPrice;
                if (basic.AcquisitionPrice.HasValue)
                {
                    pnl = basic.CalculatePnL();
                    pnlPercentage = basic.CalculatePnLPercentage();
                }
                break;

            case RealEstateAssetDetails realEstate:
                address = realEstate.Address;
                monthlyRentalIncome = realEstate.MonthlyRentalIncome;
                acquisitionDate = realEstate.AcquisitionDate;
                acquisitionPrice = realEstate.AcquisitionPrice;
                if (realEstate.AcquisitionPrice.HasValue)
                {
                    pnl = realEstate.CalculatePnL();
                    pnlPercentage = realEstate.CalculatePnLPercentage();
                }
                break;

            case LeveragedPositionDetails leveraged:
                collateral = leveraged.Collateral;
                entryPrice = leveraged.EntryPrice;
                leverage = leveraged.Leverage;
                liquidationPrice = leveraged.LiquidationPrice;
                isLong = leveraged.IsLong;
                symbol = leveraged.Symbol;
                priceSourceId = (int)leveraged.PriceSource;
                pnl = leveraged.CalculatePnL(leveraged.CurrentPrice);
                pnlPercentage = leveraged.CalculatePnLPercentage(leveraged.CurrentPrice);
                distanceToLiquidation = leveraged.CalculateDistanceToLiquidation(leveraged.CurrentPrice);
                isAtRisk = leveraged.IsAtRisk(leveraged.CurrentPrice);
                break;
        }

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
            CurrentPrice = asset.GetCurrentPrice(),
            CurrentValue = asset.GetCurrentValue(),
            CurrencyCode = asset.GetCurrencyCode(),
            // Type-specific fields
            Quantity = quantity,
            Symbol = symbol,
            PriceSourceId = priceSourceId,
            Address = address,
            MonthlyRentalIncome = monthlyRentalIncome,
            Collateral = collateral,
            EntryPrice = entryPrice,
            Leverage = leverage,
            LiquidationPrice = liquidationPrice,
            IsLong = isLong,
            DistanceToLiquidation = distanceToLiquidation,
            IsAtRisk = isAtRisk,
            // Common acquisition and P&L fields
            AcquisitionDate = acquisitionDate,
            AcquisitionPrice = acquisitionPrice,
            PnL = pnl,
            PnLPercentage = pnlPercentage
        };
    }
}
