using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Assets.Queries.DTOs;

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
                TotalValue = g.Sum(a => a.GetCurrentValue()),
                AssetCount = g.Count()
            })
            .ToList();

        var totalValueInMainCurrency = 0m;
        long totalValueInSats = 0;

        if (btcPriceUsd.HasValue && fiatRates != null)
        {
            foreach (var asset in includedAssets)
            {
                var value = asset.GetCurrentValue();
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

    private static AssetDTO MapToDto(AssetEntity entity)
    {
        var asset = entity.AsDomainObject();
        var dto = new AssetDTO
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
            CurrencyCode = asset.GetCurrencyCode()
        };

        // Fill in type-specific details
        switch (asset.Details)
        {
            case BasicAssetDetails basic:
                dto.Quantity = basic.Quantity;
                dto.Symbol = basic.Symbol;
                dto.PriceSourceId = (int)basic.PriceSource;
                break;

            case RealEstateAssetDetails realEstate:
                dto.Address = realEstate.Address;
                dto.MonthlyRentalIncome = realEstate.MonthlyRentalIncome;
                break;

            case LeveragedPositionDetails leveraged:
                dto.Collateral = leveraged.Collateral;
                dto.EntryPrice = leveraged.EntryPrice;
                dto.Leverage = leveraged.Leverage;
                dto.LiquidationPrice = leveraged.LiquidationPrice;
                dto.IsLong = leveraged.IsLong;
                dto.Symbol = leveraged.Symbol;
                dto.PriceSourceId = (int)leveraged.PriceSource;
                dto.PnL = leveraged.CalculatePnL(leveraged.CurrentPrice);
                dto.PnLPercentage = leveraged.CalculatePnLPercentage(leveraged.CurrentPrice);
                dto.DistanceToLiquidation = leveraged.CalculateDistanceToLiquidation(leveraged.CurrentPrice);
                dto.IsAtRisk = leveraged.IsAtRisk(leveraged.CurrentPrice);
                break;
        }

        return dto;
    }
}
