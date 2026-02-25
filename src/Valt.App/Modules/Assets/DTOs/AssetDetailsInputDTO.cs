namespace Valt.App.Modules.Assets.DTOs;

/// <summary>
/// Base class for asset details input DTOs.
/// </summary>
public abstract record AssetDetailsInputDTO
{
    /// <summary>
    /// Currency code for the asset (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }
}

/// <summary>
/// Input DTO for basic assets (Stock, ETF, Crypto, Commodity, Custom).
/// </summary>
public record BasicAssetDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Asset type: 0=Stock, 1=Etf, 2=Crypto, 3=Commodity, 6=Custom
    /// </summary>
    public required int AssetType { get; init; }

    /// <summary>
    /// Quantity held.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Ticker symbol (e.g., AAPL, BTC).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Price source: 0=Manual, 1=YahooFinance
    /// </summary>
    public required int PriceSource { get; init; }

    /// <summary>
    /// Current price per unit.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Acquisition date (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; init; }

    /// <summary>
    /// Price per unit at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; init; }
}

/// <summary>
/// Input DTO for real estate assets.
/// </summary>
public record RealEstateAssetDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Property address (optional).
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Current market value.
    /// </summary>
    public required decimal CurrentValue { get; init; }

    /// <summary>
    /// Monthly rental income (optional).
    /// </summary>
    public decimal? MonthlyRentalIncome { get; init; }

    /// <summary>
    /// Acquisition date (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; init; }

    /// <summary>
    /// Total purchase price at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; init; }
}

/// <summary>
/// Input DTO for leveraged position assets.
/// </summary>
public record LeveragedPositionDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Ticker symbol (e.g., BTC-PERP).
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Collateral amount.
    /// </summary>
    public required decimal Collateral { get; init; }

    /// <summary>
    /// Entry price.
    /// </summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>
    /// Current price.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Leverage multiplier (e.g., 2, 5, 10).
    /// </summary>
    public required decimal Leverage { get; init; }

    /// <summary>
    /// Liquidation price.
    /// </summary>
    public required decimal LiquidationPrice { get; init; }

    /// <summary>
    /// True for long position, false for short.
    /// </summary>
    public required bool IsLong { get; init; }

    /// <summary>
    /// Price source: 0=Manual, 1=YahooFinance
    /// </summary>
    public required int PriceSource { get; init; }
}

/// <summary>
/// Input DTO for BTC loan assets.
/// </summary>
public record BtcLoanDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Platform name (e.g., "HodlHodl", "Ledn").
    /// </summary>
    public required string PlatformName { get; init; }

    /// <summary>
    /// BTC collateral in satoshis.
    /// </summary>
    public required long CollateralSats { get; init; }

    /// <summary>
    /// Borrowed fiat amount.
    /// </summary>
    public required decimal LoanAmount { get; init; }

    /// <summary>
    /// Annual percentage rate (e.g., 0.12 for 12%).
    /// </summary>
    public required decimal Apr { get; init; }

    /// <summary>
    /// Initial LTV ratio (percentage).
    /// </summary>
    public required decimal InitialLtv { get; init; }

    /// <summary>
    /// Liquidation LTV ratio (percentage).
    /// </summary>
    public required decimal LiquidationLtv { get; init; }

    /// <summary>
    /// Margin call LTV ratio (percentage).
    /// </summary>
    public required decimal MarginCallLtv { get; init; }

    /// <summary>
    /// Fees paid.
    /// </summary>
    public decimal Fees { get; init; }

    /// <summary>
    /// Loan start date.
    /// </summary>
    public required DateOnly LoanStartDate { get; init; }

    /// <summary>
    /// Repayment date (null = indefinite).
    /// </summary>
    public DateOnly? RepaymentDate { get; init; }

    /// <summary>
    /// Current BTC price in loan currency.
    /// </summary>
    public decimal CurrentBtcPrice { get; init; }

    /// <summary>
    /// Loan status: 0=Active, 1=Repaid
    /// </summary>
    public int Status { get; init; }
}

/// <summary>
/// Input DTO for BTC lending assets.
/// </summary>
public record BtcLendingDetailsInputDTO : AssetDetailsInputDTO
{
    /// <summary>
    /// Amount lent.
    /// </summary>
    public required decimal AmountLent { get; init; }

    /// <summary>
    /// Annual percentage rate earned (e.g., 0.05 for 5%).
    /// </summary>
    public required decimal Apr { get; init; }

    /// <summary>
    /// Borrower or platform name.
    /// </summary>
    public required string BorrowerOrPlatformName { get; init; }

    /// <summary>
    /// Lending start date.
    /// </summary>
    public required DateOnly LendingStartDate { get; init; }

    /// <summary>
    /// Expected repayment date (null = indefinite).
    /// </summary>
    public DateOnly? ExpectedRepaymentDate { get; init; }

    /// <summary>
    /// Lending status: 0=Active, 1=Repaid
    /// </summary>
    public int Status { get; init; }
}
