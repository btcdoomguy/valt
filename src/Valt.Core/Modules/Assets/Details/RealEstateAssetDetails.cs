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

    public RealEstateAssetDetails(
        decimal currentValue,
        string currencyCode,
        string? address = null,
        decimal? monthlyRentalIncome = null)
    {
        if (currentValue < 0)
            throw new ArgumentException("Current value cannot be negative", nameof(currentValue));

        CurrentValue = currentValue;
        CurrencyCode = currencyCode;
        Address = address;
        MonthlyRentalIncome = monthlyRentalIncome;
    }

    public decimal CalculateCurrentValue(decimal currentPrice) => CurrentValue;

    public IAssetDetails WithUpdatedPrice(decimal newPrice)
    {
        return new RealEstateAssetDetails(
            newPrice,
            CurrencyCode,
            Address,
            MonthlyRentalIncome);
    }

    public RealEstateAssetDetails WithRentalIncome(decimal? monthlyRentalIncome)
    {
        return new RealEstateAssetDetails(
            CurrentValue,
            CurrencyCode,
            Address,
            monthlyRentalIncome);
    }
}
