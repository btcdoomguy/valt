using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;

namespace Valt.App.Modules.Budget.Categories.Commands.EditCategory;

internal sealed class EditCategoryHandler : ICommandHandler<EditCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IValidator<EditCategoryCommand> _validator;

    public EditCategoryHandler(
        ICategoryRepository categoryRepository,
        IValidator<EditCategoryCommand> validator)
    {
        _categoryRepository = categoryRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(EditCategoryCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        var categoryId = new CategoryId(command.CategoryId);
        var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);

        if (category is null)
        {
            return Result<Unit>.NotFound("Category", command.CategoryId);
        }

        var name = CategoryName.New(command.Name);
        var icon = Icon.RestoreFromId(command.IconId);

        category.Rename(name);
        category.ChangeIcon(icon);

        await _categoryRepository.SaveCategoryAsync(category);

        return Result.Success();
    }
}
