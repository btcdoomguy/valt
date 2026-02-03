using System.ComponentModel;
using LiteDB;
using Valt.Infra.Modules.Assets;
using Valt.Infra.Modules.AvgPrice;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Settings;

namespace Valt.Infra.DataAccess;

// Architecture note: Consider refactoring to hide raw database access from UI layer.
// Create a proxy/facade layer that exposes only the query methods needed by ViewModels.
public interface ILocalDatabase : INotifyPropertyChanged, IDisposable
{
    bool HasDatabaseOpen { get; }

    void OpenDatabase(string filePath, string password);
    void OpenInMemoryDatabase(Stream stream);
    void CloseDatabase();
    void ChangeDatabasePassword(string oldPassword, string newPassword);

    void ClearAccountCache();

    #region AvgPrice module

    ILiteCollection<AvgPriceProfileEntity> GetAvgPriceProfiles();
    ILiteCollection<AvgPriceLineEntity> GetAvgPriceLines();

    #endregion

    #region Budget module

    ILiteCollection<AccountEntity> GetAccounts();
    ILiteCollection<AccountGroupEntity> GetAccountGroups();
    ILiteCollection<CategoryEntity> GetCategories();
    ILiteCollection<FixedExpenseEntity> GetFixedExpenses();
    ILiteCollection<FixedExpenseRecordEntity> GetFixedExpenseRecords();
    ILiteCollection<TransactionEntity> GetTransactions();

    #endregion

    #region Support tables

    ILiteCollection<AccountCacheEntity> GetAccountCaches();
    ILiteCollection<TransactionTermEntity> GetTransactionTerms();

    #endregion

    #region Configuration module

    ILiteCollection<ConfigurationEntity> GetConfiguration();

    #endregion

    #region Settings module

    ILiteCollection<SettingEntity> GetSettings();

    #endregion

    #region Goals module

    ILiteCollection<GoalEntity> GetGoals();

    #endregion

    #region Assets module

    ILiteCollection<AssetEntity> GetAssets();

    #endregion

    #region Thread-safe database access for MCP

    /// <summary>
    /// Executes a database operation with thread-safe locking.
    /// Use this method for MCP tool operations that may run on a background thread.
    /// </summary>
    Task<T> ExecuteWithLockAsync<T>(Func<LiteDatabase, T> operation, CancellationToken ct = default);

    /// <summary>
    /// Executes a database operation with thread-safe locking (async version).
    /// </summary>
    Task<T> ExecuteWithLockAsync<T>(Func<LiteDatabase, Task<T>> operation, CancellationToken ct = default);

    /// <summary>
    /// Executes a void database operation with thread-safe locking.
    /// </summary>
    Task ExecuteWithLockAsync(Action<LiteDatabase> operation, CancellationToken ct = default);

    #endregion
}