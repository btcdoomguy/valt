using Valt.App.Modules.AvgPrice.Commands.EditProfile;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class EditProfileHandlerTests : DatabaseTest
{
    private EditProfileHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        // Clean up any existing data from previous tests
        _localDatabase.GetAvgPriceLines().DeleteAll();
        _localDatabase.GetAvgPriceProfiles().DeleteAll();

        _handler = new EditProfileHandler(_avgPriceRepository);
    }

    [Test]
    public async Task HandleAsync_WithValidData_UpdatesProfile()
    {
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithName(AvgPriceProfileName.New("Original Name"))
            .Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new EditProfileCommand
        {
            ProfileId = profile.Id.Value,
            Name = "Updated Name",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CalculationMethodId = 0,
            IconName = "",
            IconUnicode = char.MinValue,
            IconColor = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(updatedProfile!.Name.Value, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task HandleAsync_WithEmptyProfileId_ReturnsValidationError()
    {
        var command = new EditProfileCommand
        {
            ProfileId = "",
            Name = "Name",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CalculationMethodId = 0,
            IconName = "",
            IconUnicode = char.MinValue,
            IconColor = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var profile = AvgPriceProfileBuilder.AProfile().Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new EditProfileCommand
        {
            ProfileId = profile.Id.Value,
            Name = "",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CalculationMethodId = 0,
            IconName = "",
            IconUnicode = char.MinValue,
            IconColor = 0
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
        var command = new EditProfileCommand
        {
            ProfileId = "000000000000000000000001",
            Name = "Name",
            AssetName = "BTC",
            Precision = 8,
            Visible = true,
            CalculationMethodId = 0,
            IconName = "",
            IconUnicode = char.MinValue,
            IconColor = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("PROFILE_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_ChangesVisibility()
    {
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithVisible(true)
            .Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new EditProfileCommand
        {
            ProfileId = profile.Id.Value,
            Name = "Test",
            AssetName = "BTC",
            Precision = 8,
            Visible = false,
            CalculationMethodId = 0,
            IconName = "",
            IconUnicode = char.MinValue,
            IconColor = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        Assert.That(updatedProfile!.Visible, Is.False);
    }
}
