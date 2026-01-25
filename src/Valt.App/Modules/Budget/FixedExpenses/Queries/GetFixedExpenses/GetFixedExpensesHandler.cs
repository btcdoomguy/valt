using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using InfraDTO = Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenses;

internal sealed class GetFixedExpensesHandler : IQueryHandler<GetFixedExpensesQuery, IReadOnlyList<FixedExpenseDTO>>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpensesHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public async Task<IReadOnlyList<FixedExpenseDTO>> HandleAsync(GetFixedExpensesQuery query, CancellationToken ct = default)
    {
        var infraResult = await _fixedExpenseQueries.GetFixedExpensesAsync();

        return infraResult.Select(MapToAppDto).ToList();
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
