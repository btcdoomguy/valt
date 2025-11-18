using Valt.Core.Common;

namespace Valt.Core.Modules.Budget.FixedExpenses;

public record FixedExpenseRange
{
    public FiatValue? FixedAmount { get; }
    public RangedFiatValue? RangedAmount { get; }
    public FixedExpensePeriods Period { get; }
    public DateOnly PeriodStart { get; }
    public int Day { get; }
    public DayOfWeek? DayOfWeek => Period is FixedExpensePeriods.Weekly or FixedExpensePeriods.Biweekly ? (DayOfWeek?) Day : null; 

    private FixedExpenseRange(FiatValue? fixedAmount, RangedFiatValue? rangedAmount, FixedExpensePeriods period,
        DateOnly periodStart, int day)
    {
        if (period is FixedExpensePeriods.Monthly or FixedExpensePeriods.Yearly)
        {
            if (day < 1 || day > 31)
                throw new ArgumentException("Day must be between 1 and 31", nameof(day));
        }
        else
        {
            if (day < 0 || day > 6)
                throw new ArgumentException("DayOfWeek must be between 0 and 6", nameof(day));
        }

        FixedAmount = fixedAmount;
        RangedAmount = rangedAmount;
        Period = period;
        PeriodStart = periodStart;
        Day = day;
    }

    public static FixedExpenseRange CreateFixedAmount(FiatValue fixedAmount, FixedExpensePeriods period, DateOnly periodStart,
        int day) =>
        new(fixedAmount, null, period, periodStart, day);
    
    public static FixedExpenseRange CreateFixedAmount(FiatValue fixedAmount, FixedExpensePeriods period, DateOnly periodStart,
        DayOfWeek dayOfWeek) =>
        new(fixedAmount, null, period, periodStart, (int) dayOfWeek);
    
    public static FixedExpenseRange CreateRangedAmount(RangedFiatValue rangedAmount, FixedExpensePeriods period, DateOnly periodStart,
        int day) =>
        new(null, rangedAmount, period, periodStart, day);
    
    public static FixedExpenseRange CreateRangedAmount(RangedFiatValue rangedAmount, FixedExpensePeriods period, DateOnly periodStart,
        DayOfWeek dayOfWeek) =>
        new(null, rangedAmount, period, periodStart, (int) dayOfWeek);
}