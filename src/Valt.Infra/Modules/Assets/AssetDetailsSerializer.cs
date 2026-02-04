using System.Text.Json;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Infra.Modules.Assets;

/// <summary>
/// Centralized serialization for asset details DTOs.
/// </summary>
internal static class AssetDetailsSerializer
{
    public static TDto Deserialize<TDto>(string json, string typeName) where TDto : class
    {
        return JsonSerializer.Deserialize<TDto>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeName}");
    }

    public static IAssetDetails DeserializeDetails(AssetTypes assetType, string json)
    {
        return assetType switch
        {
            AssetTypes.Stock or AssetTypes.Etf or AssetTypes.Crypto or AssetTypes.Commodity or AssetTypes.Custom
                => DeserializeBasicAsset(assetType, json),
            AssetTypes.RealEstate => DeserializeRealEstate(json),
            AssetTypes.LeveragedPosition => DeserializeLeveragedPosition(json),
            _ => throw new NotSupportedException($"Asset type {assetType} is not supported")
        };
    }

    public static BasicAssetDetails DeserializeBasicAsset(AssetTypes assetType, string json)
    {
        var dto = Deserialize<BasicAssetDetailsDto>(json, nameof(BasicAssetDetails));
        return new BasicAssetDetails(
            assetType,
            dto.Quantity,
            dto.Symbol,
            (AssetPriceSource)dto.PriceSourceId,
            dto.CurrentPrice,
            dto.CurrencyCode,
            string.IsNullOrEmpty(dto.AcquisitionDate) ? null : DateOnly.Parse(dto.AcquisitionDate),
            dto.AcquisitionPrice);
    }

    public static RealEstateAssetDetails DeserializeRealEstate(string json)
    {
        var dto = Deserialize<RealEstateAssetDetailsDto>(json, nameof(RealEstateAssetDetails));
        return new RealEstateAssetDetails(
            dto.CurrentValue,
            dto.CurrencyCode,
            dto.Address,
            dto.MonthlyRentalIncome,
            string.IsNullOrEmpty(dto.AcquisitionDate) ? null : DateOnly.Parse(dto.AcquisitionDate),
            dto.AcquisitionPrice);
    }

    public static LeveragedPositionDetails DeserializeLeveragedPosition(string json)
    {
        var dto = Deserialize<LeveragedPositionDetailsDto>(json, nameof(LeveragedPositionDetails));
        return new LeveragedPositionDetails(
            dto.Collateral,
            dto.EntryPrice,
            dto.Leverage,
            dto.LiquidationPrice,
            dto.CurrentPrice,
            dto.CurrencyCode,
            dto.Symbol,
            (AssetPriceSource)dto.PriceSourceId,
            dto.IsLong);
    }

    public static string Serialize(IAssetDetails details)
    {
        return details switch
        {
            BasicAssetDetails basic => SerializeBasicAsset(basic),
            RealEstateAssetDetails realEstate => SerializeRealEstate(realEstate),
            LeveragedPositionDetails leveraged => SerializeLeveragedPosition(leveraged),
            _ => throw new NotSupportedException($"Asset details type {details.GetType().Name} is not supported")
        };
    }

    private static string SerializeBasicAsset(BasicAssetDetails details)
    {
        var dto = new BasicAssetDetailsDto
        {
            Quantity = details.Quantity,
            Symbol = details.Symbol,
            PriceSourceId = (int)details.PriceSource,
            CurrentPrice = details.CurrentPrice,
            CurrencyCode = details.CurrencyCode,
            AcquisitionDate = details.AcquisitionDate?.ToString("O"),
            AcquisitionPrice = details.AcquisitionPrice
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeRealEstate(RealEstateAssetDetails details)
    {
        var dto = new RealEstateAssetDetailsDto
        {
            CurrentValue = details.CurrentValue,
            CurrencyCode = details.CurrencyCode,
            Address = details.Address,
            MonthlyRentalIncome = details.MonthlyRentalIncome,
            AcquisitionDate = details.AcquisitionDate?.ToString("O"),
            AcquisitionPrice = details.AcquisitionPrice
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeLeveragedPosition(LeveragedPositionDetails details)
    {
        var dto = new LeveragedPositionDetailsDto
        {
            Collateral = details.Collateral,
            EntryPrice = details.EntryPrice,
            Leverage = details.Leverage,
            LiquidationPrice = details.LiquidationPrice,
            CurrentPrice = details.CurrentPrice,
            CurrencyCode = details.CurrencyCode,
            Symbol = details.Symbol,
            PriceSourceId = (int)details.PriceSource,
            IsLong = details.IsLong
        };
        return JsonSerializer.Serialize(dto);
    }
}

// DTOs for JSON serialization
internal class BasicAssetDetailsDto
{
    public decimal Quantity { get; set; }
    public string? Symbol { get; set; }
    public int PriceSourceId { get; set; }
    public decimal CurrentPrice { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
}

internal class RealEstateAssetDetailsDto
{
    public decimal CurrentValue { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string? Address { get; set; }
    public decimal? MonthlyRentalIncome { get; set; }
    public string? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
}

internal class LeveragedPositionDetailsDto
{
    public decimal Collateral { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal Leverage { get; set; }
    public decimal LiquidationPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string? Symbol { get; set; }
    public int PriceSourceId { get; set; }
    public bool IsLong { get; set; }
}
