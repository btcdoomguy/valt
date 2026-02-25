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
            AssetTypes.BtcLoan => DeserializeBtcLoan(json),
            AssetTypes.BtcLending => DeserializeBtcLending(json),
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

    public static BtcLoanDetails DeserializeBtcLoan(string json)
    {
        var dto = Deserialize<BtcLoanDetailsDto>(json, nameof(BtcLoanDetails));
        return new BtcLoanDetails(
            dto.PlatformName ?? string.Empty,
            dto.CollateralSats,
            dto.LoanAmount,
            dto.CurrencyCode,
            dto.Apr,
            dto.InitialLtv,
            dto.LiquidationLtv,
            dto.MarginCallLtv,
            dto.Fees,
            string.IsNullOrEmpty(dto.LoanStartDate) ? DateOnly.FromDateTime(DateTime.UtcNow) : DateOnly.Parse(dto.LoanStartDate),
            string.IsNullOrEmpty(dto.RepaymentDate) ? null : DateOnly.Parse(dto.RepaymentDate),
            (LoanStatus)dto.StatusId,
            dto.CurrentBtcPrice);
    }

    public static BtcLendingDetails DeserializeBtcLending(string json)
    {
        var dto = Deserialize<BtcLendingDetailsDto>(json, nameof(BtcLendingDetails));
        return new BtcLendingDetails(
            dto.AmountLent,
            dto.CurrencyCode,
            dto.Apr,
            string.IsNullOrEmpty(dto.ExpectedRepaymentDate) ? null : DateOnly.Parse(dto.ExpectedRepaymentDate),
            dto.BorrowerOrPlatformName ?? string.Empty,
            string.IsNullOrEmpty(dto.LendingStartDate) ? DateOnly.FromDateTime(DateTime.UtcNow) : DateOnly.Parse(dto.LendingStartDate),
            (LoanStatus)dto.StatusId);
    }

    public static string Serialize(IAssetDetails details)
    {
        return details switch
        {
            BasicAssetDetails basic => SerializeBasicAsset(basic),
            RealEstateAssetDetails realEstate => SerializeRealEstate(realEstate),
            LeveragedPositionDetails leveraged => SerializeLeveragedPosition(leveraged),
            BtcLoanDetails btcLoan => SerializeBtcLoan(btcLoan),
            BtcLendingDetails btcLending => SerializeBtcLending(btcLending),
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

    private static string SerializeBtcLoan(BtcLoanDetails details)
    {
        var dto = new BtcLoanDetailsDto
        {
            PlatformName = details.PlatformName,
            CollateralSats = details.CollateralSats,
            LoanAmount = details.LoanAmount,
            CurrencyCode = details.CurrencyCode,
            Apr = details.Apr,
            InitialLtv = details.InitialLtv,
            LiquidationLtv = details.LiquidationLtv,
            MarginCallLtv = details.MarginCallLtv,
            Fees = details.Fees,
            LoanStartDate = details.LoanStartDate.ToString("O"),
            RepaymentDate = details.RepaymentDate?.ToString("O"),
            StatusId = (int)details.Status,
            CurrentBtcPrice = details.CurrentBtcPriceInLoanCurrency
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeBtcLending(BtcLendingDetails details)
    {
        var dto = new BtcLendingDetailsDto
        {
            AmountLent = details.AmountLent,
            CurrencyCode = details.CurrencyCode,
            Apr = details.Apr,
            ExpectedRepaymentDate = details.ExpectedRepaymentDate?.ToString("O"),
            BorrowerOrPlatformName = details.BorrowerOrPlatformName,
            LendingStartDate = details.LendingStartDate.ToString("O"),
            StatusId = (int)details.Status
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

internal class BtcLoanDetailsDto
{
    public string? PlatformName { get; set; }
    public long CollateralSats { get; set; }
    public decimal LoanAmount { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public decimal Apr { get; set; }
    public decimal InitialLtv { get; set; }
    public decimal LiquidationLtv { get; set; }
    public decimal MarginCallLtv { get; set; }
    public decimal Fees { get; set; }
    public string? LoanStartDate { get; set; }
    public string? RepaymentDate { get; set; }
    public int StatusId { get; set; }
    public decimal CurrentBtcPrice { get; set; }
}

internal class BtcLendingDetailsDto
{
    public decimal AmountLent { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public decimal Apr { get; set; }
    public string? ExpectedRepaymentDate { get; set; }
    public string? BorrowerOrPlatformName { get; set; }
    public string? LendingStartDate { get; set; }
    public int StatusId { get; set; }
}
