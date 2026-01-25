using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using InfraDTO = Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseHistory;

internal sealed class GetFixedExpenseHistoryHandler : IQueryHandler<GetFixedExpenseHistoryQuery, FixedExpenseHistoryDTO?>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseHistoryHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public async Task<FixedExpenseHistoryDTO?> HandleAsync(GetFixedExpenseHistoryQuery query, CancellationToken ct = default)
    {
        var infraResult = await _fixedExpenseQueries.GetFixedExpenseHistoryAsync(new FixedExpenseId(query.FixedExpenseId));

        return infraResult is null ? null : MapToAppDto(infraResult);
    }

    private static FixedExpenseHistoryDTO MapToAppDto(InfraDTO.FixedExpenseHistoryDto infra)
    {
        return new FixedExpenseHistoryDTO
        {
            FixedExpenseId = infra.FixedExpenseId,
            FixedExpenseName = infra.FixedExpenseName,
            Transactions = infra.Transactions.Select(t => new TransactionHistoryItemDTO
            {
                TransactionId = t.TransactionId,
                Date = t.Date,
                Name = t.Name,
                Amount = t.Amount,
                CategoryName = t.CategoryName,
                CategoryIcon = t.CategoryIcon,
                AccountName = t.AccountName,
                AccountIcon = t.AccountIcon,
                ReferenceDate = t.ReferenceDate
            }).ToList(),
            PriceHistory = infra.PriceHistory.Select(p => new PriceHistoryItemDTO
            {
                PeriodStart = p.PeriodStart,
                Amount = p.Amount,
                Period = p.Period,
                Day = p.Day
            }).ToList()
        };
    }
}
