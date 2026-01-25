using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.AvgPrice.Commands.AddLine;

public class AddLineValidator : IValidator<AddLineCommand>
{
    public ValidationResult Validate(AddLineCommand instance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(instance.ProfileId))
            errors.Add(nameof(instance.ProfileId), ["Profile ID is required"]);

        if (instance.LineTypeId < 0 || instance.LineTypeId > 2)
            errors.Add(nameof(instance.LineTypeId), ["Invalid line type"]);

        if (instance.Quantity <= 0)
            errors.Add(nameof(instance.Quantity), ["Quantity must be greater than zero"]);

        if (instance.Amount < 0)
            errors.Add(nameof(instance.Amount), ["Amount cannot be negative"]);

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
