using System;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.UI.Lang;

namespace Valt.UI.Base.Utils;

public static class ExtensionMethods
{
    public static string ToValtCaption(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => language.DaysOfWeek_Sunday,
            DayOfWeek.Monday => language.DaysOfWeek_Monday,
            DayOfWeek.Tuesday => language.DaysOfWeek_Tuesday,
            DayOfWeek.Wednesday => language.DaysOfWeek_Wednesday,
            DayOfWeek.Thursday => language.DaysOfWeek_Thursday,
            DayOfWeek.Friday => language.DaysOfWeek_Friday,
            DayOfWeek.Saturday => language.DaysOfWeek_Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };
    }

    public static bool IsPeriodUsingFixedDay(this FixedExpensePeriods periods)
    {
        return periods switch
        {
            FixedExpensePeriods.Monthly => true,
            FixedExpensePeriods.Yearly => true,
            FixedExpensePeriods.Weekly => false,
            FixedExpensePeriods.Biweekly => false,
            _ => throw new ArgumentOutOfRangeException(nameof(periods), periods, null)
        };
    }
}