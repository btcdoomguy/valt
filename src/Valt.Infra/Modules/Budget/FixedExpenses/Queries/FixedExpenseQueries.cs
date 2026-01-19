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

    public Task<FixedExpenseHistoryDto?> GetFixedExpenseHistoryAsync(FixedExpenseId id)
    {
        var fixedExpenseObjectId = new ObjectId(id);
        var fixedExpense = localDatabase.GetFixedExpenses().FindById(fixedExpenseObjectId);

        if (fixedExpense is null)
            return Task.FromResult<FixedExpenseHistoryDto?>(null);

        // Load accounts and categories once - reuse throughout the method
        var allAccounts = localDatabase.GetAccounts().FindAll().ToList();
        var allCategories = localDatabase.GetCategories().FindAll().ToList();

        var account = fixedExpense.DefaultAccountId is not null
            ? allAccounts.SingleOrDefault(a => a.Id == fixedExpense.DefaultAccountId)
            : null;

        var displayCurrency = fixedExpense.Currency ?? account?.Currency ?? "USD";

        // Get all records with transactions for this fixed expense
        var records = localDatabase.GetFixedExpenseRecords()
            .Include(x => x.Transaction)
            .Find(x => x.FixedExpense.Id == fixedExpenseObjectId && x.Transaction != null)
            .ToList();

        var transactionItems = records
            .OrderByDescending(x => x.Transaction!.Date)
            .Select(record =>
            {
                var transaction = record.Transaction!;
                var transactionAccount = allAccounts.SingleOrDefault(a => a.Id == transaction.FromAccountId);
                var category = allCategories.SingleOrDefault(c => c.Id == transaction.CategoryId);

                var amount = transaction.FromFiatAmount is not null
                    ? CurrencyDisplay.FormatFiat(transaction.FromFiatAmount.Value, transactionAccount?.Currency ?? displayCurrency)
                    : transaction.FromSatAmount is not null
                        ? CurrencyDisplay.FormatSatsAsBitcoin(transaction.FromSatAmount.Value)
                        : string.Empty;

                return new TransactionHistoryItemDto
                {
                    TransactionId = transaction.Id.ToString(),
                    Date = DateOnly.FromDateTime(transaction.Date),
                    Name = transaction.Name,
                    Amount = amount,
                    CategoryName = category?.Name ?? string.Empty,
                    CategoryIcon = category?.Icon,
                    AccountName = transactionAccount?.Name ?? string.Empty,
                    AccountIcon = transactionAccount?.Icon,
                    ReferenceDate = DateOnly.FromDateTime(record.ReferenceDate)
                };
            })
            .ToList();

        var priceHistoryItems = fixedExpense.Ranges
            .OrderByDescending(x => x.PeriodStart)
            .Select(range =>
            {
                var amount = range.FixedAmount is not null
                    ? CurrencyDisplay.FormatFiat(range.FixedAmount.Value, displayCurrency)
                    : $"{CurrencyDisplay.FormatFiat(range.RangedAmountMin!.Value, displayCurrency)} - {CurrencyDisplay.FormatFiat(range.RangedAmountMax!.Value, displayCurrency)}";

                return new PriceHistoryItemDto
                {
                    PeriodStart = DateOnly.FromDateTime(range.PeriodStart),
                    Amount = amount,
                    Period = range.Period.ToString(),
                    Day = range.Day
                };
            })
            .ToList();

        var dto = new FixedExpenseHistoryDto
        {
            FixedExpenseId = fixedExpense.Id.ToString(),
            FixedExpenseName = fixedExpense.Name,
            Transactions = transactionItems,
            PriceHistory = priceHistoryItems
        };

        return Task.FromResult<FixedExpenseHistoryDto?>(dto);
    }

    private static FixedExpenseDto ConvertToDto(FixedExpenseEntity fixedExpense, AccountEntity? account,
        CategoryEntity? category)
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
            CategoryIcon = category?.Icon,
            DefaultAccountName = account?.Name,
            DefaultAccountIcon = account?.Icon,
        };
    }
}