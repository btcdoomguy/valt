namespace Valt.Core.Common;

public static class FinancialCalculator
{
    /// <summary>
    /// Calculates meaningful % improvement even when crossing zero or dealing with negative values.
    /// Interprets the change as "how much closer (or farther) we are to positive territory".
    /// </summary>
    public static decimal CalculateImprovementPercentage(decimal previousValue, decimal currentValue)
    {
        var change = currentValue - previousValue;

        if (change == 0m) return 0m;

        if (previousValue == 0m)
        {
            return currentValue > 0 ? 100m : -100m; 
        }

        var baseForPercentage = Math.Abs(previousValue);

        var percentage = (change / baseForPercentage) * 100m;

        return percentage;
    }
}