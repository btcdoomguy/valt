using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.Transactions.Queries;

public class TransactionQueries : ITransactionQueries
{
    private readonly ILocalDatabase _localDatabase;

    public TransactionQueries(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<TransactionsDTO> GetTransactionsAsync(TransactionQueryFilter filter)
    {
        // Load accounts and categories once at the start - reuse throughout the method
        var allAccountsList = _localDatabase.GetAccounts().FindAll().ToList();
        var allCategoriesList = _localDatabase.GetCategories().FindAll().ToList();
        var allCategories = ConvertToCategoriesDTO(allCategoriesList);

        var query = _localDatabase.GetTransactions().Query();

        if (filter.From is not null)
        {
            var parsedDate = filter.From.Value.ToValtDateTime();
            query = query.Where(x => x.Date >= parsedDate);
        }

        if (filter.To is not null)
        {
            var parsedDate = filter.To.Value.ToValtDateTime();
            query = query.Where(x => x.Date <= parsedDate);
        }

        if (filter.Accounts is not null)
        {
            var accountIds = filter.Accounts.Select(x => x.ToObjectId()).ToList();
            query = query.Where(x =>
                accountIds.Contains(x.FromAccountId) || (x.ToAccountId != null && accountIds.Contains(x.ToAccountId)));
        }

        if (filter.Categories is not null)
        {
            var categoryIds = filter.Categories.Select(x => x.ToObjectId()).ToList();
            query = query.Where(x => categoryIds.Contains(x.CategoryId));
        }

        // Apply search filter after loading, to support searching by name, category, and account
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm;

            // Find categories that match the search term (reuse already-loaded data)
            var matchingCategoryIds = allCategoriesList
                .Where(c => c.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                .Select(c => c.Id)
                .ToList();

            // Find accounts that match the search term (reuse already-loaded data)
            var matchingAccountIds = allAccountsList
                .Where(a => a.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                .Select(a => a.Id)
                .ToList();

            // Filter by name, category, or account
            query = query.Where(x =>
                x.Name.Contains(searchTerm) ||
                matchingCategoryIds.Contains(x.CategoryId) ||
                matchingAccountIds.Contains(x.FromAccountId) ||
                (x.ToAccountId != null && matchingAccountIds.Contains(x.ToAccountId)));
        }

        var result = query.ToList();

        // Batch load all FixedExpenseRecords for transactions to avoid N+1 queries
        var transactionIds = result.Select(t => t.Id).ToList();
        var fixedExpenseRecords = _localDatabase.GetFixedExpenseRecords()
            .Include(x => x.FixedExpense)
            .Find(x => x.Transaction != null && transactionIds.Contains(x.Transaction.Id))
            .ToDictionary(x => x.Transaction!.Id);

        var dtos = result.Select(transactionEntity =>
        {
            fixedExpenseRecords.TryGetValue(transactionEntity.Id, out var fixedExpenseRecord);

            var category = allCategories.Items.SingleOrDefault(x => x.Id == transactionEntity.CategoryId.ToString())!;
            var fromAccount = allAccountsList.SingleOrDefault(a => a.Id == transactionEntity.FromAccountId);
            var toAccount = transactionEntity.ToAccountId is not null
                ? allAccountsList.SingleOrDefault(a => a.Id == transactionEntity.ToAccountId)
                : null;
            var transferType = transactionEntity.Type.ToString();

            TransactionTypes transactionType;
            if (toAccount is not null)
                transactionType = TransactionTypes.Transfer;
            else if (transactionEntity.FromFiatAmount < 0 || transactionEntity.FromSatAmount < 0)
                transactionType = TransactionTypes.Debt;
            else
                transactionType = TransactionTypes.Credit;

            return new TransactionDTO()
            {
                Id = transactionEntity.Id.ToString(),
                Date = DateOnly.FromDateTime(transactionEntity.Date),
                Name = transactionEntity.Name,
                CategoryId = transactionEntity.CategoryId.ToString(),
                CategoryIcon = category.Icon,
                CategoryName = category.Name,
                FromAccountId = transactionEntity.FromAccountId.ToString(),
                FromAccountIcon = fromAccount?.Icon,
                FromAccountName = fromAccount?.Name ?? string.Empty,
                FromAccountCurrency = fromAccount?.Currency,
                ToAccountId = transactionEntity.ToAccountId?.ToString(),
                ToAccountIcon = toAccount?.Icon,
                ToAccountName = toAccount?.Name,
                ToAccountCurrency = toAccount?.Currency,
                FormattedFromAmount = FormatAmount(transactionEntity.FromSatAmount, transactionEntity.FromFiatAmount,
                    fromAccount?.Currency),
                FromAmountFiat = transactionEntity.FromFiatAmount,
                FromAmountSats = transactionEntity.FromSatAmount,
                FormattedToAmount = FormatAmount(transactionEntity.ToSatAmount, transactionEntity.ToFiatAmount,
                    toAccount?.Currency),
                ToAmountFiat = transactionEntity.ToFiatAmount,
                ToAmountSats = transactionEntity.ToSatAmount,
                TransferType = transferType,
                TransactionType = transactionType.ToString(),
                AutoSatAmount = transactionEntity.SatAmount,
                AutoSatAmountSummary = transactionEntity.SatAmount?.ToString() ?? string.Empty,
                FixedExpenseRecordId = fixedExpenseRecord?.Id.ToString(),
                FixedExpenseId = fixedExpenseRecord?.FixedExpense.Id.ToString(),
                FixedExpenseName = fixedExpenseRecord?.FixedExpense.Name,
                FixedExpenseReferenceDate = fixedExpenseRecord is not null
                    ? DateOnly.FromDateTime(fixedExpenseRecord.ReferenceDate)
                    : null,
                Notes = transactionEntity.Notes
            };
        });

        return Task.FromResult(new TransactionsDTO(dtos.ToList()));
    }

    public Task<IReadOnlyList<TransactionNameSearchDTO>> GetTransactionNamesAsync(string searchTerm)
    {
        var categories = _localDatabase.GetCategories().FindAll().ToList();
        var matches = _localDatabase.GetTransactionTerms()
            .Find(x => x.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            .OrderByDescending(x => x.Count)
            .Take(5);

        return Task.FromResult<IReadOnlyList<TransactionNameSearchDTO>>(matches.Select(x =>
            new TransactionNameSearchDTO()
            {
                CategoryId = x.CategoryId.ToString(),
                Count = x.Count,
                Name = x.Name,
                CategoryName = categories.Single(y => y.Id == x.CategoryId)?.Name ?? string.Empty,
                IsBitcoin = x.SatAmount is not null,
                SatAmount = x.SatAmount,
                FiatAmount = x.FiatAmount
            }).ToList());
    }

    private static string? FormatAmount(long? satAmount, decimal? fiatAmount, string? currency)
    {
        if (satAmount is not null)
            return CurrencyDisplay.FormatSatsAsBitcoin(satAmount.Value);

        if (fiatAmount is not null && currency is not null)
            return CurrencyDisplay.FormatFiat(fiatAmount.Value, currency);

        return null;
    }

    private static CategoriesDTO ConvertToCategoriesDTO(List<Categories.CategoryEntity> data)
    {
        return new CategoriesDTO(data.Select(category =>
        {
            var icon = category.Icon != null ? Icon.RestoreFromId(category.Icon) : Icon.Empty;

            var name = category.Name;
            if (category.ParentId is not null)
            {
                var parent = data.SingleOrDefault(x => x.Id == category.ParentId);
                if (parent is not null)
                    name = $"{parent.Name} > {name}";
            }

            return new CategoryDTO()
            {
                Id = category.Id.ToString(),
                Name = name,
                Icon = category.Icon,
                Unicode = icon.Unicode,
                Color = icon.Color
            };
        }).ToList());
    }
}