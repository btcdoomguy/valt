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

    // Selection state for card visuals (hover handled via CSS :pointerover)
    [ObservableProperty]
    private bool _isSelected;

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
    public decimal? DistanceToLiquidation { get; }

    /// <summary>
    /// Indicates if the leveraged position is at risk of liquidation.
    /// Only true when within 10% of liquidation AND PnL is negative or small.
    /// A position with significant profit (> 50% PnL) cannot be close to liquidation.
    /// </summary>
    public bool IsAtRisk { get; }

    // Common acquisition and P&L fields
    public DateOnly? AcquisitionDate { get; }
    public decimal? AcquisitionPrice { get; }
    public decimal? PnL { get; }
    public decimal? PnLPercentage { get; }
    public string AcquisitionPriceFormatted { get; }
    public bool HasAcquisitionData { get; }

    public string PnLFormatted { get; }
    public string PnLPercentageFormatted { get; }
    public string PnLCombinedFormatted { get; }
    public string DistanceToLiquidationFormatted { get; }
    public string LeverageFormatted { get; }
    public string EntryPriceFormatted { get; }
    public string LiquidationPriceFormatted { get; }
    public string CollateralFormatted { get; }

    // BTC loan specific
    public string? PlatformName { get; }
    public long? CollateralSats { get; }
    public decimal? LoanAmount { get; }
    public decimal? Apr { get; }
    public decimal? CurrentLtv { get; }
    public decimal? InitialLtv { get; }
    public decimal? LiquidationLtv { get; }
    public decimal? MarginCallLtv { get; }
    public decimal? Fees { get; }
    public DateOnly? LoanStartDate { get; }
    public DateOnly? RepaymentDate { get; }
    public int? LoanStatusId { get; }
    public string? LoanStatusName { get; }
    public int? LoanHealthStatusId { get; }
    public string? LoanHealthStatusName { get; }
    public decimal? AccruedInterest { get; }
    public decimal? DistanceToLiquidationLtv { get; }
    public int? DaysUntilRepayment { get; }

    // BTC lending specific
    public decimal? AmountLent { get; }
    public string? BorrowerOrPlatformName { get; }
    public DateOnly? LendingStartDate { get; }
    public decimal? EarnedInterest { get; }

    // Formatted loan/lending fields
    public string CollateralSatsFormatted { get; }
    public string LoanAmountFormatted { get; }
    public string AprFormatted { get; }
    public string CurrentLtvFormatted { get; }
    public string AccruedInterestFormatted { get; }
    public string DistanceToLiquidationLtvFormatted { get; }
    public string DaysUntilRepaymentFormatted { get; }
    public string AmountLentFormatted { get; }
    public string EarnedInterestFormatted { get; }
    public string FeesFormatted { get; }
    public string LoanHealthColor { get; }

    // Display helpers
    public bool IsBasicAsset => AssetType is AssetTypes.Stock or AssetTypes.Etf or AssetTypes.Crypto
        or AssetTypes.Commodity or AssetTypes.Custom;
    public bool IsRealEstate => AssetType == AssetTypes.RealEstate;
    public bool IsLeveragedPosition => AssetType == AssetTypes.LeveragedPosition;
    public bool IsBtcLoan => AssetType == AssetTypes.BtcLoan;
    public bool IsBtcLending => AssetType == AssetTypes.BtcLending;
    public bool IsLoanOrLending => IsBtcLoan || IsBtcLending;

    // Returns P&L for leveraged positions, Value for others
    public string DisplayValueFormatted => IsLeveragedPosition ? PnLFormatted : CurrentValueFormatted;
    public string PositionDirection => IsLong == true ? "Long" : "Short";
    public string PnLColor => PnL >= 0 ? "#4CAF50" : "#F44336";
    public string AtRiskIndicator => IsAtRisk ? "!" : "";
    public string ValueColor => (IsLeveragedPosition ? PnL ?? 0 : CurrentValue) >= 0 ? "#4CAF50" : "#F44336";
    public bool IsNegativeValue => CurrentValue < 0;

    // Card display properties (darker tones for white text readability)
    public string TitleBarColor => AssetType switch
    {
        AssetTypes.Stock => "#2D5442",        // Dark forest green
        AssetTypes.Etf => "#157A73",          // Dark teal
        AssetTypes.Crypto => "#B8860B",       // Dark goldenrod
        AssetTypes.RealEstate => "#8B0000",   // Dark red
        AssetTypes.Commodity => "#7A6B4E",    // Dark bronze
        AssetTypes.LeveragedPosition => "#6B238E", // Dark purple
        AssetTypes.Custom => "#3A3A3A",       // Dark gray
        AssetTypes.BtcLoan => "#1A237E",      // Dark indigo
        AssetTypes.BtcLending => "#004D40",   // Dark teal
        _ => "#3A3A3A"
    };
    public string TitleBarTextColor => "#FFFFFF";
    public string NetWorthIcon => IncludeInNetWorth ? "\xE86C" : "\xE5C9";
    public string NetWorthIconColor => IncludeInNetWorth ? "#4CAF50" : "#757575";
    public string VisibilityIcon => "\xE8F5";

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
        DistanceToLiquidation = dto.DistanceToLiquidation;
        // Only show at risk if reported AND PnL is not significantly positive
        // A position with > 50% profit cannot logically be close to liquidation
        IsAtRisk = dto.IsAtRisk == true && (dto.PnLPercentage == null || dto.PnLPercentage <= 50);

        // BTC loan
        PlatformName = dto.PlatformName;
        CollateralSats = dto.CollateralSats;
        LoanAmount = dto.LoanAmount;
        Apr = dto.Apr;
        CurrentLtv = dto.CurrentLtv;
        InitialLtv = dto.InitialLtv;
        LiquidationLtv = dto.LiquidationLtv;
        MarginCallLtv = dto.MarginCallLtv;
        Fees = dto.Fees;
        LoanStartDate = dto.LoanStartDate;
        RepaymentDate = dto.RepaymentDate;
        LoanStatusId = dto.LoanStatusId;
        LoanStatusName = GetLocalizedLoanStatusName(dto.LoanStatusId);
        LoanHealthStatusId = dto.LoanHealthStatusId;
        LoanHealthStatusName = GetLocalizedHealthStatusName(dto.LoanHealthStatusId);
        AccruedInterest = dto.AccruedInterest;
        DistanceToLiquidationLtv = dto.DistanceToLiquidationLtv;
        DaysUntilRepayment = dto.DaysUntilRepayment;

        // BTC lending
        AmountLent = dto.AmountLent;
        BorrowerOrPlatformName = dto.BorrowerOrPlatformName;
        LendingStartDate = dto.LendingStartDate;
        EarnedInterest = dto.EarnedInterest;

        // Common acquisition and P&L fields
        AcquisitionDate = dto.AcquisitionDate;
        AcquisitionPrice = dto.AcquisitionPrice;
        PnL = dto.PnL;
        PnLPercentage = dto.PnLPercentage;
        HasAcquisitionData = dto.AcquisitionPrice.HasValue;

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

        AcquisitionPriceFormatted = AcquisitionPrice.HasValue
            ? CurrencyDisplay.FormatFiat(AcquisitionPrice.Value, CurrencyCode)
            : "-";

        // BTC loan/lending formatted fields
        CollateralSatsFormatted = CollateralSats.HasValue
            ? $"{CollateralSats.Value:N0} sats"
            : "-";

        LoanAmountFormatted = LoanAmount.HasValue
            ? CurrencyDisplay.FormatFiat(LoanAmount.Value, CurrencyCode)
            : "-";

        AprFormatted = Apr.HasValue
            ? $"{Apr.Value * 100:N2}%"
            : "-";

        CurrentLtvFormatted = CurrentLtv.HasValue
            ? $"{CurrentLtv.Value:N2}%"
            : "-";

        AccruedInterestFormatted = AccruedInterest.HasValue
            ? CurrencyDisplay.FormatFiat(AccruedInterest.Value, CurrencyCode)
            : "-";

        DistanceToLiquidationLtvFormatted = DistanceToLiquidationLtv.HasValue
            ? $"{DistanceToLiquidationLtv.Value:N2}pp"
            : "-";

        DaysUntilRepaymentFormatted = DaysUntilRepayment.HasValue
            ? $"{DaysUntilRepayment.Value}d"
            : "-";

        AmountLentFormatted = AmountLent.HasValue
            ? CurrencyDisplay.FormatFiat(AmountLent.Value, CurrencyCode)
            : "-";

        EarnedInterestFormatted = EarnedInterest.HasValue
            ? CurrencyDisplay.FormatFiat(EarnedInterest.Value, CurrencyCode)
            : "-";

        FeesFormatted = Fees.HasValue
            ? CurrencyDisplay.FormatFiat(Fees.Value, CurrencyCode)
            : "-";

        LoanHealthColor = LoanHealthStatusId switch
        {
            0 => "#4CAF50", // Healthy - green
            1 => "#FFC107", // Warning - amber
            2 => "#F44336", // Danger - red
            _ => "#757575"  // Unknown - gray
        };
    }

    private static string? GetLocalizedLoanStatusName(int? statusId) => statusId switch
    {
        0 => language.Assets_Status_Active,
        1 => language.Assets_Status_Repaid,
        _ => null
    };

    private static string? GetLocalizedHealthStatusName(int? healthStatusId) => healthStatusId switch
    {
        0 => language.Assets_Health_Healthy,
        1 => language.Assets_Health_Warning,
        2 => language.Assets_Health_Danger,
        _ => null
    };

    private static string GetLocalizedAssetTypeName(AssetTypes assetType) => assetType switch
    {
        AssetTypes.Stock => language.Assets_Type_Stock,
        AssetTypes.Etf => language.Assets_Type_Etf,
        AssetTypes.Crypto => language.Assets_Type_Crypto,
        AssetTypes.Commodity => language.Assets_Type_Commodity,
        AssetTypes.RealEstate => language.Assets_Type_RealEstate,
        AssetTypes.LeveragedPosition => language.Assets_Type_LeveragedPosition,
        AssetTypes.Custom => language.Assets_Type_Custom,
        AssetTypes.BtcLoan => language.Assets_Type_BtcLoan,
        AssetTypes.BtcLending => language.Assets_Type_BtcLending,
        _ => assetType.ToString()
    };
}
