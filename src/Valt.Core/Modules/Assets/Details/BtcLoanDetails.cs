namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Asset details for BTC-collateralized loans (borrowing fiat against BTC collateral).
/// </summary>
public sealed class BtcLoanDetails : IAssetDetails
{
    public AssetTypes AssetType => AssetTypes.BtcLoan;

    /// <summary>
    /// The lending platform name (e.g., "HodlHodl", "Ledn").
    /// </summary>
    public string PlatformName { get; }

    /// <summary>
    /// BTC collateral amount in satoshis.
    /// </summary>
    public long CollateralSats { get; }

    /// <summary>
    /// The borrowed fiat amount.
    /// </summary>
    public decimal LoanAmount { get; }

    /// <summary>
    /// Currency code for the loan (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// Annual percentage rate (e.g., 0.12 for 12%).
    /// </summary>
    public decimal Apr { get; }

    /// <summary>
    /// The LTV ratio at loan origination.
    /// </summary>
    public decimal InitialLtv { get; }

    /// <summary>
    /// The LTV ratio that triggers liquidation.
    /// </summary>
    public decimal LiquidationLtv { get; }

    /// <summary>
    /// The LTV ratio that triggers a margin call warning.
    /// </summary>
    public decimal MarginCallLtv { get; }

    /// <summary>
    /// Fees paid for the loan.
    /// </summary>
    public decimal Fees { get; }

    /// <summary>
    /// When the loan started.
    /// </summary>
    public DateOnly LoanStartDate { get; }

    /// <summary>
    /// When the loan is due for repayment (null = indefinite/open-ended).
    /// </summary>
    public DateOnly? RepaymentDate { get; }

    /// <summary>
    /// Current loan status.
    /// </summary>
    public LoanStatus Status { get; }

    /// <summary>
    /// Current BTC price in the loan's currency, used for LTV calculations.
    /// </summary>
    public decimal CurrentBtcPriceInLoanCurrency { get; }

    public BtcLoanDetails(
        string platformName,
        long collateralSats,
        decimal loanAmount,
        string currencyCode,
        decimal apr,
        decimal initialLtv,
        decimal liquidationLtv,
        decimal marginCallLtv,
        decimal fees,
        DateOnly loanStartDate,
        DateOnly? repaymentDate,
        LoanStatus status,
        decimal currentBtcPriceInLoanCurrency)
    {
        if (collateralSats <= 0)
            throw new ArgumentException("Collateral must be positive", nameof(collateralSats));

        if (loanAmount <= 0)
            throw new ArgumentException("Loan amount must be positive", nameof(loanAmount));

        if (apr < 0)
            throw new ArgumentException("APR cannot be negative", nameof(apr));

        if (marginCallLtv <= 0)
            throw new ArgumentException("Margin call LTV must be positive", nameof(marginCallLtv));

        if (liquidationLtv <= marginCallLtv)
            throw new ArgumentException("Liquidation LTV must be greater than margin call LTV", nameof(liquidationLtv));

        PlatformName = platformName;
        CollateralSats = collateralSats;
        LoanAmount = loanAmount;
        CurrencyCode = currencyCode;
        Apr = apr;
        InitialLtv = initialLtv;
        LiquidationLtv = liquidationLtv;
        MarginCallLtv = marginCallLtv;
        Fees = fees;
        LoanStartDate = loanStartDate;
        RepaymentDate = repaymentDate;
        Status = status;
        CurrentBtcPriceInLoanCurrency = currentBtcPriceInLoanCurrency;
    }

    /// <summary>
    /// Calculates the current LTV ratio given a BTC price.
    /// LTV = LoanAmount / CollateralValue
    /// </summary>
    public decimal CalculateCurrentLtv(decimal btcPrice)
    {
        var collateralValue = CollateralSats / 100_000_000m * btcPrice;
        if (collateralValue == 0)
            return 0;

        return Math.Round(LoanAmount / collateralValue * 100, 2);
    }

    /// <summary>
    /// Determines the health status of the loan based on current LTV.
    /// </summary>
    public LoanHealthStatus CalculateHealthStatus(decimal btcPrice)
    {
        var currentLtv = CalculateCurrentLtv(btcPrice);
        if (currentLtv >= LiquidationLtv)
            return LoanHealthStatus.Danger;
        if (currentLtv >= MarginCallLtv)
            return LoanHealthStatus.Warning;
        return LoanHealthStatus.Healthy;
    }

    /// <summary>
    /// Calculates the distance (in percentage points) to the liquidation LTV.
    /// </summary>
    public decimal CalculateDistanceToLiquidation(decimal btcPrice)
    {
        var currentLtv = CalculateCurrentLtv(btcPrice);
        return Math.Round(Math.Max(0, LiquidationLtv - currentLtv), 2);
    }

    /// <summary>
    /// Calculates the accrued interest from loan start to today.
    /// </summary>
    public decimal CalculateAccruedInterest()
    {
        var daysSinceStart = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - LoanStartDate.DayNumber);
        if (daysSinceStart <= 0)
            return 0;

        return Math.Round(LoanAmount * Apr / 365 * daysSinceStart, 2);
    }

    /// <summary>
    /// Calculates the number of days until the repayment date.
    /// Returns null if no repayment date is set.
    /// </summary>
    public int? CalculateDaysUntilRepayment()
    {
        if (RepaymentDate is null)
            return null;

        var days = RepaymentDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        return Math.Max(0, days);
    }

    /// <summary>
    /// Calculates the net worth impact: CollateralValue - LoanAmount - AccruedInterest - Fees.
    /// Returns 0 if BTC price is unavailable (prevents bogus negative values).
    /// </summary>
    public decimal CalculateCurrentValue(decimal btcPrice)
    {
        if (btcPrice <= 0)
            return 0;

        var collateralValue = CollateralSats / 100_000_000m * btcPrice;
        return collateralValue - LoanAmount - CalculateAccruedInterest() - Fees;
    }

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new BtcLoanDetails(
            PlatformName,
            CollateralSats,
            LoanAmount,
            CurrencyCode,
            Apr,
            InitialLtv,
            LiquidationLtv,
            MarginCallLtv,
            Fees,
            LoanStartDate,
            RepaymentDate,
            Status,
            newPrice);
    }

    public BtcLoanDetails WithStatus(LoanStatus newStatus)
    {
        return new BtcLoanDetails(
            PlatformName,
            CollateralSats,
            LoanAmount,
            CurrencyCode,
            Apr,
            InitialLtv,
            LiquidationLtv,
            MarginCallLtv,
            Fees,
            LoanStartDate,
            RepaymentDate,
            newStatus,
            CurrentBtcPriceInLoanCurrency);
    }
}
