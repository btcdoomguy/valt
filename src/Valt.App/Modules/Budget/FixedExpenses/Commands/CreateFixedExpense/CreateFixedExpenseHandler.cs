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

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;

internal sealed class CreateFixedExpenseHandler : ICommandHandler<CreateFixedExpenseCommand, CreateFixedExpenseResult>
{
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IValidator<CreateFixedExpenseCommand> _validator;

    public CreateFixedExpenseHandler(
        IFixedExpenseRepository fixedExpenseRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IValidator<CreateFixedExpenseCommand> validator)
    {
        _fixedExpenseRepository = fixedExpenseRepository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _validator = validator;
    }

    public async Task<Result<CreateFixedExpenseResult>> HandleAsync(
        CreateFixedExpenseCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateFixedExpenseResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Validate category exists
        var category = await _categoryRepository.GetCategoryByIdAsync(new CategoryId(command.CategoryId));
        if (category is null)
            return Result<CreateFixedExpenseResult>.Failure("CATEGORY_NOT_FOUND", $"Category with id {command.CategoryId} not found");

        // Validate account exists if provided
        AccountId? accountId = null;
        if (!string.IsNullOrWhiteSpace(command.DefaultAccountId))
        {
            var account = await _accountRepository.GetAccountByIdAsync(new AccountId(command.DefaultAccountId));
            if (account is null)
                return Result<CreateFixedExpenseResult>.Failure("ACCOUNT_NOT_FOUND", $"Account with id {command.DefaultAccountId} not found");
            accountId = account.Id;
        }

        // Parse currency if provided
        FiatCurrency? fiatCurrency = null;
        if (!string.IsNullOrWhiteSpace(command.Currency))
        {
            try
            {
                fiatCurrency = FiatCurrency.GetFromCode(command.Currency);
            }
            catch (InvalidCurrencyCodeException)
            {
                return Result<CreateFixedExpenseResult>.Failure("INVALID_CURRENCY", $"Invalid currency code: {command.Currency}");
            }
        }

        // Build ranges
        var ranges = new List<FixedExpenseRange>();
        foreach (var rangeDto in command.Ranges)
        {
            var range = BuildRange(rangeDto);
            if (range is null)
                return Result<CreateFixedExpenseResult>.Failure("INVALID_RANGE", "Invalid range configuration");
            ranges.Add(range);
        }

        // Create fixed expense
        var fixedExpense = FixedExpense.New(
            FixedExpenseName.New(command.Name),
            accountId,
            category.Id,
            fiatCurrency,
            ranges,
            command.Enabled);

        await _fixedExpenseRepository.SaveFixedExpenseAsync(fixedExpense);

        return Result<CreateFixedExpenseResult>.Success(new CreateFixedExpenseResult(fixedExpense.Id.Value));
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
