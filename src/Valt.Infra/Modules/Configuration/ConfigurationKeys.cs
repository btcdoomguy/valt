namespace Valt.Infra.Modules.Configuration;

internal static class ConfigurationKeys
{
    public static string MigrationVersion => "MigrationVersion";
    public static string AvailableFiatCurrencies => "AvailableFiatCurrencies";
    public static string MinimumAssemblyVersion => "MinimumAssemblyVersion";
    public static string StatisticsExcludedCategories => "StatisticsExcludedCategories";
    public static string ExpensesCategoryFilterExcluded => "ExpensesCategoryFilterExcluded";
    public static string IncomeCategoryFilterExcluded => "IncomeCategoryFilterExcluded";
    public static string SimulatedPrices => "SimulatedPrices";
}