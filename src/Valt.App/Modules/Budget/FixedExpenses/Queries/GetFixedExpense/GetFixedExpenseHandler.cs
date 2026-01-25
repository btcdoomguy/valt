using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using InfraDTO = Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;

internal sealed class GetFixedExpenseHandler : IQueryHandler<GetFixedExpenseQuery, FixedExpenseDTO?>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public async Task<FixedExpenseDTO?> HandleAsync(GetFixedExpenseQuery query, CancellationToken ct = default)
    {
        var infraResult = await _fixedExpenseQueries.GetFixedExpenseAsync(new FixedExpenseId(query.FixedExpenseId));

        return infraResult is null ? null : MapToAppDto(infraResult);
    }

    private static FixedExpenseDTO MapToAppDto(InfraDTO.FixedExpenseDto infra)
    {
        return new FixedExpenseDTO
        {
            Id = infra.Id,
            Name = infra.Name,
            CategoryId = infra.CategoryId,
            CategoryName = infra.CategoryName,
            CategoryIcon = infra.CategoryIcon,
            DefaultAccountId = infra.DefaultAccountId,
            DefaultAccountName = infra.DefaultAccountName,
            DefaultAccountIcon = infra.DefaultAccountIcon,
            Currency = infra.Currency,
            DisplayCurrency = infra.DisplayCurrency,
            Enabled = infra.Enabled,
            Ranges = infra.Ranges.Select(r => new FixedExpenseRangeDTO
            {
                PeriodStart = r.PeriodStart,
                FixedAmount = r.FixedAmount,
                FixedAmountFormatted = r.FixedAmountFormatted,
                RangedAmountMin = r.RangedAmountMin,
                RangedAmountMinFormatted = r.RangedAmountMinFormatted,
                RangedAmountMax = r.RangedAmountMax,
                RangedAmountMaxFormatted = r.RangedAmountMaxFormatted,
                PeriodId = r.PeriodId,
                PeriodDescription = r.PeriodDescription,
                Day = r.Day
            }).ToList()
        };
    }
}
