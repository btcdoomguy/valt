using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.DeleteLine;

internal sealed class DeleteLineHandler : ICommandHandler<DeleteLineCommand, DeleteLineResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    public DeleteLineHandler(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public async Task<Result<DeleteLineResult>> HandleAsync(
        DeleteLineCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileId))
            return Result<DeleteLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.ProfileId), ["Profile ID is required"] }
                }));

        if (string.IsNullOrWhiteSpace(command.LineId))
            return Result<DeleteLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.LineId), ["Line ID is required"] }
                }));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(
            new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<DeleteLineResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        var line = profile.AvgPriceLines.FirstOrDefault(l => l.Id.Value == command.LineId);
        if (line is null)
            return Result<DeleteLineResult>.Failure(
                "LINE_NOT_FOUND", $"Line with id {command.LineId} not found");

        profile.RemoveLine(line);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        return Result<DeleteLineResult>.Success(new DeleteLineResult());
    }
}
