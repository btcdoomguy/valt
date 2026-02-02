namespace Valt.Infra.Modules.Configuration;

/// <summary>
/// Interface for configuration management operations.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Gets whether the local database is currently open.
    /// </summary>
    bool HasLocalDatabaseOpen { get; }

    /// <summary>
    /// Gets the current migration version.
    /// </summary>
    Task<int> GetMigrationVersionAsync();

    /// <summary>
    /// Sets the migration version.
    /// </summary>
    Task SetMigrationVersionAsync(int version);

    /// <summary>
    /// Gets the list of available fiat currencies from configuration.
    /// </summary>
    List<string> GetAvailableFiatCurrencies();

    /// <summary>
    /// Sets the list of available fiat currencies.
    /// </summary>
    void SetAvailableFiatCurrencies(IEnumerable<string> currencies);

    /// <summary>
    /// Adds a fiat currency to the available currencies list if not already present.
    /// </summary>
    void AddFiatCurrency(string currencyCode);

    /// <summary>
    /// Removes a fiat currency from the available currencies list.
    /// </summary>
    void RemoveFiatCurrency(string currencyCode);

    /// <summary>
    /// Gets the minimum assembly version required to open this database.
    /// </summary>
    Version? GetMinimumAssemblyVersion();

    /// <summary>
    /// Sets the minimum assembly version required to open this database.
    /// </summary>
    void SetMinimumAssemblyVersion(Version version);

    /// <summary>
    /// Gets the list of fiat currencies currently in use by accounts, fixed expenses, and avg price profiles.
    /// </summary>
    List<string> GetCurrenciesInUse();

    /// <summary>
    /// Gets the list of category IDs excluded from statistics calculations.
    /// </summary>
    List<string> GetStatisticsExcludedCategoryIds();

    /// <summary>
    /// Sets the list of category IDs to exclude from statistics calculations.
    /// </summary>
    void SetStatisticsExcludedCategoryIds(IEnumerable<string> categoryIds);
}
