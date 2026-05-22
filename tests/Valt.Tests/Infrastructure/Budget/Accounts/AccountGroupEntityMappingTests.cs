using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Tests.Builders;

namespace Valt.Tests.Infrastructure.Budget.Accounts;

/// <summary>
/// Tests for AccountGroup domain/entity mapping.
/// </summary>[TestFixture]
public class AccountGroupEntityMappingTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [Test]
    public void AsDomainObject_Should_Map_All_Properties()
    {
        // Arrange
        var entity = AccountGroupBuilder.AGroup()
            .WithName("Test Group")
            .WithDisplayOrder(5)
            .WithVersion(3)
            .WithTotalCurrency(AccountGroupTotalCurrency.Fiat("EUR"))
            .BuildEntity();

        // Act
        var domain = entity.AsDomainObject();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(domain.Name.Value, Is.EqualTo("Test Group"));
            Assert.That(domain.DisplayOrder, Is.EqualTo(5));
            Assert.That(domain.Version, Is.EqualTo(3));
            Assert.That(domain.TotalCurrency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.SpecificFiat));
            Assert.That(domain.TotalCurrency.CurrencyCode, Is.EqualTo("EUR"));
        });
    }

    [Test]
    public void AsDomainObject_WithDefaultFiat_Should_Map_Correctly()
    {
        // Arrange
        var entity = new AccountGroupEntity
        {
            Id = new LiteDB.ObjectId(),
            Name = "Default Group",
            DisplayOrder = 0,
            Version = 1,
            TotalCurrency = "DEFAULT"
        };

        // Act
        var domain = entity.AsDomainObject();

        // Assert
        Assert.That(domain.TotalCurrency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void AsDomainObject_WithBitcoin_Should_Map_Correctly()
    {
        // Arrange
        var entity = new AccountGroupEntity
        {
            Id = new LiteDB.ObjectId(),
            Name = "Bitcoin Group",
            DisplayOrder = 0,
            Version = 1,
            TotalCurrency = "BTC"
        };

        // Act
        var domain = entity.AsDomainObject();

        // Assert
        Assert.That(domain.TotalCurrency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.Bitcoin));
    }

    [Test]
    public void AsDomainObject_WithInvalidCurrency_Should_Fallback_To_DefaultFiat()
    {
        // Arrange
        var entity = new AccountGroupEntity
        {
            Id = new LiteDB.ObjectId(),
            Name = "Invalid Group",
            DisplayOrder = 0,
            Version = 1,
            TotalCurrency = "INVALID"
        };

        // Act
        var domain = entity.AsDomainObject();

        // Assert
        Assert.That(domain.TotalCurrency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }

    [Test]
    public void AsEntity_Should_Map_All_Properties()
    {
        // Arrange
        var domain = AccountGroupBuilder.AGroup()
            .WithName("Test Group")
            .WithDisplayOrder(5)
            .WithVersion(3)
            .WithTotalCurrency(AccountGroupTotalCurrency.Fiat("BRL"))
            .Build();

        // Act
        var entity = domain.AsEntity();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.Name, Is.EqualTo("Test Group"));
            Assert.That(entity.DisplayOrder, Is.EqualTo(5));
            Assert.That(entity.Version, Is.EqualTo(3));
            Assert.That(entity.TotalCurrency, Is.EqualTo("BRL"));
        });
    }

    [Test]
    public void RoundTrip_DomainToEntityToDomain_Should_Preserve_TotalCurrency()
    {
        // Arrange
        var original = AccountGroupBuilder.AGroup()
            .WithTotalCurrency(AccountGroupTotalCurrency.Bitcoin())
            .Build();

        // Act
        var entity = original.AsEntity();
        var restored = entity.AsDomainObject();

        // Assert
        Assert.That(restored.TotalCurrency, Is.EqualTo(original.TotalCurrency));
    }

    [Test]
    public void RoundTrip_WithDefaultFiat_Should_Preserve_DefaultFiat()
    {
        // Arrange
        var original = AccountGroup.New(AccountGroupName.New("Test"));

        // Act
        var entity = original.AsEntity();
        var restored = entity.AsDomainObject();

        // Assert
        Assert.That(restored.TotalCurrency.Type, Is.EqualTo(AccountGroupTotalCurrency.TotalCurrencyType.DefaultFiat));
    }
}
