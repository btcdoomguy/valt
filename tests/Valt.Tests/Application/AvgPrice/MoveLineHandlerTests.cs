using Valt.App.Modules.AvgPrice.Commands.MoveLine;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Tests.Builders;


namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class MoveLineHandlerTests : DatabaseTest
{
    private MoveLineHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        // Clean up any existing data from previous tests
        _localDatabase.GetAvgPriceLines().DeleteAll();
        _localDatabase.GetAvgPriceProfiles().DeleteAll();

        _handler = new MoveLineHandler(_avgPriceRepository);
    }

    [Test]
    public async Task HandleAsync_MovesLineUp()
    {
        // Create profile and add lines via AddLine to generate events for persistence
        var profile = AvgPriceProfileBuilder.AProfile().Build();
        var testDate = new DateOnly(2024, 1, 15);

        // Add first line
        profile.AddLine(testDate, 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(100m), "Line 1");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // Add second line
        profile.AddLine(testDate, 1, AvgPriceLineTypes.Buy, 2m, FiatValue.New(200m), "Line 2");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // Get the saved profile to get actual line IDs
        var savedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var lines = savedProfile!.AvgPriceLines.OrderBy(l => l.DisplayOrder).ToList();
        var line1Id = lines[0].Id.Value;
        var line2Id = lines[1].Id.Value;

        // Move line2 up (direction = 0)
        var command = new MoveLineCommand
        {
            ProfileId = profile.Id.Value,
            LineId = line2Id,
            Direction = 0 // 0 = up
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True, () => $"Error: {result.Error?.Code} - {result.Error?.Message}");

        var updatedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var orderedLines = updatedProfile!.AvgPriceLines.OrderBy(l => l.DisplayOrder).ToList();
        Assert.That(orderedLines[0].Id.Value, Is.EqualTo(line2Id));
        Assert.That(orderedLines[1].Id.Value, Is.EqualTo(line1Id));
    }

    [Test]
    public async Task HandleAsync_MovesLineDown()
    {
        // Create profile and add lines via AddLine to generate events for persistence
        var profile = AvgPriceProfileBuilder.AProfile().Build();
        var testDate = new DateOnly(2024, 1, 15);

        // Add first line
        profile.AddLine(testDate, 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(100m), "Line 1");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // Add second line
        profile.AddLine(testDate, 1, AvgPriceLineTypes.Buy, 2m, FiatValue.New(200m), "Line 2");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // Get the saved profile to get actual line IDs
        var savedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var lines = savedProfile!.AvgPriceLines.OrderBy(l => l.DisplayOrder).ToList();
        var line1Id = lines[0].Id.Value;
        var line2Id = lines[1].Id.Value;

        // Move line1 down (direction = 1)
        var command = new MoveLineCommand
        {
            ProfileId = profile.Id.Value,
            LineId = line1Id,
            Direction = 1 // 1 = down
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var orderedLines = updatedProfile!.AvgPriceLines.OrderBy(l => l.DisplayOrder).ToList();
        Assert.That(orderedLines[0].Id.Value, Is.EqualTo(line2Id));
        Assert.That(orderedLines[1].Id.Value, Is.EqualTo(line1Id));
    }

    [Test]
    public async Task HandleAsync_WithEmptyProfileId_ReturnsValidationError()
    {
        var command = new MoveLineCommand
        {
            ProfileId = "",
            LineId = "some-line-id",
            Direction = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyLineId_ReturnsValidationError()
    {
        var profile = AvgPriceProfileBuilder.AProfile().Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new MoveLineCommand
        {
            ProfileId = profile.Id.Value,
            LineId = "",
            Direction = 0
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
        var command = new MoveLineCommand
        {
            ProfileId = "000000000000000000000001",
            LineId = "some-line-id",
            Direction = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("PROFILE_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentLineId_ReturnsLineNotFound()
    {
        var profile = AvgPriceProfileBuilder.AProfile().Build();
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new MoveLineCommand
        {
            ProfileId = profile.Id.Value,
            LineId = "000000000000000000000001",
            Direction = 0
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("LINE_NOT_FOUND"));
        });
    }
}
