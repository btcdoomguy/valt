using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.App.Modules.Budget.Categories.Commands.DeleteCategory;

internal sealed class DeleteCategoryHandler : ICommandHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionQueries _transactionQueries;

    public DeleteCategoryHandler(
        ICategoryRepository categoryRepository,
        ITransactionQueries transactionQueries)
    {
        _categoryRepository = categoryRepository;
        _transactionQueries = transactionQueries;
    }

    public async Task<Result<Unit>> HandleAsync(DeleteCategoryCommand command, CancellationToken ct = default)
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

        // Check if category is used by any transaction
        var transactionsWithCategory = await _transactionQueries.GetTransactionsAsync(new TransactionQueryFilter
        {
            Categories = [command.CategoryId]
        });

        if (transactionsWithCategory.Items.Count > 0)
        {
            return Result<Unit>.Failure(
                "CATEGORY_IN_USE",
                "Cannot delete category because it is used by one or more transactions.");
        }

        await _categoryRepository.DeleteCategoryAsync(categoryId);

        return Result.Success();
    }
}
