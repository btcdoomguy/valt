using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkEditTransactions;

internal sealed class BulkEditTransactionsHandler : ICommandHandler<BulkEditTransactionsCommand, BulkEditTransactionsResult>
{
    private const int MaxNameLength = 60;

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BulkEditTransactionsHandler(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<BulkEditTransactionsResult>> HandleAsync(BulkEditTransactionsCommand command, CancellationToken ct = default)
    {
        if (command.TransactionIds is null || command.TransactionIds.Length == 0)
        {
            return Result<BulkEditTransactionsResult>.Failure("VALIDATION_FAILED",
                "At least one transaction ID is required.");
        }

        var hasNewName = !string.IsNullOrWhiteSpace(command.NewName);
        var hasNewCategory = !string.IsNullOrWhiteSpace(command.NewCategoryId);

        if (!hasNewName && !hasNewCategory)
        {
            return Result<BulkEditTransactionsResult>.Failure("VALIDATION_FAILED",
                "A new name or a new category is required.");
        }

        CategoryId? newCategoryId = null;
        if (hasNewCategory)
        {
            newCategoryId = new CategoryId(command.NewCategoryId!);
            var category = await _categoryRepository.GetCategoryByIdAsync(newCategoryId);
            if (category is null)
            {
                return Result<BulkEditTransactionsResult>.NotFound("Category", command.NewCategoryId!);
            }
        }

        TransactionName? newName = null;
        if (hasNewName)
        {
            if (command.NewName!.Length > MaxNameLength)
            {
                return Result<BulkEditTransactionsResult>.Failure("VALIDATION_FAILED",
                    $"Transaction name cannot exceed {MaxNameLength} characters.");
            }

            newName = TransactionName.New(command.NewName);
        }

        var transactionIds = command.TransactionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => new TransactionId(id))
            .ToList();

        var transactions = await _transactionRepository.GetTransactionsByIdsAsync(transactionIds);
        var updatedCount = 0;

        foreach (var transaction in transactions)
        {
            if (newName is not null && newCategoryId is not null)
            {
                transaction.ChangeNameAndCategory(newName, newCategoryId);
            }
            else if (newName is not null)
            {
                transaction.Rename(newName);
            }
            else if (newCategoryId is not null)
            {
                transaction.ChangeCategory(newCategoryId);
            }

            await _transactionRepository.SaveTransactionAsync(transaction);
            updatedCount++;
        }

        return Result<BulkEditTransactionsResult>.Success(new BulkEditTransactionsResult(updatedCount));
    }
}
