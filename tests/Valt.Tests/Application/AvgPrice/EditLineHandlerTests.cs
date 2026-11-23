using Valt.App.Modules.AvgPrice.Commands.AddLine;
using Valt.App.Modules.AvgPrice.Commands.EditLine;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.AvgPrice;

[TestFixture]
public class EditLineHandlerTests : DatabaseTest
{
    private EditLineHandler _editHandler = null!;
    private AddLineHandler _addHandler = null!;

    [SetUp]
    public void SetUpHandlers()
    {
        _localDatabase.GetAvgPriceLines().DeleteAll();
        _localDatabase.GetAvgPriceProfiles().DeleteAll();

        _editHandler = new EditLineHandler(_avgPriceRepository);
        _addHandler = new AddLineHandler(_avgPriceRepository, new AddLineValidator());
    }

    [Test]
    public async Task HandleAsync_EditingFirstLineOnSameDate_PreservesDisplayOrderAndRecalculatesInOrder()
    {
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();
        var sameDate = new DateOnly(2024, 1, 15);

        var firstLine = profile.AddLine(sameDate, 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "First buy");
        var secondLine = profile.AddLine(sameDate, 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(60000m), "Second buy");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        var command = new EditLineCommand
        {
            ProfileId = profile.Id.Value,
            LineId = firstLine.Id.Value,
            Date = sameDate,
            LineTypeId = (int)AvgPriceLineTypes.Buy,
            Quantity = 1m,
            Amount = 40000m,
            Comment = "First buy edited"
        };

        var result = await _editHandler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedProfile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var orderedLines = updatedProfile!.AvgPriceLines.OrderBy(l => l.DisplayOrder).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(orderedLines, Has.Count.EqualTo(2));
            Assert.That(orderedLines[0].Comment, Is.EqualTo("First buy edited"));
            Assert.That(orderedLines[0].DisplayOrder, Is.EqualTo(0));
            Assert.That(orderedLines[0].Totals.TotalCost, Is.EqualTo(40000m));
            Assert.That(orderedLines[1].Id.Value, Is.EqualTo(secondLine.Id.Value));
            Assert.That(orderedLines[1].DisplayOrder, Is.EqualTo(1));
            Assert.That(orderedLines[1].Totals.TotalCost, Is.EqualTo(100000m));
        });
    }

    [Test]
    public async Task AddLineHandler_AfterRemovingEarlierLine_ReturnsNewlyCreatedLineId()
    {
        var profile = AvgPriceProfileBuilder.AProfile()
            .WithCurrency(FiatCurrency.Usd)
            .Build();
        var date = new DateOnly(2024, 1, 15);

        var line1 = profile.AddLine(date, 0, AvgPriceLineTypes.Buy, 1m, FiatValue.New(50000m), "Line 1");
        var line2 = profile.AddLine(date, 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(60000m), "Line 2");
        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // Remove the first line to free the first HashSet slot
        var loaded = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var toRemove = loaded!.AvgPriceLines.First(l => l.Id == line1.Id);
        loaded.RemoveLine(toRemove);
        await _avgPriceRepository.SaveAvgPriceProfileAsync(loaded);

        var addResult = await _addHandler.HandleAsync(new AddLineCommand
        {
            ProfileId = profile.Id.Value,
            Date = date,
            LineTypeId = (int)AvgPriceLineTypes.Buy,
            Quantity = 2m,
            Amount = 140000m,
            Comment = "Line 3"
        });

        Assert.That(addResult.IsSuccess, Is.True);
        Assert.That(addResult.Value!.LineId, Is.Not.EqualTo(line2.Id.Value));

        var reloaded = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profile.Id);
        var addedLine = reloaded!.AvgPriceLines.FirstOrDefault(l => l.Id.Value == addResult.Value.LineId);
        Assert.That(addedLine, Is.Not.Null);
        Assert.That(addedLine!.Comment, Is.EqualTo("Line 3"));
    }
}
