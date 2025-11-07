using System.ComponentModel;
using LiteDB;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;

namespace Valt.Infra.DataAccess;

//TODO: refactor to hide this from the UI. should use a proxy to access what is needed
public interface ILocalDatabase : INotifyPropertyChanged, IDisposable
{
    bool HasDatabaseOpen { get; }

    void OpenDatabase(string filePath, string password);
    void OpenInMemoryDatabase(Stream stream);
    void CloseDatabase();
    void ChangeDatabasePassword(string oldPassword, string newPassword);

    void ClearAccountCache();

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
}