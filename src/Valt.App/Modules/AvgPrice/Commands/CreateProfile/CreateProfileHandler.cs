using System.Drawing;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.CreateProfile;

internal sealed class CreateProfileHandler : ICommandHandler<CreateProfileCommand, CreateProfileResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;
    private readonly IValidator<CreateProfileCommand> _validator;

    public CreateProfileHandler(
        IAvgPriceRepository avgPriceRepository,
        IValidator<CreateProfileCommand> validator)
    {
        _avgPriceRepository = avgPriceRepository;
        _validator = validator;
    }

    public async Task<Result<CreateProfileResult>> HandleAsync(
        CreateProfileCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateProfileResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Parse currency
        FiatCurrency fiatCurrency;
        try
        {
            fiatCurrency = FiatCurrency.GetFromCode(command.CurrencyCode);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateProfileResult>.Failure(
                "INVALID_CURRENCY", $"Invalid currency code: {command.CurrencyCode}");
        }

        var icon = string.IsNullOrWhiteSpace(command.IconName)
            ? Icon.Empty
            : new Icon("", command.IconName, command.IconUnicode, Color.FromArgb(command.IconColor));

        var profile = AvgPriceProfile.New(
            AvgPriceProfileName.New(command.Name),
            new AvgPriceAsset(command.AssetName, command.Precision),
            command.Visible,
            icon,
            fiatCurrency,
            (AvgPriceCalculationMethod)command.CalculationMethodId);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        return Result<CreateProfileResult>.Success(new CreateProfileResult(profile.Id.Value));
    }
}
