using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;

internal sealed class EditFixedExpenseHandler : ICommandHandler<EditFixedExpenseCommand, EditFixedExpenseResult>
{
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IValidator<EditFixedExpenseCommand> _validator;

    public EditFixedExpenseHandler(
        IFixedExpenseRepository fixedExpenseRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IValidator<EditFixedExpenseCommand> validator)
    {
        _fixedExpenseRepository = fixedExpenseRepository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _validator = validator;
    }

    public async Task<Result<EditFixedExpenseResult>> HandleAsync(
        EditFixedExpenseCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<EditFixedExpenseResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Get existing fixed expense
        FixedExpense? fixedExpense;
        try
        {
            fixedExpense = await _fixedExpenseRepository.GetFixedExpenseByIdAsync(
                new FixedExpenseId(command.FixedExpenseId));
        }
        catch (Exception) when (true)
        {
            // Repository has a bug where it throws an exception when entity is not found
            fixedExpense = null;
        }

        if (fixedExpense is null)
            return Result<EditFixedExpenseResult>.Failure(
                "FIXED_EXPENSE_NOT_FOUND", $"Fixed expense with id {command.FixedExpenseId} not found");

        // Validate category exists
        var category = await _categoryRepository.GetCategoryByIdAsync(new CategoryId(command.CategoryId));
        if (category is null)
            return Result<EditFixedExpenseResult>.Failure(
                "CATEGORY_NOT_FOUND", $"Category with id {command.CategoryId} not found");

        // Rename if changed
        fixedExpense.Rename(FixedExpenseName.New(command.Name));

        // Set category if changed
        fixedExpense.SetCategory(category.Id);

        // Set enabled status
        fixedExpense.SetEnabled(command.Enabled);

        // Handle account/currency (mutually exclusive)
        if (!string.IsNullOrWhiteSpace(command.DefaultAccountId))
        {
            var account = await _accountRepository.GetAccountByIdAsync(new AccountId(command.DefaultAccountId));
            if (account is null)
                return Result<EditFixedExpenseResult>.Failure(
                    "ACCOUNT_NOT_FOUND", $"Account with id {command.DefaultAccountId} not found");
            fixedExpense.SetDefaultAccountId(account.Id);
        }
        else if (!string.IsNullOrWhiteSpace(command.Currency))
        {
            FiatCurrency fiatCurrency;
            try
            {
                fiatCurrency = FiatCurrency.GetFromCode(command.Currency);
            }
            catch (InvalidCurrencyCodeException)
            {
                return Result<EditFixedExpenseResult>.Failure(
                    "INVALID_CURRENCY", $"Invalid currency code: {command.Currency}");
            }
            fixedExpense.SetCurrency(fiatCurrency);
        }

        // Add new range if provided
        if (command.NewRange is not null)
        {
            var range = BuildRange(command.NewRange);
            if (range is null)
                return Result<EditFixedExpenseResult>.Failure("INVALID_RANGE", "Invalid range configuration");

            try
            {
                fixedExpense.AddRange(range);
            }
            catch (Core.Modules.Budget.FixedExpenses.Exceptions.InvalidFixedExpenseRangeException ex)
            {
                return Result<EditFixedExpenseResult>.Failure(
                    "INVALID_RANGE_DATE", $"Range start date must be after {ex.MinimumDate}");
            }
        }

        await _fixedExpenseRepository.SaveFixedExpenseAsync(fixedExpense);

        return Result<EditFixedExpenseResult>.Success(new EditFixedExpenseResult());
    }

    private static FixedExpenseRange? BuildRange(FixedExpenseRangeInputDTO dto)
    {
        var period = (FixedExpensePeriods)dto.PeriodId;

        if (dto.FixedAmount.HasValue)
        {
            var amount = FiatValue.New(dto.FixedAmount.Value);
            return FixedExpenseRange.CreateFixedAmount(amount, period, dto.PeriodStart, dto.Day);
        }

        if (dto.RangedAmountMin.HasValue && dto.RangedAmountMax.HasValue)
        {
            var min = FiatValue.New(dto.RangedAmountMin.Value);
            var max = FiatValue.New(dto.RangedAmountMax.Value);
            var rangedAmount = new RangedFiatValue(min, max);
            return FixedExpenseRange.CreateRangedAmount(rangedAmount, period, dto.PeriodStart, dto.Day);
        }

        return null;
    }
}
