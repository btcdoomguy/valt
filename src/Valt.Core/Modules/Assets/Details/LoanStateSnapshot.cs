namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// A point-in-time snapshot of a BTC-backed loan's state.
/// </summary>
public sealed class LoanStateSnapshot
{
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
    /// Annual percentage rate at the time of the snapshot (e.g., 0.12 for 12%).
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
    /// Loan status at the time of the snapshot.
    /// </summary>
    public LoanStatus Status { get; }

    /// <summary>
    /// BTC price in the loan's currency at the time of the snapshot, used for LTV calculations.
    /// </summary>
    public decimal CurrentBtcPriceInLoanCurrency { get; }

    /// <summary>
    /// Optional predefined total debt recorded in the snapshot.
    /// </summary>
    public decimal? FixedTotalDebt { get; }

    /// <summary>
    /// The borrowed principal still owed at the time of the snapshot.
    /// </summary>
    public decimal TotalBorrowed { get; }

    /// <summary>
    /// Interest charged up to the snapshot's effective date.
    /// </summary>
    public decimal InterestAccruedUntilDate { get; }

    /// <summary>
    /// The current total debt at the time of the snapshot.
    /// </summary>
    public decimal CurrentTotalDebt => TotalBorrowed + InterestAccruedUntilDate + Fees;

    /// <summary>
    /// The effective date of the snapshot. Only one snapshot is allowed per effective date on a loan.
    /// </summary>
    public DateOnly EffectiveDate { get; }

    /// <summary>
    /// Optional note describing the snapshot. Null or empty values are allowed.
    /// </summary>
    public string? Note { get; }

    /// <summary>
    /// Creates a new <see cref="LoanStateSnapshot"/>.
    /// </summary>
    public LoanStateSnapshot(
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
        decimal? fixedTotalDebt,
        decimal totalBorrowed,
        decimal interestAccruedUntilDate,
        DateOnly effectiveDate,
        string? note = null)
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

        if (fees < 0)
            throw new ArgumentException("Fees cannot be negative", nameof(fees));

        if (totalBorrowed < 0)
            throw new ArgumentException("Total borrowed cannot be negative", nameof(totalBorrowed));

        if (interestAccruedUntilDate < 0)
            throw new ArgumentException("Interest accrued cannot be negative", nameof(interestAccruedUntilDate));

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
        TotalBorrowed = totalBorrowed;
        InterestAccruedUntilDate = interestAccruedUntilDate;
        EffectiveDate = effectiveDate;
        Note = note;
    }

    /// <summary>
    /// Legacy overload that derives total borrowed and accrued interest from a single total debt value.
    /// </summary>
    public LoanStateSnapshot(
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
        decimal? fixedTotalDebt,
        decimal currentTotalDebt,
        DateOnly effectiveDate,
        string? note = null)
        : this(
            platformName,
            collateralSats,
            loanAmount,
            currencyCode,
            apr,
            initialLtv,
            liquidationLtv,
            marginCallLtv,
            fees,
            loanStartDate,
            repaymentDate,
            status,
            currentBtcPriceInLoanCurrency,
            fixedTotalDebt,
            totalBorrowed: loanAmount,
            interestAccruedUntilDate: Math.Max(0m, currentTotalDebt - loanAmount - fees),
            effectiveDate,
            note)
    {
    }
}
