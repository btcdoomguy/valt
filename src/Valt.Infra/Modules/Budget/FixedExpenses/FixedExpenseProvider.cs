using System.Diagnostics;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public class FixedExpenseProvider : IFixedExpenseProvider
{
    private readonly ILocalDatabase _localDatabase;

    public FixedExpenseProvider(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IEnumerable<FixedExpenseProviderEntry>> GetFixedExpensesOfMonthAsync(DateOnly date)
    {
        var minDate = new DateOnly(date.Year, date.Month, 1);
        var maxDate = minDate.AddMonths(1).AddDays(-1);

        var allAccounts = _localDatabase.GetAccounts().FindAll().ToDictionary(x => x.Id, x => x);

        var allFixedExpenses = _localDatabase.GetFixedExpenses().FindAll()
            .Where(x => x.Enabled)
            .ToList();

        var entries = new List<FixedExpenseProviderEntry>();
        var lastReferenceDate = minDate;
        foreach (var fixedExpense in allFixedExpenses)
        {
            var rangedEntities = GetFixedExpenseRangesFor(fixedExpense, minDate, maxDate);

            foreach (var rangedEntity in rangedEntities)
            {
                var daysToProcess = new List<int>();
                if (rangedEntity.Entity.Period is FixedExpensePeriods.Weekly or FixedExpensePeriods.Biweekly)
                {
                    var periodDates = GetDatesInPeriod(DateOnly.FromDateTime(rangedEntity.Entity.PeriodStart),
                        rangedEntity.Entity.DayOfWeek.GetValueOrDefault(DayOfWeek.Sunday), minDate, maxDate,
                        rangedEntity.Entity.Period is FixedExpensePeriods.Weekly
                            ? Frequency.Weekly
                            : Frequency.BiWeekly);

                    foreach (var periodDate in periodDates)
                    {
                        var day = periodDate.Day;
                        //checks if the ranged entry is valid for the period
                        if (day > maxDate.Day)
                            day = maxDate.Day;

                        daysToProcess.Add(day);
                    }
                }
                else
                {
                    //checks if the ranged entry is valid for the period
                    var day = rangedEntity.Entity.Day;
                    if (day > maxDate.Day)
                        day = maxDate.Day;

                    daysToProcess.Add(day);
                }

                foreach (var day in daysToProcess)
                {
                    var referenceDate = new DateOnly(date.Year, date.Month, day);

                    if (referenceDate < DateOnly.FromDateTime(rangedEntity.Entity.PeriodStart))
                        continue;

                    if (referenceDate > DateOnly.FromDateTime(rangedEntity.RangeEnd ?? DateTime.MaxValue))
                        continue;

                    if (rangedEntities.Any(x =>
                            x.Entity.PeriodStart != rangedEntity.Entity.PeriodStart &&
                            x.Entity.PeriodStart <= referenceDate.ToValtDateTime() &&
                            x.Entity.PeriodStart > lastReferenceDate.ToValtDateTime()))
                        continue;

                    if (rangedEntity.Entity.Period == FixedExpensePeriods.Yearly)
                    {
                        var fixedExpenseMonth = rangedEntity.Entity.PeriodStart.Month;

                        if (date.Month != fixedExpenseMonth)
                            continue;
                    }

                    lastReferenceDate = referenceDate;

                    entries.Add(new FixedExpenseProviderEntry(fixedExpense.Id.ToString(), fixedExpense.Name,
                        fixedExpense.CategoryId.ToString(), referenceDate, fixedExpense.DefaultAccountId?.ToString(),
                        rangedEntity.Entity.FixedAmount, rangedEntity.Entity.RangedAmountMin,
                        rangedEntity.Entity.RangedAmountMax,
                        fixedExpense.Currency ?? allAccounts[fixedExpense.DefaultAccountId!].Currency!));
                }
            }
        }

        var rangeMin = minDate.ToValtDateTime();
        var rangeMax = minDate.AddMonths(1).ToValtDateTime();
        //scan all fixed expense records from this month
        var fixedExpenseRecords = _localDatabase.GetFixedExpenseRecords().Query()
            .Where(x => x.ReferenceDate >= rangeMin && x.ReferenceDate < rangeMax).ToList();
        foreach (var entry in entries)
        {
            // Compare using DateOnly to avoid timezone issues with DateTime
            var entryDate = entry.ReferenceDate;
            var match = fixedExpenseRecords.SingleOrDefault(x =>
                DateOnly.FromDateTime(x.ReferenceDate.ToUniversalTime()) == entryDate &&
                x.FixedExpense.Id.ToString() == entry.Id);

            if (match is null)
                continue;

            switch (match.FixedExpenseRecordStateId)
            {
                case (int)FixedExpenseRecordState.Paid when match.Transaction is not null:
                    entry.Pay(match.Transaction.Id.ToString());
                    break;
                case (int)FixedExpenseRecordState.Ignored:
                    entry.Ignore();
                    break;
                case (int)FixedExpenseRecordState.ManuallyPaid:
                    entry.MarkAsPaid();
                    break;
            }
        }

        return Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(entries.OrderBy(x => x.Day));
    }

    private List<FixedExpenseRangeWithEnd> GetFixedExpenseRangesFor(FixedExpenseEntity fixedExpense, DateOnly minDate,
        DateOnly maxDate)
    {
        var fixedExpenseRanged = fixedExpense.Ranges.OrderBy(x => x.PeriodStart);

        var initialRangeEntity = fixedExpenseRanged.Where(x => x.PeriodStart <= minDate.ToValtDateTime())
            .OrderByDescending(x => x.PeriodStart).FirstOrDefault();

        var intervalRangeEntities = fixedExpenseRanged
            .Where(x => x.PeriodStart <= maxDate.ToValtDateTime() && x.PeriodStart > minDate.ToValtDateTime())
            .ToHashSet();

        if (initialRangeEntity is not null)
            intervalRangeEntities.Add(initialRangeEntity);

        var orderedRanges = intervalRangeEntities.OrderBy(x => x.PeriodStart).ToArray();

        var result = new List<FixedExpenseRangeWithEnd>();
        if (orderedRanges.Length == 0)
            return result;
        
        for (var index = 0; index < orderedRanges.Length - 1; index++)
        {
            result.Add(new FixedExpenseRangeWithEnd(orderedRanges[index], orderedRanges[index + 1].PeriodStart));
        }
        result.Add(new FixedExpenseRangeWithEnd(orderedRanges[^1], null));
        
        return result;
    }

    public enum Frequency
    {
        Weekly,
        BiWeekly
    }

    public static List<DateOnly> GetDatesInPeriod(
        DateOnly initialDate,
        DayOfWeek dayOfWeek,
        DateOnly startDate,
        DateOnly endDate,
        Frequency frequency)
    {
        var result = new List<DateOnly>();

        // Validate input
        if (startDate > endDate)
        {
            return result;
        }

        // Find first occurrence of dayOfWeek on or after initialDate
        var currentDate = initialDate;
        while (currentDate.DayOfWeek != dayOfWeek)
        {
            currentDate = currentDate.AddDays(1);
        }

        // Calculate days to add based on frequency
        int daysToAdd = frequency == Frequency.Weekly ? 7 : 14;

        // Add dates in the correct frequency cycle within startDate and endDate
        while (currentDate <= endDate)
        {
            if (currentDate >= startDate)
            {
                result.Add(currentDate);
            }

            currentDate = currentDate.AddDays(daysToAdd);
        }

        return result;
    }

    internal record FixedExpenseRangeWithEnd(FixedExpenseRangeEntity Entity, DateTime? RangeEnd);
}