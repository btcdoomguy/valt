using LiteDB;
using Valt.Infra.Modules.AvgPrice;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.DataAccess.Migrations.Scripts;

/// <summary>
/// Migration to convert all DateTime fields from local midnight to UTC noon.
/// This ensures dates remain stable across timezone changes.
///
/// IMPORTANT: This migration should be run while the user is in the same timezone
/// as when the data was originally created. If the timezone has already changed,
/// the dates may be off by one day.
/// </summary>
internal class Migration_001_ConvertDatesToUtcNoon : IMigrationScript
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;

    public Migration_001_ConvertDatesToUtcNoon(ILocalDatabase localDatabase, IPriceDatabase priceDatabase)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
    }

    public int Version => 1;

    public Version MinimumAssemblyVersion => new(0, 2, 0, 2);

    public Task ExecuteAsync()
    {
        // Migrate local database collections
        MigrateTransactions();
        MigrateFixedExpenses();
        MigrateFixedExpenseRecords();
        MigrateAvgPriceLines();
        MigrateAccountCache();

        // Migrate price database (shared, but needs to match new format)
        MigratePriceDatabase();

        return Task.CompletedTask;
    }

    private void MigrateTransactions()
    {
        var collection = _localDatabase.GetTransactions();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            item.Date = ConvertToUtcNoon(item.Date);
            collection.Update(item);
        }
    }

    private void MigrateFixedExpenses()
    {
        var collection = _localDatabase.GetFixedExpenses();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            item.PeriodStart = ConvertToUtcNoon(item.PeriodStart);

            // Also migrate embedded ranges
            foreach (var range in item.Ranges)
            {
                range.PeriodStart = ConvertToUtcNoon(range.PeriodStart);
            }

            collection.Update(item);
        }
    }

    private void MigrateFixedExpenseRecords()
    {
        var collection = _localDatabase.GetFixedExpenseRecords();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            item.ReferenceDate = ConvertToUtcNoon(item.ReferenceDate);
            collection.Update(item);
        }
    }

    private void MigrateAvgPriceLines()
    {
        var collection = _localDatabase.GetAvgPriceLines();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            item.Date = ConvertToUtcNoon(item.Date);
            collection.Update(item);
        }
    }

    private void MigrateAccountCache()
    {
        var collection = _localDatabase.GetAccountCaches();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            item.CurrentDate = ConvertToUtcNoon(item.CurrentDate);
            collection.Update(item);
        }
    }

    private void MigratePriceDatabase()
    {
        // Check if price database is already migrated by looking at first entry
        var btcSample = _priceDatabase.GetBitcoinData().FindAll().FirstOrDefault();
        if (btcSample != null && IsAlreadyUtcNoon(btcSample.Date))
        {
            // Already migrated (probably by another database instance)
            return;
        }

        // Migrate Bitcoin prices
        var btcCollection = _priceDatabase.GetBitcoinData();
        var btcItems = btcCollection.FindAll().ToList();
        foreach (var item in btcItems)
        {
            item.Date = ConvertToUtcNoon(item.Date);
            btcCollection.Update(item);
        }

        // Migrate Fiat prices
        var fiatCollection = _priceDatabase.GetFiatData();
        var fiatItems = fiatCollection.FindAll().ToList();
        foreach (var item in fiatItems)
        {
            item.Date = ConvertToUtcNoon(item.Date);
            fiatCollection.Update(item);
        }
    }

    /// <summary>
    /// Converts a DateTime (assumed to be local midnight from old format) to UTC noon.
    /// LiteDB returns DateTime in local time, so we extract the local date and create UTC noon.
    /// </summary>
    private static DateTime ConvertToUtcNoon(DateTime localDateTime)
    {
        // The DateTime from LiteDB is already converted to local time.
        // We trust that the local date is correct (user hasn't changed timezone since data was created).
        // Extract the date and create UTC noon for that date.
        var date = localDateTime.Date;
        return new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Checks if a DateTime is already in UTC noon format.
    /// </summary>
    private static bool IsAlreadyUtcNoon(DateTime dateTime)
    {
        // Convert to UTC and check if it's at noon
        var utc = dateTime.ToUniversalTime();
        return utc.Hour == 12 && utc.Minute == 0 && utc.Second == 0;
    }
}
