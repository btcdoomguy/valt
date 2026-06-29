namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Asset details for leveraged positions (e.g., futures, perpetuals, margin trading).
/// </summary>
public sealed class LeveragedPositionDetails : IAssetDetails
{
    public AssetTypes AssetType => AssetTypes.LeveragedPosition;

    /// <summary>
    /// The type of asset used as collateral.
    /// </summary>
    public LeveragedPositionCollateralAssetType CollateralAssetType { get; }

    /// <summary>
    /// The collateral amount.
    /// For Fiat collateral: the fiat amount (initial margin).
    /// For BTC collateral: the BTC amount deposited.
    /// </summary>
    public decimal Collateral { get; }

    /// <summary>
    /// The entry price when the position was opened (BTC price in the chosen fiat currency).
    /// </summary>
    public decimal EntryPrice { get; }

    /// <summary>
    /// The leverage multiplier (e.g., 2x, 5x, 10x).
    /// For Fiat collateral this is provided by the user.
    /// For BTC collateral this is derived from notional exposure / collateral.
    /// </summary>
    public decimal Leverage { get; }

    /// <summary>
    /// The liquidation price where the position would be closed.
    /// </summary>
    public decimal LiquidationPrice { get; }

    /// <summary>
    /// The current price of the underlying asset.
    /// </summary>
    public decimal CurrentPrice { get; }

    /// <summary>
    /// The currency code for the values (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// The ticker symbol for the underlying asset.
    /// </summary>
    public string? Symbol { get; }

    /// <summary>
    /// The price source for automatic price updates.
    /// </summary>
    public AssetPriceSource PriceSource { get; }

    /// <summary>
    /// True if this is a long position, false if short.
    /// </summary>
    public bool IsLong { get; }

    /// <summary>
    /// The input mode used to define the position (Collateral or ExactPosition).
    /// Only meaningful when collateral is Fiat.
    /// </summary>
    public LeveragedPositionInputMode InputMode { get; }

    /// <summary>
    /// Number of contracts. Only used when collateral is BTC.
    /// </summary>
    public decimal ContractCount { get; }

    /// <summary>
    /// Contract size in USD. Only used when collateral is BTC.
    /// </summary>
    public decimal ContractSizeUsd { get; }

    /// <summary>
    /// The position size in units of the underlying asset.
    /// For Fiat collateral: Collateral * Leverage / EntryPrice.
    /// For BTC collateral: ContractCount * ContractSizeUsd / EntryPrice (notional BTC at entry).
    /// </summary>
    public decimal PositionSize => EntryPrice > 0
        ? (CollateralAssetType == LeveragedPositionCollateralAssetType.Btc
            ? ContractCount * ContractSizeUsd / EntryPrice
            : Collateral * Leverage / EntryPrice)
        : 0;

    public LeveragedPositionDetails(
        decimal collateral,
        decimal entryPrice,
        decimal leverage,
        decimal liquidationPrice,
        decimal currentPrice,
        string currencyCode,
        string? symbol = null,
        AssetPriceSource priceSource = AssetPriceSource.Manual,
        bool isLong = true,
        LeveragedPositionInputMode inputMode = LeveragedPositionInputMode.Collateral,
        LeveragedPositionCollateralAssetType collateralAssetType = LeveragedPositionCollateralAssetType.Fiat,
        decimal contractCount = 0,
        decimal contractSizeUsd = 0)
    {
        if (collateral <= 0)
            throw new ArgumentException("Collateral must be positive", nameof(collateral));

        if (entryPrice <= 0)
            throw new ArgumentException("Entry price must be positive", nameof(entryPrice));

        if (liquidationPrice <= 0)
            throw new ArgumentException("Liquidation price must be positive", nameof(liquidationPrice));

        CollateralAssetType = collateralAssetType;
        Collateral = collateral;
        EntryPrice = entryPrice;
        LiquidationPrice = liquidationPrice;
        CurrentPrice = currentPrice;
        CurrencyCode = currencyCode;
        Symbol = symbol;
        PriceSource = priceSource;
        IsLong = isLong;
        InputMode = inputMode;
        ContractCount = contractCount;
        ContractSizeUsd = contractSizeUsd;

        if (collateralAssetType == LeveragedPositionCollateralAssetType.Fiat)
        {
            if (leverage < 1)
                throw new ArgumentException("Leverage must be at least 1 for fiat collateral", nameof(leverage));

            Leverage = leverage;
        }
        else
        {
            if (contractCount <= 0)
                throw new ArgumentException("Contract count must be positive for BTC collateral", nameof(contractCount));

            if (contractSizeUsd <= 0)
                throw new ArgumentException("Contract size must be positive for BTC collateral", nameof(contractSizeUsd));

            var notionalBtc = contractCount * contractSizeUsd / entryPrice;
            Leverage = notionalBtc / collateral;
        }
    }

