using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Domain.Assets.Details;

[TestFixture]
public class RealEstateAssetDetailsTests
{
    #region Construction Tests

    [Test]
    public void Should_Create_With_Valid_Parameters()
    {
        // Act
        var details = new RealEstateAssetDetails(500000m, "USD", "123 Main St", 2500m);

        // Assert
        Assert.That(details.AssetType, Is.EqualTo(AssetTypes.RealEstate));
        Assert.That(details.CurrentValue, Is.EqualTo(500000m));
        Assert.That(details.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(details.Address, Is.EqualTo("123 Main St"));
        Assert.That(details.MonthlyRentalIncome, Is.EqualTo(2500m));
    }

    [Test]
    public void Should_Create_With_Optional_Address()
    {
        // Act
        var details = new RealEstateAssetDetails(500000m, "USD", address: null);

        // Assert
        Assert.That(details.Address, Is.Null);
    }

    [Test]
    public void Should_Create_With_Optional_RentalIncome()
    {
        // Act
        var details = new RealEstateAssetDetails(500000m, "USD", "123 Main St", monthlyRentalIncome: null);

        // Assert
        Assert.That(details.MonthlyRentalIncome, Is.Null);
    }

    [Test]
    public void Should_Allow_Zero_CurrentValue()
    {
        // Act
        var details = new RealEstateAssetDetails(0m, "USD");

        // Assert
        Assert.That(details.CurrentValue, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Throw_For_Negative_CurrentValue()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new RealEstateAssetDetails(-100m, "USD"));
    }

    #endregion

    #region Value Calculation Tests

    [Test]
    public void Should_Return_CurrentValue_As_Value()
    {
        // Arrange
        var details = new RealEstateAssetDetails(500000m, "USD");

        // Act
        var value = details.CalculateCurrentValue(500000m);

        // Assert - RealEstate returns CurrentValue directly
        Assert.That(value, Is.EqualTo(500000m));
    }

    [Test]
    public void Should_Return_CurrentValue_Ignoring_Parameter()
    {
        // Arrange
        var details = new RealEstateAssetDetails(500000m, "USD");

        // Act - Pass different price, but it should be ignored
        var value = details.CalculateCurrentValue(1000000m);

        // Assert - Still returns stored CurrentValue
        Assert.That(value, Is.EqualTo(500000m));
    }

    #endregion

    #region Builder Method Tests

    [Test]
    public void Should_Create_New_Details_With_Updated_Price()
    {
        // Arrange
        var original = new RealEstateAssetDetails(500000m, "USD", "123 Main St", 2500m);

        // Act
        var updated = (RealEstateAssetDetails)original.WithUpdatedPrice(600000m);

        // Assert
        Assert.That(updated.CurrentValue, Is.EqualTo(600000m));
        Assert.That(updated.CurrencyCode, Is.EqualTo(original.CurrencyCode));
        Assert.That(updated.Address, Is.EqualTo(original.Address));
        Assert.That(updated.MonthlyRentalIncome, Is.EqualTo(original.MonthlyRentalIncome));
    }

    [Test]
    public void Should_Create_New_Details_With_Updated_RentalIncome()
    {
        // Arrange
        var original = new RealEstateAssetDetails(500000m, "USD", "123 Main St", 2500m);

        // Act
        var updated = original.WithRentalIncome(3000m);

        // Assert
        Assert.That(updated.MonthlyRentalIncome, Is.EqualTo(3000m));
        Assert.That(updated.CurrentValue, Is.EqualTo(original.CurrentValue));
        Assert.That(updated.CurrencyCode, Is.EqualTo(original.CurrencyCode));
        Assert.That(updated.Address, Is.EqualTo(original.Address));
    }

    [Test]
    public void Should_Set_RentalIncome_To_Null()
    {
        // Arrange
        var original = new RealEstateAssetDetails(500000m, "USD", "123 Main St", 2500m);

        // Act
        var updated = original.WithRentalIncome(null);

        // Assert
        Assert.That(updated.MonthlyRentalIncome, Is.Null);
    }

    #endregion

    #region Currency Tests

    [Test]
    public void Should_Support_Different_Currencies()
    {
        // Arrange & Act
        var usdDetails = new RealEstateAssetDetails(500000m, "USD");
        var brlDetails = new RealEstateAssetDetails(2500000m, "BRL");
        var eurDetails = new RealEstateAssetDetails(450000m, "EUR");

        // Assert
        Assert.That(usdDetails.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(brlDetails.CurrencyCode, Is.EqualTo("BRL"));
        Assert.That(eurDetails.CurrencyCode, Is.EqualTo("EUR"));
    }

    #endregion
}
