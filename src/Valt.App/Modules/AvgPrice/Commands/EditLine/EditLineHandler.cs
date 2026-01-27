using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.EditLine;

internal sealed class EditLineHandler : ICommandHandler<EditLineCommand, EditLineResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    public EditLineHandler(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public async Task<Result<EditLineResult>> HandleAsync(
        EditLineCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileId))
            return Result<EditLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.ProfileId), ["Profile ID is required"] }
                }));

        if (string.IsNullOrWhiteSpace(command.LineId))
            return Result<EditLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.LineId), ["Line ID is required"] }
                }));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<EditLineResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        var existingLine = profile.AvgPriceLines.FirstOrDefault(l => l.Id.Value == command.LineId);

        if (existingLine is null)
            return Result<EditLineResult>.Failure(
                "LINE_NOT_FOUND", $"Line with id {command.LineId} not found");

        // Remove the existing line and add a new one with updated values
        // This approach maintains data integrity through the domain's recalculation logic
        var displayOrder = existingLine.DisplayOrder;
        profile.RemoveLine(existingLine);

        var lineType = (AvgPriceLineTypes)command.LineTypeId;
        var amount = FiatValue.New(command.Amount);

        profile.AddLine(
            command.Date,
            displayOrder,
            lineType,
            command.Quantity,
            amount,
            command.Comment ?? string.Empty);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        return Result<EditLineResult>.Success(new EditLineResult());
    }
}