    /// <summary>
    /// Calculates the current value of the leveraged position.
    /// For Fiat collateral: Collateral * (1 ± PriceChange * Leverage)
    /// For BTC collateral: (Collateral + PnL_Btc) * CurrentPrice
    /// </summary>
    public decimal CalculateCurrentValue(decimal currentPrice)
    {
        if (CollateralAssetType == LeveragedPositionCollateralAssetType.Btc)
            return CalculateBtcCurrentValue(currentPrice);

        if (EntryPrice == 0)
            return Collateral;

        var priceChange = (currentPrice - EntryPrice) / EntryPrice;
        var leveragedChange = priceChange * Leverage;

        return IsLong
            ? Collateral * (1 + leveragedChange)
            : Collateral * (1 - leveragedChange);
    }

    private decimal CalculateBtcCurrentValue(decimal currentPrice)
    {
        if (currentPrice == 0)
            return 0;

        var pnlBtc = CalculateBtcPnL(currentPrice);
        var totalBtc = Collateral + pnlBtc;
        return totalBtc * currentPrice;
    }

    private decimal CalculateBtcPnL(decimal currentPrice)
    {
        if (EntryPrice == 0 || currentPrice == 0)
            return 0;

        var notionalBtcEntry = ContractCount * ContractSizeUsd / EntryPrice;
        var notionalBtcCurrent = ContractCount * ContractSizeUsd / currentPrice;

        return IsLong
            ? notionalBtcEntry - notionalBtcCurrent
            : notionalBtcCurrent - notionalBtcEntry;
    }

    /// <summary>
    /// Calculates the unrealized P&L.
    /// For Fiat collateral: current value - collateral.
    /// For BTC collateral: USD-denominated position P&L = price change * notional USD.
    /// </summary>
    public decimal CalculatePnL(decimal currentPrice)
    {
        if (CollateralAssetType == LeveragedPositionCollateralAssetType.Btc)
            return CalculateBtcPositionPnLUsd(currentPrice);

        return CalculateCurrentValue(currentPrice) - Collateral;
    }

    private decimal CalculateBtcPositionPnLUsd(decimal currentPrice)
    {
        if (EntryPrice == 0 || currentPrice == 0)
            return 0;

        var notionalUsd = ContractCount * ContractSizeUsd;
        var priceChange = IsLong
            ? (currentPrice - EntryPrice) / EntryPrice
            : (EntryPrice - currentPrice) / EntryPrice;

        return notionalUsd * priceChange;
    }

    /// <summary>
    /// Calculates the P&L percentage.
    /// </summary>
    public decimal CalculatePnLPercentage(decimal currentPrice)
    {
        var costBasis = CollateralAssetType == LeveragedPositionCollateralAssetType.Btc
            ? Collateral * EntryPrice
            : Collateral;

        if (costBasis == 0)
            return 0;

        return Math.Round(CalculatePnL(currentPrice) / costBasis * 100, 2);
    }

    /// <summary>
    /// Calculates the distance to liquidation as a percentage.
    /// </summary>
    public decimal CalculateDistanceToLiquidation(decimal currentPrice)
    {
        if (currentPrice == 0 || LiquidationPrice == 0)
            return 100;

        var distance = IsLong
            ? (currentPrice - LiquidationPrice) / currentPrice * 100
            : (LiquidationPrice - currentPrice) / currentPrice * 100;

        return Math.Round(Math.Max(0, distance), 2);
    }

    /// <summary>
    /// Determines if the position is at risk (within 10% of liquidation).
    /// </summary>
    public bool IsAtRisk(decimal currentPrice)
    {
        return CalculateDistanceToLiquidation(currentPrice) < 10;
    }

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new LeveragedPositionDetails(
            Collateral,
            EntryPrice,
            Leverage,
            LiquidationPrice,
            newPrice,
            CurrencyCode,
            Symbol,
            PriceSource,
            IsLong,
            InputMode,
            CollateralAssetType,
            ContractCount,
            ContractSizeUsd);
    }

    public LeveragedPositionDetails WithCollateral(decimal newCollateral)
    {
        return new LeveragedPositionDetails(
            newCollateral,
            EntryPrice,
            Leverage,
            LiquidationPrice,
            CurrentPrice,
            CurrencyCode,
            Symbol,
            PriceSource,
            IsLong,
            InputMode,
            CollateralAssetType,
            ContractCount,
            ContractSizeUsd);
    }
}
