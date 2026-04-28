using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkChangeCategoryTransactions;

internal sealed class BulkChangeCategoryTransactionsHandler : ICommandHandler<BulkChangeCategoryTransactionsCommand, BulkChangeCategoryTransactionsResult>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BulkChangeCategoryTransactionsHandler(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<BulkChangeCategoryTransactionsResult>> HandleAsync(BulkChangeCategoryTransactionsCommand command, CancellationToken ct = default)
    {
        if (command.TransactionIds is null || command.TransactionIds.Length == 0)
        {
            return Result<BulkChangeCategoryTransactionsResult>.Failure("VALIDATION_FAILED",
                "At least one transaction ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewCategoryId))
        {
            return Result<BulkChangeCategoryTransactionsResult>.Failure("VALIDATION_FAILED",
                "New category ID is required.");
        }

        // Verify category exists
        var newCategoryId = new CategoryId(command.NewCategoryId);
        var category = await _categoryRepository.GetCategoryByIdAsync(newCategoryId);
        if (category is null)
        {
            return Result<BulkChangeCategoryTransactionsResult>.NotFound("Category", command.NewCategoryId);
        }

        var updatedCount = 0;

        // Batch-load all transactions in a single query to avoid N+1 round trips
        var transactionIds = command.TransactionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => new TransactionId(id))
            .ToList();

        var transactions = await _transactionRepository.GetTransactionsByIdsAsync(transactionIds);

        foreach (var transaction in transactions)
        {
            transaction.ChangeCategory(newCategoryId);
            await _transactionRepository.SaveTransactionAsync(transaction);
            updatedCount++;
        }

        return Result<BulkChangeCategoryTransactionsResult>.Success(
            new BulkChangeCategoryTransactionsResult(updatedCount));
    }
}
