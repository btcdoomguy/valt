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
            return Result<AddTransactionResult>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        // Verify category exists
        var categoryId = new CategoryId(command.CategoryId);
        var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
        if (category is null)
        {
            return Result<AddTransactionResult>.NotFound("Category", command.CategoryId);
        }

        // Verify accounts exist
        var accountValidation = await ValidateAccountsAsync(command.Details);
        if (accountValidation is not null)
        {
            return accountValidation;
        }

        // Verify fixed expense exists if specified
        TransactionFixedExpenseReference? fixedExpenseReference = null;
        if (!string.IsNullOrEmpty(command.FixedExpenseId))
        {
            var fixedExpenseId = new FixedExpenseId(command.FixedExpenseId);
            var fixedExpense = await _fixedExpenseRepository.GetFixedExpenseByIdAsync(fixedExpenseId);
            if (fixedExpense is null)
            {
                return Result<AddTransactionResult>.NotFound("FixedExpense", command.FixedExpenseId);
            }
            fixedExpenseReference = new TransactionFixedExpenseReference(
                fixedExpenseId,
                command.FixedExpenseReferenceDate!.Value);
        }

        // Build transaction details
        var transactionDetails = BuildTransactionDetails(command.Details);

        // Create transaction
        var name = TransactionName.New(command.Name);
        var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes;
        var transaction = Transaction.New(
            command.Date,
            name,
            categoryId,
            transactionDetails,
            notes,
            fixedExpenseReference);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result<AddTransactionResult>.Success(
            new AddTransactionResult(transaction.Id.Value, transaction.Date));
    }

    private async Task<Result<AddTransactionResult>?> ValidateAccountsAsync(TransactionDetailsDto details)
    {
        var fromAccountId = new AccountId(details.FromAccountId);
        var fromAccount = await _accountRepository.GetAccountByIdAsync(fromAccountId);
        if (fromAccount is null)
        {
            return Result<AddTransactionResult>.NotFound("Account", details.FromAccountId);
        }

        string? toAccountIdString = details switch
        {
            FiatToFiatTransferDto f => f.ToAccountId,
            BitcoinToBitcoinTransferDto b => b.ToAccountId,
            FiatToBitcoinTransferDto fb => fb.ToAccountId,
            BitcoinToFiatTransferDto bf => bf.ToAccountId,
            _ => null
        };

        if (toAccountIdString is not null)
        {
            var toAccountId = new AccountId(toAccountIdString);
            var toAccount = await _accountRepository.GetAccountByIdAsync(toAccountId);
            if (toAccount is null)
            {
                return Result<AddTransactionResult>.NotFound("Account", toAccountIdString);
            }
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
