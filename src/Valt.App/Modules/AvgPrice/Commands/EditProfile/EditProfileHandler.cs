using System.Drawing;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.EditProfile;

internal sealed class EditProfileHandler : ICommandHandler<EditProfileCommand, EditProfileResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    public EditProfileHandler(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public async Task<Result<EditProfileResult>> HandleAsync(
        EditProfileCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProfileId))
            return Result<EditProfileResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.ProfileId), ["Profile ID is required"] }
                }));

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<EditProfileResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.Name), ["Name is required"] }
                }));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<EditProfileResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        // Apply changes
        profile.Rename(AvgPriceProfileName.New(command.Name));
        profile.ChangeAsset(command.AssetName, command.Precision);
        profile.ChangeVisibility(command.Visible);
        profile.ChangeCalculationMethod((AvgPriceCalculationMethod)command.CalculationMethodId);

        var icon = string.IsNullOrWhiteSpace(command.IconName)
            ? Icon.Empty
            : new Icon("", command.IconName, command.IconUnicode, Color.FromArgb(command.IconColor));
        profile.ChangeIcon(icon);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        return Result<EditProfileResult>.Success(new EditProfileResult());
    }
}
