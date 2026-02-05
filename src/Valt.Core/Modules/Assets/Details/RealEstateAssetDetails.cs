namespace Valt.Core.Modules.Assets.Details;

/// <summary>
/// Asset details specific to real estate investments.
/// </summary>
public sealed class RealEstateAssetDetails : IAssetDetails
{
    public AssetTypes AssetType => AssetTypes.RealEstate;

    /// <summary>
    /// The current estimated value of the property.
    /// </summary>
    public decimal CurrentValue { get; }

    /// <summary>
    /// The currency code for the value (e.g., "USD", "BRL").
    /// </summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// Optional address or description of the property.
    /// </summary>
    public string? Address { get; }

    /// <summary>
    /// Optional monthly rental income.
    /// </summary>
    public decimal? MonthlyRentalIncome { get; }

    /// <summary>
    /// The date when the property was acquired (optional).
    /// </summary>
    public DateOnly? AcquisitionDate { get; }

    /// <summary>
    /// The total purchase price at acquisition (optional).
    /// </summary>
    public decimal? AcquisitionPrice { get; }

    public RealEstateAssetDetails(
        decimal currentValue,
        string currencyCode,
        string? address = null,
        decimal? monthlyRentalIncome = null,
        DateOnly? acquisitionDate = null,
        decimal? acquisitionPrice = null)
    {
        if (currentValue < 0)
            throw new ArgumentException("Current value cannot be negative", nameof(currentValue));

        CurrentValue = currentValue;
        CurrencyCode = currencyCode;
        Address = address;
        MonthlyRentalIncome = monthlyRentalIncome;
        AcquisitionDate = acquisitionDate;
        AcquisitionPrice = acquisitionPrice;
    }

    public decimal CalculateCurrentValue(decimal currentPrice) => CurrentValue;

    /// <summary>
    /// Calculates the profit/loss based on acquisition price.
    /// </summary>
    public decimal CalculatePnL() => AcquisitionPrice.HasValue
        ? CurrentValue - AcquisitionPrice.Value
        : 0;

    /// <summary>
    /// Calculates the profit/loss percentage based on acquisition price.
    /// </summary>
    public decimal CalculatePnLPercentage() => AcquisitionPrice.HasValue && AcquisitionPrice.Value != 0
        ? Math.Round((CurrentValue - AcquisitionPrice.Value) / AcquisitionPrice.Value * 100, 2)
        : 0;

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new RealEstateAssetDetails(
            newPrice,
            CurrencyCode,
            Address,
            MonthlyRentalIncome,
            AcquisitionDate,
            AcquisitionPrice);
    }

    public RealEstateAssetDetails WithRentalIncome(decimal? monthlyRentalIncome)
    {
        return new RealEstateAssetDetails(
            CurrentValue,
            CurrencyCode,
            Address,
            monthlyRentalIncome,
            AcquisitionDate,
            AcquisitionPrice);
    }
}
