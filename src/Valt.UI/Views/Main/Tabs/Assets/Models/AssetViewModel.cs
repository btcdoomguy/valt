using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Assets.Models;

public partial class AssetViewModel : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string AssetTypeName { get; }
    public AssetTypes AssetType { get; }
    public string Icon { get; }
    public bool IncludeInNetWorth { get; }
    public bool Visible { get; }
    public DateTime LastPriceUpdateAt { get; }
    public string CurrencyCode { get; }

    // Selection and hover state for card visuals
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    private bool _isHovered;

    // Price and value
    public decimal CurrentPrice { get; }
    public decimal CurrentValue { get; }
    public string CurrentValueFormatted { get; }
    public string CurrentPriceFormatted { get; }

    // Basic asset specific
    public decimal? Quantity { get; }
    public string? Symbol { get; }
    public AssetPriceSource? PriceSource { get; }
    public string QuantityFormatted { get; }

    // Real estate specific
    public string? Address { get; }
    public decimal? MonthlyRentalIncome { get; }
    public string MonthlyRentalIncomeFormatted { get; }

    // Leveraged position specific
    public decimal? Collateral { get; }
    public decimal? EntryPrice { get; }
    public decimal? Leverage { get; }
    public decimal? LiquidationPrice { get; }
    public bool? IsLong { get; }
    public decimal? PnL { get; }
    public decimal? PnLPercentage { get; }
    public decimal? DistanceToLiquidation { get; }
    public bool? IsAtRisk { get; }

    public string PnLFormatted { get; }
    public string PnLPercentageFormatted { get; }
    public string PnLCombinedFormatted { get; }
    public string DistanceToLiquidationFormatted { get; }
    public string LeverageFormatted { get; }
    public string EntryPriceFormatted { get; }
    public string LiquidationPriceFormatted { get; }
    public string CollateralFormatted { get; }

    // Display helpers
    public bool IsBasicAsset => AssetType is AssetTypes.Stock or AssetTypes.Etf or AssetTypes.Crypto
        or AssetTypes.Commodity or AssetTypes.Custom;
    public bool IsRealEstate => AssetType == AssetTypes.RealEstate;
    public bool IsLeveragedPosition => AssetType == AssetTypes.LeveragedPosition;

    // Returns P&L for leveraged positions, Value for others
    public string DisplayValueFormatted => IsLeveragedPosition ? PnLCombinedFormatted : CurrentValueFormatted;
    public string PositionDirection => IsLong == true ? "Long" : "Short";
    public string PnLColor => PnL >= 0 ? "#4CAF50" : "#F44336";
    public string AtRiskIndicator => IsAtRisk == true ? "!" : "";
    public string ValueColor => CurrentValue >= 0 ? "#1A1A1A" : "#F44336";
    public bool IsNegativeValue => CurrentValue < 0;

    // Card display properties
    public string TitleBarColor => AssetType switch
    {
        AssetTypes.Stock => "#35654D",
        AssetTypes.Etf => "#1FB2A6",
        AssetTypes.Crypto => "#F5A623",
        AssetTypes.RealEstate => "#8B0000",
        AssetTypes.Commodity => "#C4A35A",
        AssetTypes.LeveragedPosition => "#9932CC",
        AssetTypes.Custom => "#4A4A4A",
        _ => "#4A4A4A"
    };
    public string TitleBarTextColor => "#FFFFFF";
    public string NetWorthIcon => IncludeInNetWorth ? "\xE86C" : "\xE5C9";
    public string NetWorthIconColor => IncludeInNetWorth ? "#4CAF50" : "#757575";
    public string VisibilityIcon => "\xE8F5";

    // Card border color based on selection/hover state
    public string CardBorderBrush => IsSelected ? "#FF6200" : IsHovered ? "#666666" : "#1A1A1A";

    public AssetViewModel(AssetDTO dto, string mainCurrencyCode)
    {
        Id = dto.Id;
        Name = dto.Name;
        AssetType = (AssetTypes)dto.AssetTypeId;
        AssetTypeName = GetLocalizedAssetTypeName(AssetType);
        Icon = dto.Icon;
        IncludeInNetWorth = dto.IncludeInNetWorth;
        Visible = dto.Visible;
        LastPriceUpdateAt = dto.LastPriceUpdateAt;
        CurrencyCode = dto.CurrencyCode;
        CurrentPrice = dto.CurrentPrice;
        CurrentValue = dto.CurrentValue;

        // Basic asset
        Quantity = dto.Quantity;
        Symbol = dto.Symbol;
        PriceSource = dto.PriceSourceId.HasValue ? (AssetPriceSource)dto.PriceSourceId.Value : null;

        // Real estate
        Address = dto.Address;
        MonthlyRentalIncome = dto.MonthlyRentalIncome;

        // Leveraged position
        Collateral = dto.Collateral;
        EntryPrice = dto.EntryPrice;
        Leverage = dto.Leverage;
        LiquidationPrice = dto.LiquidationPrice;
        IsLong = dto.IsLong;
        PnL = dto.PnL;
        PnLPercentage = dto.PnLPercentage;
        DistanceToLiquidation = dto.DistanceToLiquidation;
        IsAtRisk = dto.IsAtRisk;

        // Format values
        CurrentValueFormatted = CurrencyDisplay.FormatFiat(CurrentValue, CurrencyCode);
        CurrentPriceFormatted = CurrencyDisplay.FormatFiat(CurrentPrice, CurrencyCode);
        QuantityFormatted = Quantity?.ToString("N4", CultureInfo.CurrentCulture) ?? "-";

        MonthlyRentalIncomeFormatted = MonthlyRentalIncome.HasValue
            ? CurrencyDisplay.FormatFiat(MonthlyRentalIncome.Value, CurrencyCode)
            : "-";

        PnLFormatted = PnL.HasValue
            ? CurrencyDisplay.FormatFiat(PnL.Value, CurrencyCode)
            : "-";

        PnLPercentageFormatted = PnLPercentage.HasValue
            ? $"{(PnLPercentage.Value >= 0 ? "+" : "")}{PnLPercentage.Value}%"
            : "-";

        PnLCombinedFormatted = PnL.HasValue && PnLPercentage.HasValue
            ? $"{CurrencyDisplay.FormatFiat(PnL.Value, CurrencyCode)} ({(PnLPercentage.Value >= 0 ? "+" : "")}{PnLPercentage.Value}%)"
            : "-";

        DistanceToLiquidationFormatted = DistanceToLiquidation.HasValue
            ? $"{DistanceToLiquidation.Value}%"
            : "-";

        LeverageFormatted = Leverage.HasValue
            ? $"{Leverage.Value}x"
            : "-";

        EntryPriceFormatted = EntryPrice.HasValue
            ? CurrencyDisplay.FormatFiat(EntryPrice.Value, CurrencyCode)
            : "-";

        LiquidationPriceFormatted = LiquidationPrice.HasValue
            ? CurrencyDisplay.FormatFiat(LiquidationPrice.Value, CurrencyCode)
            : "-";

        CollateralFormatted = Collateral.HasValue
            ? CurrencyDisplay.FormatFiat(Collateral.Value, CurrencyCode)
            : "-";
    }

    private static string GetLocalizedAssetTypeName(AssetTypes assetType) => assetType switch
    {
        AssetTypes.Stock => language.Assets_Type_Stock,
        AssetTypes.Etf => language.Assets_Type_Etf,
        AssetTypes.Crypto => language.Assets_Type_Crypto,
        AssetTypes.Commodity => language.Assets_Type_Commodity,
        AssetTypes.RealEstate => language.Assets_Type_RealEstate,
        AssetTypes.LeveragedPosition => language.Assets_Type_LeveragedPosition,
        AssetTypes.Custom => language.Assets_Type_Custom,
        _ => assetType.ToString()
    };
}
