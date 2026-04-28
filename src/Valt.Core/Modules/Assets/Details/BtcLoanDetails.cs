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
    /// When <see cref="FixedTotalDebt"/> is set, this value is derived from the fixed debt
    /// and the loan period so existing dashboards/reports keep working uniformly.
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

    /// <summary>
    /// Optional predefined total debt (e.g., HodlHodl-style loans where the repayment amount
    /// is fixed up-front and does not accrue daily). When set, <see cref="CalculateTotalDebt"/>
    /// always returns this value regardless of how much time has passed.
    /// </summary>
    public decimal? FixedTotalDebt { get; }

    /// <summary>
    /// Whether this loan uses a fixed total debt rather than daily APR accrual.
    /// </summary>
    public bool HasFixedTotalDebt => FixedTotalDebt.HasValue;

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
        decimal currentBtcPriceInLoanCurrency,
        decimal? fixedTotalDebt = null)
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

        if (fixedTotalDebt.HasValue && fixedTotalDebt.Value < loanAmount + fees)
            throw new ArgumentException(
                "Fixed total debt cannot be lower than the principal plus fees",
                nameof(fixedTotalDebt));

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
        FixedTotalDebt = fixedTotalDebt;
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
    /// Calculates the accrued interest from loan start to today. For fixed-debt loans,
    /// the total interest is known up-front (FixedTotalDebt - LoanAmount - Fees) and
    /// does not grow over time, so that full amount is returned.
    /// </summary>
    public decimal CalculateAccruedInterest()
    {
        if (FixedTotalDebt.HasValue)
            return Math.Max(0, FixedTotalDebt.Value - LoanAmount - Fees);

        var daysSinceStart = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - LoanStartDate.DayNumber);
        if (daysSinceStart <= 0)
            return 0;

        return Math.Round(LoanAmount * Apr / 365 * daysSinceStart, 2);
    }

    /// <summary>
    /// Calculates the total debt obligation. For APR-based loans this is
    /// LoanAmount + AccruedInterest + Fees. For fixed-debt loans the predefined
    /// total is returned regardless of elapsed time.
    /// </summary>
    public decimal CalculateTotalDebt()
    {
        if (FixedTotalDebt.HasValue)
            return FixedTotalDebt.Value;

        return LoanAmount + CalculateAccruedInterest() + Fees;
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
    /// Calculates the net worth impact as a pure liability: -TotalDebt.
    /// The BTC collateral is expected to be tracked separately in a BTC account,
    /// so the loan only represents the debt obligation to avoid double-counting.
    /// The btcPrice parameter is accepted for interface compatibility but not used.
    /// </summary>
    public decimal CalculateCurrentValue(decimal btcPrice)
    {
        return -CalculateTotalDebt();
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
            newPrice,
            FixedTotalDebt);
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
            CurrentBtcPriceInLoanCurrency,
            FixedTotalDebt);
    }

    /// <summary>
    /// Derives an annualized percentage rate from a fixed total debt and loan period.
    /// Returns 0 when the required inputs are missing.
    /// </summary>
    public static decimal DeriveAprFromFixedDebt(
        decimal loanAmount,
        decimal fixedTotalDebt,
        DateOnly loanStartDate,
        DateOnly? repaymentDate)
    {
        if (loanAmount <= 0 || !repaymentDate.HasValue)
            return 0m;

        var days = repaymentDate.Value.DayNumber - loanStartDate.DayNumber;
        if (days <= 0)
            return 0m;

        var interest = fixedTotalDebt - loanAmount;
        if (interest <= 0)
            return 0m;

        return Math.Round(interest / loanAmount * 365m / days, 6);
    }
}
