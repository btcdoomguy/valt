namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Asset details for BTC/fiat lending positions (lending to a borrower or platform).
/// </summary>
public sealed class BtcLendingDetails : IAssetDetails
{
    public AssetTypes AssetType => AssetTypes.BtcLending;

    /// <summary>
    /// The amount lent.
    /// </summary>
    public decimal AmountLent { get; }

    /// <summary>
    /// Currency code for the lending (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// Annual percentage rate earned (e.g., 0.05 for 5%).
    /// </summary>
    public decimal Apr { get; }

    /// <summary>
    /// Expected repayment date (null = indefinite/open-ended).
    /// </summary>
    public DateOnly? ExpectedRepaymentDate { get; }

    /// <summary>
    /// The borrower name or platform name.
    /// </summary>
    public string BorrowerOrPlatformName { get; }

    /// <summary>
    /// When the lending started.
    /// </summary>
    public DateOnly LendingStartDate { get; }

    /// <summary>
    /// Current lending status.
    /// </summary>
    public LoanStatus Status { get; }

    public BtcLendingDetails(
        decimal amountLent,
        string currencyCode,
        decimal apr,
        DateOnly? expectedRepaymentDate,
        string borrowerOrPlatformName,
        DateOnly lendingStartDate,
        LoanStatus status)
    {
        if (amountLent <= 0)
            throw new ArgumentException("Amount lent must be positive", nameof(amountLent));

        if (apr < 0)
            throw new ArgumentException("APR cannot be negative", nameof(apr));

        AmountLent = amountLent;
        CurrencyCode = currencyCode;
        Apr = apr;
        ExpectedRepaymentDate = expectedRepaymentDate;
        BorrowerOrPlatformName = borrowerOrPlatformName;
        LendingStartDate = lendingStartDate;
        Status = status;
    }

    /// <summary>
    /// Calculates the interest earned from lending start to today.
    /// </summary>
    public decimal CalculateEarnedInterest()
    {
        var daysSinceStart = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - LendingStartDate.DayNumber;
        if (daysSinceStart <= 0)
            return 0;

        return Math.Round(AmountLent * Apr / 365 * daysSinceStart, 2);
    }

    /// <summary>
    /// Calculates the number of days until the expected repayment date.
    /// Returns null if no repayment date is set.
    /// </summary>
    public int? CalculateDaysUntilRepayment()
    {
        if (ExpectedRepaymentDate is null)
            return null;

        var days = ExpectedRepaymentDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        return Math.Max(0, days);
    }

    /// <summary>
    /// Calculates the current value: AmountLent + EarnedInterest.
    /// The price parameter is ignored since lending positions are not price-sensitive.
    /// </summary>
    public decimal CalculateCurrentValue(decimal currentPrice)
    {
        return AmountLent + CalculateEarnedInterest();
    }

    /// <summary>
    /// Returns self since lending positions are not price-sensitive.
    /// </summary>
    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return this;
    }

    public BtcLendingDetails WithStatus(LoanStatus newStatus)
    {
        return new BtcLendingDetails(
            AmountLent,
            CurrencyCode,
            Apr,
            ExpectedRepaymentDate,
            BorrowerOrPlatformName,
            LendingStartDate,
            newStatus);
    }
}
