using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.MoveLine;

internal sealed class MoveLineHandler : ICommandHandler<MoveLineCommand, MoveLineResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    public MoveLineHandler(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public async Task<Result<MoveLineResult>> HandleAsync(
        MoveLineCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileId))
            return Result<MoveLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.ProfileId), ["Profile ID is required"] }
                }));

        if (string.IsNullOrWhiteSpace(command.LineId))
            return Result<MoveLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.LineId), ["Line ID is required"] }
                }));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<MoveLineResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        var line = profile.AvgPriceLines.FirstOrDefault(l => l.Id.Value == command.LineId);

        if (line is null)
            return Result<MoveLineResult>.Failure(
                "LINE_NOT_FOUND", $"Line with id {command.LineId} not found");

        // Move up or down based on direction
        if (command.Direction == 0)
            profile.MoveLineUp(line);
        else
            profile.MoveLineDown(line);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        return Result<MoveLineResult>.Success(new MoveLineResult());
    }
}
