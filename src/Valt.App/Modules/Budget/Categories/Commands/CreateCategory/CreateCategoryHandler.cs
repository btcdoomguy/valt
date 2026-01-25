using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;

namespace Valt.App.Modules.Budget.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryHandler : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IValidator<CreateCategoryCommand> _validator;

    public CreateCategoryHandler(
        ICategoryRepository categoryRepository,
        IValidator<CreateCategoryCommand> validator)
    {
        _categoryRepository = categoryRepository;
        _validator = validator;
    }

    public async Task<Result<CreateCategoryResult>> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<CreateCategoryResult>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        // Validate parent exists if specified
        CategoryId? parentId = null;
        if (!string.IsNullOrEmpty(command.ParentId))
        {
            parentId = new CategoryId(command.ParentId);
            var parent = await _categoryRepository.GetCategoryByIdAsync(parentId);
            if (parent is null)
            {
                return Result<CreateCategoryResult>.NotFound("Category", command.ParentId);
            }

            // Ensure we don't create more than 2 levels
            if (parent.ParentId is not null)
            {
                return Result<CreateCategoryResult>.Failure(
                    "INVALID_PARENT",
                    "Cannot create a category with more than 2 levels of nesting.");
            }
        }

        var name = CategoryName.New(command.Name);
        var icon = Icon.RestoreFromId(command.IconId);

        var category = Category.New(name, icon, parentId);

        await _categoryRepository.SaveCategoryAsync(category);

        return Result<CreateCategoryResult>.Success(new CreateCategoryResult(category.Id.Value));
    }
}
