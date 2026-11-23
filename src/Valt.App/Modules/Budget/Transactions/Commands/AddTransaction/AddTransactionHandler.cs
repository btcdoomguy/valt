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

namespace Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;

internal sealed class AddTransactionHandler : ICommandHandler<AddTransactionCommand, AddTransactionResult>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly IValidator<AddTransactionCommand> _validator;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IFixedExpenseRepository fixedExpenseRepository,
        IValidator<AddTransactionCommand> validator)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _fixedExpenseRepository = fixedExpenseRepository;
        _validator = validator;
    }

    public async Task<Result<AddTransactionResult>> HandleAsync(AddTransactionCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<AddTransactionResult>.ValidationFailure(validation.Errors);
        }

        var categoryId = new CategoryId(command.CategoryId);

        // Run independent existence checks in parallel
        var categoryTask = _categoryRepository.GetCategoryByIdAsync(categoryId);
        var accountsTask = TransactionCommandHelper.ValidateAccountsAsync<AddTransactionResult>(_accountRepository, command.Details);
        var fixedExpenseTask = string.IsNullOrEmpty(command.FixedExpenseId)
            ? Task.FromResult<FixedExpense?>(null)
            : _fixedExpenseRepository.GetFixedExpenseByIdAsync(new FixedExpenseId(command.FixedExpenseId));

        await Task.WhenAll(categoryTask, accountsTask, fixedExpenseTask);

        var category = await categoryTask;
        if (category is null)
        {
            return Result<AddTransactionResult>.NotFound("Category", command.CategoryId);
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
                return Result<AddTransactionResult>.NotFound("FixedExpense", command.FixedExpenseId);
            }
            fixedExpenseReference = new TransactionFixedExpenseReference(
                new FixedExpenseId(command.FixedExpenseId),
                command.FixedExpenseReferenceDate!.Value);
        }

        // Build transaction details
        var transactionDetails = TransactionCommandHelper.BuildTransactionDetails(command.Details);

        // Create transaction
        var name = TransactionName.New(command.Name);
        var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes;
        var groupId = string.IsNullOrEmpty(command.GroupId)
            ? null
            : new GroupId(command.GroupId);

        var transaction = Transaction.New(
            command.Date,
            name,
            categoryId,
            transactionDetails,
            notes,
            fixedExpenseReference,
            groupId,
            command.IsExcluded);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result<AddTransactionResult>.Success(
            new AddTransactionResult(transaction.Id.Value, transaction.Date));
    }
}
