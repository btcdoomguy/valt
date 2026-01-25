using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;

namespace Valt.App.Modules.Budget.Categories.Commands.ChangeCategoryParent;

internal sealed class ChangeCategoryParentHandler : ICommandHandler<ChangeCategoryParentCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;

    public ChangeCategoryParentHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<Unit>> HandleAsync(ChangeCategoryParentCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.CategoryId))
        {
            return Result<Unit>.Failure("VALIDATION_FAILED", "Category ID is required.");
        }

        var categoryId = new CategoryId(command.CategoryId);

        // Verify category exists
        var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
        if (category is null)
        {
            return Result<Unit>.NotFound("Category", command.CategoryId);
        }

        CategoryId? newParentId = null;
        if (!string.IsNullOrEmpty(command.NewParentId))
        {
            newParentId = new CategoryId(command.NewParentId);

            // Verify new parent exists
            var parent = await _categoryRepository.GetCategoryByIdAsync(newParentId);
            if (parent is null)
            {
                return Result<Unit>.NotFound("Category", command.NewParentId);
            }

            // Ensure we don't create more than 2 levels
            if (parent.ParentId is not null)
            {
                return Result<Unit>.Failure(
                    "INVALID_PARENT",
                    "Cannot set parent to a category that already has a parent (max 2 levels).");
            }

            // Ensure category is not being set as its own parent
            if (command.CategoryId == command.NewParentId)
            {
                return Result<Unit>.Failure(
                    "INVALID_PARENT",
                    "A category cannot be its own parent.");
            }

            // Ensure category doesn't have children if moving under a parent
            var allCategories = await _categoryRepository.GetCategoriesAsync();
            var hasChildren = allCategories.Any(c => c.ParentId?.Value == command.CategoryId);
            if (hasChildren)
            {
                return Result<Unit>.Failure(
                    "INVALID_PARENT",
                    "Cannot move a category with children under another parent (max 2 levels).");
            }
        }

        category.ChangeParent(newParentId);
        await _categoryRepository.SaveCategoryAsync(category);

        return Result.Success();
    }
}
