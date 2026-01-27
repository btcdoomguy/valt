using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryValidator : IValidator<CreateCategoryCommand>
{
    private const int MaxNameLength = 50;

    public ValidationResult Validate(CreateCategoryCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Category name is required.");

        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > MaxNameLength)
        {
            builder.AddError(nameof(command.Name), $"Category name cannot exceed {MaxNameLength} characters.");
        }

        builder.AddErrorIfNullOrWhiteSpace(command.IconId, nameof(command.IconId), "Icon is required.");

        return builder.Build();
    }
}
