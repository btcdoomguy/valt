using LiteDB;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.FixedExpenses.Queries;

public class FixedExpenseQueries(ILocalDatabase localDatabase) : IFixedExpenseQueries
{
    public Task<FixedExpenseDto?> GetFixedExpenseAsync(FixedExpenseId id)
    {
        var fixedExpense = localDatabase.GetFixedExpenses().FindById(new ObjectId(id));

        if (fixedExpense is null)
            return Task.FromResult<FixedExpenseDto?>(null);
        
        var account = fixedExpense!.DefaultAccountId is not null ? localDatabase.GetAccounts().FindById(fixedExpense?.DefaultAccountId) : null;
        var category = localDatabase.GetCategories().FindById(fixedExpense?.CategoryId);

        var dto = ConvertToDto(fixedExpense!, account, category);
        
        return Task.FromResult(dto)!;
    }

    public Task<IEnumerable<FixedExpenseDto>> GetFixedExpensesAsync()
    {
        var data = localDatabase.GetFixedExpenses().FindAll().ToList();

        var allAccounts = localDatabase.GetAccounts().FindAll().ToList();
        var allCategories = localDatabase.GetCategories().FindAll().ToList();

        var dto = data.Select(entity => ConvertToDto(entity,
            allAccounts.SingleOrDefault(x => x.Id == entity.DefaultAccountId),
            allCategories.SingleOrDefault(x => x.Id == entity.CategoryId)!));

        return Task.FromResult(dto);
    }

    public Task<FixedExpenseNamesDTO> GetFixedExpenseNamesAsync(FixedExpenseId? current = null)
    {
        var data = localDatabase.GetFixedExpenses().FindAll().ToList();

        data = data.Where(x => x.Enabled || (current is not null && x.Id == new ObjectId(current.Value))).ToList();

        var dto = new FixedExpenseNamesDTO(data
            .Select(fixedExpense => new FixedExpenseNameDTO(fixedExpense.Id.ToString(), fixedExpense.Name)).ToList());
        
        return Task.FromResult(dto);
    }

    private static FixedExpenseDto ConvertToDto(FixedExpenseEntity fixedExpense, AccountEntity? account,
        CategoryEntity category)
    {
        var displayCurrency = fixedExpense.Currency ??
                              account?.Currency;

        return new FixedExpenseDto
        {
            Id = fixedExpense.Id.ToString(),
            Name = fixedExpense.Name,
            Currency = fixedExpense.Currency,
            DisplayCurrency = displayCurrency!,
            CategoryId = fixedExpense.CategoryId?.ToString(),
            DefaultAccountId = fixedExpense.DefaultAccountId?.ToString(),
            Enabled = fixedExpense.Enabled,
            Ranges = fixedExpense.Ranges.Select(range => new FixedExpenseDto.RangeDto()
            {
                Day = range.Day,
                PeriodId = range.PeriodId,
                FixedAmount = range.FixedAmount,
                FixedAmountFormatted = range.FixedAmount is not null
                    ? CurrencyDisplay.FormatFiat(range.FixedAmount.Value, displayCurrency!)
                    : string.Empty,
                RangedAmountMin = range.RangedAmountMin,
                RangedAmountMinFormatted = range.RangedAmountMin is not null
                    ? CurrencyDisplay.FormatFiat(range.RangedAmountMin.Value, displayCurrency!)
                    : string.Empty,
                RangedAmountMax = range.RangedAmountMax,
                RangedAmountMaxFormatted = range.RangedAmountMax is not null
                    ? CurrencyDisplay.FormatFiat(range.RangedAmountMax.Value, displayCurrency!)
                    : string.Empty,
                PeriodDescription = range.Period.ToString(),
                PeriodStart = DateOnly.FromDateTime(range.PeriodStart)
            }).OrderBy(x => x.PeriodStart).ToHashSet(),
            CategoryName = category?.Name,
            DefaultAccountName = account?.Name,
        };
    }
}