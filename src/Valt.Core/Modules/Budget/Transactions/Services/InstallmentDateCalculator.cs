namespace Valt.Core.Modules.Budget.Transactions.Services;

public static class InstallmentDateCalculator
{
    /// <summary>
    /// Calculates the dates for installments starting from a given date.
    /// Handles month-end edge cases (e.g., 31 Jan → 28 Feb → 31 Mar).
    /// </summary>
    /// <param name="startDate">The date of the first installment</param>
    /// <param name="numberOfInstallments">The total number of installments</param>
    /// <returns>An enumerable of dates for each installment</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when numberOfInstallments is less than 1</exception>
    public static IEnumerable<DateOnly> CalculateInstallmentDates(DateOnly startDate, int numberOfInstallments)
    {
        if (numberOfInstallments < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfInstallments),
                "Number of installments must be at least 1");

        var originalDay = startDate.Day;

        for (var i = 0; i < numberOfInstallments; i++)
        {
            if (i == 0)
            {
                yield return startDate;
                continue;
            }

            // Calculate target year and month
            var targetMonth = startDate.Month + i;
            var targetYear = startDate.Year;

            // Handle year overflow
            while (targetMonth > 12)
            {
                targetMonth -= 12;
                targetYear++;
            }

            // Get the number of days in the target month
            var daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);

            // Use the original day if possible, otherwise use the last day of the month
            var targetDay = Math.Min(originalDay, daysInTargetMonth);

            yield return new DateOnly(targetYear, targetMonth, targetDay);
        }
    }
}
