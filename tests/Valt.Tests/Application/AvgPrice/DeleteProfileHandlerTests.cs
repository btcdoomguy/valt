using LiteDB;
using Valt.App.Modules.AvgPrice.Commands.DeleteProfile;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class DeleteProfileHandlerTests : DatabaseTest
{
    private DeleteProfileHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new DeleteProfileHandler(_avgPriceRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidProfileId_DeletesProfile()
    {
        // Create a profile first
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName("Test Profile")
            .Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new DeleteProfileCommand
        {
            ProfileId = profile.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        // Verify deletion
        var entity = _localDatabase.GetAvgPriceProfiles().FindById(new ObjectId(profile.Id.Value));
        Assert.That(entity, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithEmptyProfileId_ReturnsValidationError()
    {
        var command = new DeleteProfileCommand
        {
            ProfileId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentProfileId_ReturnsNotFound()
    {
        var command = new DeleteProfileCommand
        {
            ProfileId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("PROFILE_NOT_FOUND"));
        });
    }
}
