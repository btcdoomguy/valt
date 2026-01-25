using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.Categories.Commands.EditCategory;

internal sealed class EditCategoryValidator : IValidator<EditCategoryCommand>
{
    private const int MaxNameLength = 50;

    public ValidationResult Validate(EditCategoryCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.CategoryId, nameof(command.CategoryId), "Category ID is required.");
        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Category name is required.");

        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > MaxNameLength)
        {
            builder.AddError(nameof(command.Name), $"Category name cannot exceed {MaxNameLength} characters.");
        }

        builder.AddErrorIfNullOrWhiteSpace(command.IconId, nameof(command.IconId), "Icon is required.");

        return builder.Build();
    }
}
