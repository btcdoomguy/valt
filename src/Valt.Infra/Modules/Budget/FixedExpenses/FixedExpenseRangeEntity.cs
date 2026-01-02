using LiteDB;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public class FixedExpenseRangeEntity
{
    [BsonField("fixed")] public decimal? FixedAmount { get; set; }
    [BsonField("rangeMin")] public decimal? RangedAmountMin { get; set; }
    [BsonField("rangeMax")] public decimal? RangedAmountMax { get; set; }
    [BsonField("period")] public int PeriodId { get; set; }
    [BsonField("periodDt")] public DateTime PeriodStart { get; set; }
    [BsonField("day")] public int Day { get; set; }
    [BsonIgnore]
    public FixedExpensePeriods Period
    {
        get => (FixedExpensePeriods)PeriodId;
        set => PeriodId = (int)value;
    }

    [BsonIgnore]
    public DayOfWeek? DayOfWeek => Period is FixedExpensePeriods.Weekly or FixedExpensePeriods.Biweekly ? (DayOfWeek?)Day : null;
}