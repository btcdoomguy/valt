using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.AvgPrice.Commands.CreateProfile;

public class CreateProfileValidator : IValidator<CreateProfileCommand>
{
    public ValidationResult Validate(CreateProfileCommand instance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(nameof(instance.Name), ["Name is required"]);

        if (string.IsNullOrWhiteSpace(instance.AssetName))
            errors.Add(nameof(instance.AssetName), ["Asset name is required"]);

        if (instance.Precision < 0 || instance.Precision > 8)
            errors.Add(nameof(instance.Precision), ["Precision must be between 0 and 8"]);

        if (string.IsNullOrWhiteSpace(instance.CurrencyCode))
            errors.Add(nameof(instance.CurrencyCode), ["Currency code is required"]);

        if (instance.CalculationMethodId < 0 || instance.CalculationMethodId > 1)
            errors.Add(nameof(instance.CalculationMethodId), ["Invalid calculation method"]);

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
