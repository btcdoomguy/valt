using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands;

internal static class FixedExpenseRangeBuilder
{
    public static FixedExpenseRange? BuildRange(FixedExpenseRangeInputDTO dto)
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
