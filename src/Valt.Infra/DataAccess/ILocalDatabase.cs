using System.ComponentModel;
using LiteDB;
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
}