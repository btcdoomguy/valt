using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;

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
            return Result<Unit>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
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
        var accountsTask = ValidateAccountsAsync(command.Details);
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
        var transactionDetails = BuildTransactionDetails(command.Details);
        var name = TransactionName.New(command.Name);
        var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes;

        // Apply changes
        transaction.ChangeDate(command.Date);
        transaction.ChangeNameAndCategory(name, categoryId);
        transaction.SetFixedExpense(fixedExpenseReference);
        transaction.ChangeTransactionDetails(transactionDetails);
        transaction.ChangeNotes(notes);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result.Success();
    }

    private async Task<Result<Unit>?> ValidateAccountsAsync(TransactionDetailsDto details)
    {
        var fromAccountId = new AccountId(details.FromAccountId);

        string? toAccountIdString = details switch
        {
            FiatToFiatTransferDto f => f.ToAccountId,
            BitcoinToBitcoinTransferDto b => b.ToAccountId,
            FiatToBitcoinTransferDto fb => fb.ToAccountId,
            BitcoinToFiatTransferDto bf => bf.ToAccountId,
            _ => null
        };

        var fromAccountTask = _accountRepository.GetAccountByIdAsync(fromAccountId);
        Task<Account?>? toAccountTask = toAccountIdString is not null
            ? _accountRepository.GetAccountByIdAsync(new AccountId(toAccountIdString))
            : null;

        var tasks = new List<Task> { fromAccountTask };
        if (toAccountTask is not null)
            tasks.Add(toAccountTask);

        await Task.WhenAll(tasks);

        if (await fromAccountTask is null)
        {
            return Result<Unit>.NotFound("Account", details.FromAccountId);
        }

        if (toAccountIdString is not null && toAccountTask is not null && await toAccountTask is null)
        {
            return Result<Unit>.NotFound("Account", toAccountIdString);
        }

        return null;
    }

    private static TransactionDetails BuildTransactionDetails(TransactionDetailsDto dto)
    {
        return dto switch
        {
            FiatTransactionDto fiat => new FiatDetails(
                new AccountId(fiat.FromAccountId),
                FiatValue.New(fiat.Amount),
                fiat.IsCredit),

            BitcoinTransactionDto btc => new BitcoinDetails(
                new AccountId(btc.FromAccountId),
                (BtcValue)btc.AmountSats,
                btc.IsCredit),

            FiatToFiatTransferDto fiatToFiat => new FiatToFiatDetails(
                new AccountId(fiatToFiat.FromAccountId),
                new AccountId(fiatToFiat.ToAccountId),
                FiatValue.New(fiatToFiat.FromAmount),
                FiatValue.New(fiatToFiat.ToAmount)),

            BitcoinToBitcoinTransferDto btcToBtc => new BitcoinToBitcoinDetails(
                new AccountId(btcToBtc.FromAccountId),
                new AccountId(btcToBtc.ToAccountId),
                (BtcValue)btcToBtc.AmountSats),

            FiatToBitcoinTransferDto fiatToBtc => new FiatToBitcoinDetails(
                new AccountId(fiatToBtc.FromAccountId),
                new AccountId(fiatToBtc.ToAccountId),
                FiatValue.New(fiatToBtc.FromFiatAmount),
                (BtcValue)fiatToBtc.ToSatsAmount),

            BitcoinToFiatTransferDto btcToFiat => new BitcoinToFiatDetails(
                new AccountId(btcToFiat.FromAccountId),
                new AccountId(btcToFiat.ToAccountId),
                (BtcValue)btcToFiat.FromSatsAmount,
                FiatValue.New(btcToFiat.ToFiatAmount)),

            _ => throw new ArgumentException($"Unknown transaction details type: {dto.GetType().Name}")
        };
    }
}
