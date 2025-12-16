using LiteDB;
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Events;
using Valt.Infra.Modules.AvgPrice;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.AvgPrice;

[TestFixture]
public class AvgPriceRepositoryTests : DatabaseTest
{
    #region Save and Retrieve Tests

    [Test]
    public async Task Save_Should_Store_And_Retrieve_Profile()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Test Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert - Events cleared
        Assert.That(profile.Events, Is.Empty);

        // Assert - Domain event published
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AvgPriceProfileCreatedEvent>());

        // Assert - Can retrieve profile
        var restoredProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(restoredProfile, Is.Not.Null);
        Assert.That(restoredProfile!.Id, Is.EqualTo(profile.Id));
        Assert.That(restoredProfile.Name, Is.EqualTo(profile.Name));
        Assert.That(restoredProfile.Visible, Is.EqualTo(profile.Visible));
        Assert.That(restoredProfile.Currency, Is.EqualTo(profile.Currency));
        Assert.That(restoredProfile.CalculationMethod, Is.EqualTo(profile.CalculationMethod));
        Assert.That(restoredProfile.AvgPriceLines, Is.Empty);
    }

    [Test]
    public async Task Save_Should_Store_And_Retrieve_Profile_With_Lines()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Profile With Lines",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 15),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(50000m),
            "First buy");

        profile.AddLine(
            new DateOnly(2024, 2, 20),
            displayOrder: 2,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.3m),
            FiatValue.New(55000m),
            "Second buy");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert - Events cleared
        Assert.That(profile.Events, Is.Empty);

        // Assert - Profile created event + 2 line created events
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AvgPriceProfileCreatedEvent>());
        await _domainEventPublisher.Received(2).PublishAsync(Arg.Any<AvgPriceLineCreatedEvent>());

        // Assert - Can retrieve profile with lines
        var restoredProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(restoredProfile, Is.Not.Null);
        Assert.That(restoredProfile!.AvgPriceLines.Count, Is.EqualTo(2));

        var firstLine = restoredProfile.AvgPriceLines.First(l => l.Comment == "First buy");
        Assert.That(firstLine.Date, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(firstLine.Type, Is.EqualTo(AvgPriceLineTypes.Buy));
        Assert.That(firstLine.BtcAmount.Sats, Is.EqualTo(BtcValue.ParseBitcoin(0.5m).Sats));

        var secondLine = restoredProfile.AvgPriceLines.First(l => l.Comment == "Second buy");
        Assert.That(secondLine.Date, Is.EqualTo(new DateOnly(2024, 2, 20)));
        Assert.That(secondLine.Type, Is.EqualTo(AvgPriceLineTypes.Buy));
    }

    #endregion

    #region Version Tests

    [Test]
    public async Task Save_Should_Preserve_Version_When_Retrieving()
    {
        // Arrange
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Versioned Profile")
            .WithVersion(0) // New profile
            .Build();

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert
        var restoredProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(restoredProfile!.Version, Is.EqualTo(profile.Version));
    }

    #endregion

    #region Events Tests

    [Test]
    public async Task Save_Should_Clear_Events_After_Saving()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Events Test Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);

        // Add a line to generate more events
        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(60000m),
            "Test line");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Verify events exist before save
        Assert.That(profile.Events, Is.Not.Empty);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert - Events should be cleared
        Assert.That(profile.Events, Is.Empty);
    }

    [Test]
    public async Task Save_Should_Publish_ProfileCreated_Event_For_New_Profile()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "New Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert
        await _domainEventPublisher.Received(1).PublishAsync(
            Arg.Is<AvgPriceProfileCreatedEvent>(e => e.AvgPriceProfile.Id == profile.Id));
    }

    [Test]
    public async Task Save_Should_Publish_LineCreated_Event_When_Adding_Line()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Line Events Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 4, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.1m),
            FiatValue.New(70000m),
            "Buy line");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AvgPriceLineCreatedEvent>());
    }

    #endregion

    #region GetAll Tests

    [Test]
    public async Task GetAll_Should_Return_All_Profiles_With_Lines()
    {
        // Arrange
        var profile1 = AvgPriceProfile.New(
            "Profile One",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);
        profile1.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(40000m),
            "Line 1");

        var profile2 = AvgPriceProfile.New(
            "Profile Two",
            visible: false,
            Icon.Empty,
            FiatCurrency.Usd,
            AvgPriceCalculationMethod.BrazilianRule);
        profile2.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(2m),
            FiatValue.New(45000m),
            "Line 2");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        await repository.SaveAvgPriceProfileAsync(profile1);
        await repository.SaveAvgPriceProfileAsync(profile2);

        // Act
        var allProfiles = (await repository.GetAvgPriceProfilesAsync()).ToList();

        // Assert
        Assert.That(allProfiles.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(allProfiles.Any(p => p.Name == "Profile One"));
        Assert.That(allProfiles.Any(p => p.Name == "Profile Two"));
        Assert.That(allProfiles.First(p => p.Name == "Profile One").AvgPriceLines.Count, Is.EqualTo(1));
        Assert.That(allProfiles.First(p => p.Name == "Profile Two").AvgPriceLines.Count, Is.EqualTo(1));
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_Should_Remove_Profile_And_All_Lines()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Profile To Delete",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 5, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(50000m),
            "Line to delete 1");

        profile.AddLine(
            new DateOnly(2024, 5, 15),
            displayOrder: 2,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.3m),
            FiatValue.New(52000m),
            "Line to delete 2");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        await repository.SaveAvgPriceProfileAsync(profile);

        // Verify profile exists
        var savedProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(savedProfile, Is.Not.Null);
        Assert.That(savedProfile!.AvgPriceLines.Count, Is.EqualTo(2));

        // Act
        await repository.DeleteAvgPriceProfileAsync(profile);

        // Assert - Profile should be gone
        var deletedProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(deletedProfile, Is.Null);

        // Assert - Deletion event should be published
        await _domainEventPublisher.Received(1).PublishAsync(
            Arg.Is<AvgPriceProfileDeletedEvent>(e => e.AvgPriceProfile.Id == profile.Id));
    }

    #endregion

    #region Line Types Tests

    [Test]
    public async Task Save_Should_Store_Different_Line_Types_Correctly()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Multiple Line Types Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Setup,
            BtcValue.ParseBitcoin(2m),
            FiatValue.New(30000m),
            "Initial setup");

        profile.AddLine(
            new DateOnly(2024, 2, 1),
            displayOrder: 2,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(45000m),
            "Buy more");

        profile.AddLine(
            new DateOnly(2024, 3, 1),
            displayOrder: 3,
            AvgPriceLineTypes.Sell,
            BtcValue.ParseBitcoin(0.2m),
            FiatValue.New(50000m),
            "Partial sell");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert
        var restoredProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(restoredProfile, Is.Not.Null);
        Assert.That(restoredProfile!.AvgPriceLines.Count, Is.EqualTo(3));

        var setupLine = restoredProfile.AvgPriceLines.First(l => l.Comment == "Initial setup");
        Assert.That(setupLine.Type, Is.EqualTo(AvgPriceLineTypes.Setup));

        var buyLine = restoredProfile.AvgPriceLines.First(l => l.Comment == "Buy more");
        Assert.That(buyLine.Type, Is.EqualTo(AvgPriceLineTypes.Buy));

        var sellLine = restoredProfile.AvgPriceLines.First(l => l.Comment == "Partial sell");
        Assert.That(sellLine.Type, Is.EqualTo(AvgPriceLineTypes.Sell));
    }

    #endregion

    #region Line Totals Tests

    [Test]
    public async Task Save_Should_Persist_Line_Totals()
    {
        // Arrange
        var profile = AvgPriceProfile.New(
            "Totals Test Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 1, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(1m),
            FiatValue.New(50000m),
            "Line with totals");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert
        var restoredProfile = await repository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(restoredProfile, Is.Not.Null);

        var line = restoredProfile!.AvgPriceLines.First();
        // The totals should be calculated by the strategy
        Assert.That(line.Totals, Is.Not.Null);
        Assert.That(line.Totals.BtcAmount.Sats, Is.GreaterThan(0));
    }

    #endregion

    #region Direct Database Access Tests (Diagnostic)

    [Test]
    public async Task Save_Should_Persist_Lines_To_Database_Collection()
    {
        // This test verifies that lines are being persisted to the database
        // by directly querying the collection (bypassing repository retrieval)

        // Arrange
        var profile = AvgPriceProfile.New(
            "Direct DB Test Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 6, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.5m),
            FiatValue.New(55000m),
            "Direct DB test line");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);

        // Act
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert - Directly query the lines collection
        var allLinesInDb = _localDatabase.GetAvgPriceLines().FindAll().ToList();
        var linesForProfile = allLinesInDb.Where(l => l.ProfileId == new ObjectId(profile.Id.ToString())).ToList();

        Assert.That(linesForProfile.Count, Is.EqualTo(1),
            $"Expected 1 line in DB for profile {profile.Id}. Total lines in DB: {allLinesInDb.Count}");
        Assert.That(linesForProfile[0].Comment, Is.EqualTo("Direct DB test line"));
    }

    [Test]
    public async Task GetById_Should_Find_Lines_With_Matching_ProfileId()
    {
        // This test checks if the line retrieval query correctly finds lines by ProfileId

        // Arrange
        var profile = AvgPriceProfile.New(
            "Query Test Profile",
            visible: true,
            Icon.Empty,
            FiatCurrency.Brl,
            AvgPriceCalculationMethod.BrazilianRule);

        profile.AddLine(
            new DateOnly(2024, 7, 1),
            displayOrder: 1,
            AvgPriceLineTypes.Buy,
            BtcValue.ParseBitcoin(0.3m),
            FiatValue.New(60000m),
            "Query test line");

        var repository = new AvgPriceRepository(_localDatabase, _domainEventPublisher);
        await repository.SaveAvgPriceProfileAsync(profile);

        // Assert - Test the query that the repository uses
        var profileObjectId = new ObjectId(profile.Id.ToString());
        var linesFromQuery = _localDatabase.GetAvgPriceLines()
            .Find(x => x.ProfileId == profileObjectId)
            .ToList();

        Assert.That(linesFromQuery.Count, Is.EqualTo(1),
            $"Repository query should find 1 line for ProfileId {profile.Id}");
    }

    #endregion
}
