using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.DeleteProfile;

internal sealed class DeleteProfileHandler : ICommandHandler<DeleteProfileCommand, DeleteProfileResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    public DeleteProfileHandler(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public async Task<Result<DeleteProfileResult>> HandleAsync(
        DeleteProfileCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileId))
            return Result<DeleteProfileResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.ProfileId), ["Profile ID is required"] }
                }));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(
            new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<DeleteProfileResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        await _avgPriceRepository.DeleteAvgPriceProfileAsync(profile);

        return Result<DeleteProfileResult>.Success(new DeleteProfileResult());
    }
}
