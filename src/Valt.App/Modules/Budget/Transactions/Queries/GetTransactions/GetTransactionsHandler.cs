using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using InfraDTO = Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactions;

internal sealed class GetTransactionsHandler : IQueryHandler<GetTransactionsQuery, TransactionsDTO>
{
    private readonly ITransactionQueries _transactionQueries;

    public GetTransactionsHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public async Task<TransactionsDTO> HandleAsync(GetTransactionsQuery query, CancellationToken ct = default)
    {
        var filter = new InfraDTO.TransactionQueryFilter
        {
            Accounts = query.AccountIds?.Select(id => new AccountId(id)).ToArray(),
            Categories = query.CategoryIds?.Select(id => new CategoryId(id)).ToArray(),
            From = query.From,
            To = query.To,
            SearchTerm = query.SearchTerm
        };

        var infraResult = await _transactionQueries.GetTransactionsAsync(filter);

        var items = infraResult.Items.Select(MapToAppDto).ToList();

        return new TransactionsDTO(items);
    }

    private static TransactionDTO MapToAppDto(InfraDTO.TransactionDTO infra)
    {
        return new TransactionDTO
        {
            Id = infra.Id,
            Date = infra.Date,
            Name = infra.Name,
            CategoryId = infra.CategoryId,
            CategoryName = infra.CategoryName,
            CategoryIcon = infra.CategoryIcon,
            FromAccountId = infra.FromAccountId,
            FromAccountName = infra.FromAccountName,
            FromAccountIcon = infra.FromAccountIcon,
            FromAccountCurrency = infra.FromAccountCurrency,
            ToAccountId = infra.ToAccountId,
            ToAccountName = infra.ToAccountName,
            ToAccountIcon = infra.ToAccountIcon,
            ToAccountCurrency = infra.ToAccountCurrency,
            FormattedFromAmount = infra.FormattedFromAmount,
            FromAmountSats = infra.FromAmountSats,
            FromAmountFiat = infra.FromAmountFiat,
            FormattedToAmount = infra.FormattedToAmount,
            ToAmountSats = infra.ToAmountSats,
            ToAmountFiat = infra.ToAmountFiat,
            TransferType = infra.TransferType,
            TransactionType = infra.TransactionType,
            AutoSatAmount = infra.AutoSatAmount,
            AutoSatAmountSummary = infra.AutoSatAmountSummary,
            FixedExpenseRecordId = infra.FixedExpenseRecordId,
            FixedExpenseId = infra.FixedExpenseId,
            FixedExpenseName = infra.FixedExpenseName,
            FixedExpenseReferenceDate = infra.FixedExpenseReferenceDate,
            Notes = infra.Notes
        };
    }
}
