using System.Collections.Generic;
using System.Linq;

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
    /// Ordered timeline of loan-state snapshots. The latest snapshot (by effective date)
    /// is the source of truth for calculations; when empty, the immutable setup values are used.
    /// </summary>
    public IReadOnlyList<LoanStateSnapshot> Snapshots { get; }

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
        decimal? fixedTotalDebt = null,
        IReadOnlyList<LoanStateSnapshot>? snapshots = null)
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
        Snapshots = snapshots ?? new List<LoanStateSnapshot>().AsReadOnly();
    }

    /// <summary>
    /// Returns the latest snapshot (by effective date), or <c>null</c> when no snapshots exist.
    /// All current-state calculations route through this method so the latest snapshot is the
    /// single source of truth, falling back to the immutable setup values when empty.
    /// </summary>
    private LoanStateSnapshot? GetEffectiveSnapshot()
        => Snapshots.Count == 0 ? null : Snapshots.MaxBy(s => s.EffectiveDate);

    /// <summary>
    /// Calculates the current LTV ratio given a BTC price.
    /// LTV = LoanAmount / CollateralValue
    /// </summary>
    public decimal CalculateCurrentLtv(decimal btcPrice)
    {
        var snapshot = GetEffectiveSnapshot();
        var collateralSats = snapshot?.CollateralSats ?? CollateralSats;
        var loanAmount = snapshot?.LoanAmount ?? LoanAmount;

        var collateralValue = collateralSats / 100_000_000m * btcPrice;
        if (collateralValue == 0)
            return 0;

        return Math.Round(loanAmount / collateralValue * 100, 2);
    }

    /// <summary>
    /// Determines the health status of the loan based on current LTV.
    /// </summary>
    public LoanHealthStatus CalculateHealthStatus(decimal btcPrice)
    {
        var snapshot = GetEffectiveSnapshot();
        var liquidationLtv = snapshot?.LiquidationLtv ?? LiquidationLtv;
        var marginCallLtv = snapshot?.MarginCallLtv ?? MarginCallLtv;

        var currentLtv = CalculateCurrentLtv(btcPrice);
        if (currentLtv >= liquidationLtv)
            return LoanHealthStatus.Danger;
        if (currentLtv >= marginCallLtv)
            return LoanHealthStatus.Warning;
        return LoanHealthStatus.Healthy;
    }

    /// <summary>
    /// Calculates the distance (in percentage points) to the liquidation LTV.
    /// </summary>
    public decimal CalculateDistanceToLiquidation(decimal btcPrice)
    {
        var snapshot = GetEffectiveSnapshot();
        var liquidationLtv = snapshot?.LiquidationLtv ?? LiquidationLtv;

        var currentLtv = CalculateCurrentLtv(btcPrice);
        return Math.Round(Math.Max(0, liquidationLtv - currentLtv), 2);
    }

    /// <summary>
    /// Calculates the accrued interest from loan start to today. For fixed-debt loans,
    /// the total interest is known up-front (FixedTotalDebt - LoanAmount - Fees) and
    /// does not grow over time, so that full amount is returned.
    /// When a snapshot exists, its <see cref="LoanStateSnapshot.CurrentTotalDebt"/> is
    /// authoritative and already encompasses interest and fees, so this returns 0.
    /// </summary>
    public decimal CalculateAccruedInterest()
    {
        if (GetEffectiveSnapshot() is not null)
            return 0m;

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
    /// When a snapshot exists, its <see cref="LoanStateSnapshot.CurrentTotalDebt"/> is used.
    /// </summary>
    public decimal CalculateTotalDebt()
    {
        var snapshot = GetEffectiveSnapshot();
        if (snapshot is not null)
            return snapshot.CurrentTotalDebt;

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
            FixedTotalDebt,
            Snapshots);
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
            FixedTotalDebt,
            Snapshots);
    }

    /// <summary>
    /// Returns a new <see cref="BtcLoanDetails"/> with the supplied snapshot added.
    /// Throws <see cref="ArgumentException"/> if a snapshot for the same effective date already exists.
    /// </summary>
    public BtcLoanDetails WithAddedSnapshot(LoanStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Snapshots.Any(s => s.EffectiveDate == snapshot.EffectiveDate))
        {
            throw new ArgumentException(
                $"A snapshot already exists for {snapshot.EffectiveDate}",
                nameof(snapshot));
        }

        var newSnapshots = Snapshots
            .Append(snapshot)
            .OrderBy(s => s.EffectiveDate)
            .ToList()
            .AsReadOnly();

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
            CurrentBtcPriceInLoanCurrency,
            FixedTotalDebt,
            newSnapshots);
    }

    /// <summary>
    /// Returns a new <see cref="BtcLoanDetails"/> without the snapshot matching the supplied effective date.
    /// </summary>
    public BtcLoanDetails WithoutSnapshot(DateOnly effectiveDate)
    {
        var newSnapshots = Snapshots
            .Where(s => s.EffectiveDate != effectiveDate)
            .OrderBy(s => s.EffectiveDate)
            .ToList()
            .AsReadOnly();

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
            CurrentBtcPriceInLoanCurrency,
            FixedTotalDebt,
            newSnapshots);
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
