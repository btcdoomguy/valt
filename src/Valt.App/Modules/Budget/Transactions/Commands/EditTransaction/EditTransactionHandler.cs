using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;

internal sealed class EditTransactionHandler : ICommandHandler<EditTransactionCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly IValidator<EditTransactionCommand> _validator;

    public EditTransactionHandler(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IFixedExpenseRepository fixedExpenseRepository,
        IValidator<EditTransactionCommand> validator)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _fixedExpenseRepository = fixedExpenseRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(EditTransactionCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailure(validation.Errors);
        }

        // Get existing transaction
        var transactionId = new TransactionId(command.TransactionId);
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
        if (transaction is null)
        {
            return Result<Unit>.NotFound("Transaction", command.TransactionId);
        }

        var categoryId = new CategoryId(command.CategoryId);

        // Run independent existence checks in parallel
        var categoryTask = _categoryRepository.GetCategoryByIdAsync(categoryId);
        var accountsTask = TransactionCommandHelper.ValidateAccountsAsync<Unit>(_accountRepository, command.Details);
        var fixedExpenseTask = string.IsNullOrEmpty(command.FixedExpenseId)
            ? Task.FromResult<FixedExpense?>(null)
            : _fixedExpenseRepository.GetFixedExpenseByIdAsync(new FixedExpenseId(command.FixedExpenseId));

        await Task.WhenAll(categoryTask, accountsTask, fixedExpenseTask);

        var category = await categoryTask;
        if (category is null)
        {
            return Result<Unit>.NotFound("Category", command.CategoryId);
        }

        var accountValidation = await accountsTask;
        if (accountValidation is not null)
        {
            return accountValidation;
        }

        TransactionFixedExpenseReference? fixedExpenseReference = null;
        var fixedExpense = await fixedExpenseTask;
        if (!string.IsNullOrEmpty(command.FixedExpenseId))
        {
            if (fixedExpense is null)
            {
                return Result<Unit>.NotFound("FixedExpense", command.FixedExpenseId);
            }
            fixedExpenseReference = new TransactionFixedExpenseReference(
                new FixedExpenseId(command.FixedExpenseId),
                command.FixedExpenseReferenceDate!.Value);
        }

        // Build transaction details
        var transactionDetails = TransactionCommandHelper.BuildTransactionDetails(command.Details);
        var name = TransactionName.New(command.Name);
        var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes;

        // Apply changes
        transaction.ChangeDate(command.Date);
        transaction.ChangeNameAndCategory(name, categoryId);
        transaction.SetFixedExpense(fixedExpenseReference);
        transaction.ChangeTransactionDetails(transactionDetails);
        transaction.ChangeNotes(notes);
        transaction.SetExcluded(command.IsExcluded);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result.Success();
    }
}
