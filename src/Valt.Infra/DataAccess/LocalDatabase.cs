using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess.LiteDBMappers;
using Valt.Infra.Modules.AvgPrice;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Settings;

namespace Valt.Infra.DataAccess;

internal sealed class LocalDatabase : ILocalDatabase
{
    private readonly IClock _clock;
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private LiteDatabase? _database;
    private bool _inMemoryDb = true;
    private string? _filePath;
    private string? _password;
    public bool HasDatabaseOpen => _database != null;

    public LocalDatabase(IClock clock)
    {
        _clock = clock;
    }
    
    public void OpenDatabase(string filePath, string password)
    {
        if (HasDatabaseOpen)
            throw new InvalidOperationException("A database is already open.");

        var connectionString = new ConnectionString(filePath) { Password = password, Connection = ConnectionType.Direct };

        _database = new LiteDatabase(connectionString, CreateMapper());
        _filePath = filePath;
        _inMemoryDb = false;
        _password = password;

        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    public void OpenInMemoryDatabase(Stream stream)
    {
        if (HasDatabaseOpen)
            throw new InvalidOperationException("A database is already open.");

        _database = new LiteDatabase(stream, CreateMapper());
        _filePath = null;
        _inMemoryDb = true;
        _password = null;
        
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    public void CloseDatabase()
    {
        _database?.Dispose();
        _database = null;
        _filePath = null;
        _inMemoryDb = true;
        _password = null;
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    public void ChangeDatabasePassword(string oldPassword, string newPassword)
    {
        //LiteDB recommended Rebuild() method doesn't work good when DB has lot of entries, so here we use a workaround (just copy all to new db and override the file)
        if (!HasDatabaseOpen)
            throw new InvalidOperationException("No database available.");
        
        if (_inMemoryDb)
            throw new InvalidOperationException("Cannot change password for in-memory database.");
        
        if (oldPassword != _password)
            throw new InvalidOperationException("Old password is not valid.");

        var filePath = _filePath!;
        var password = _password;
        
        CloseDatabase();

        var currentDirectory = Path.GetDirectoryName(filePath)!;
        var backupFileName = Path.GetFileNameWithoutExtension(filePath) + $"-{_clock.GetCurrentDateTimeUtc().ToString("yyyyMMddhhmmss")}.bak";
        
        var tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolderPath);
        
        var bkpFinalFilePath = Path.Combine(currentDirectory, backupFileName);

        var targetFilePath = Path.Combine(tempFolderPath, "newdb.db");
        
        var sourceConnectionString = new ConnectionString(filePath) { Password = password, Connection = ConnectionType.Shared };
        var targetConnectionString = new ConnectionString(targetFilePath) { Password = newPassword, Connection = ConnectionType.Direct };
        
        var sourceDb = new LiteDatabase(sourceConnectionString, CreateMapper());
        var targetDb = new LiteDatabase(targetConnectionString, CreateMapper());

        try
        {
            var collectionNames = sourceDb.GetCollectionNames().ToList();

            foreach (var collName in collectionNames)
            {
                var sourceColl = sourceDb.GetCollection(collName);
                var targetColl = targetDb.GetCollection(collName);

                // Batch size for bulk insert - balances memory usage vs. performance
                const int batchSize = 5_000;
                var batch = new List<BsonDocument>(batchSize);

                foreach (var doc in sourceColl.FindAll().AsEnumerable())
                {
                    batch.Add(doc);
                    if (batch.Count >= batchSize)
                    {
                        targetColl.InsertBulk(batch);
                        batch.Clear();
                    }
                }

                if (batch.Any())
                {
                    targetColl.InsertBulk(batch);
                }
            }
            
            targetDb.Checkpoint();
        }
        finally
        {
            sourceDb.Dispose();
            targetDb.Dispose();
        }

        File.Replace(targetFilePath, filePath, bkpFinalFilePath);
        
        OpenDatabase(filePath, newPassword);
    }

    public void ClearAccountCache()
    {
        GetAccountCaches().DeleteAll();
    }

    private BsonMapper CreateMapper()
    {
        var mapper = new BsonMapper();
        DateOnlyMapper.Register(mapper);
        DateTimeMapper.Register(mapper);
        return mapper;
    }

    private LiteDatabase GetOpenDatabase()
    {
        if (!HasDatabaseOpen || _database is null)
            throw new InvalidOperationException("Open a database first.");
        return _database;
    }

    #region AvgPrice module

    public ILiteCollection<AvgPriceProfileEntity> GetAvgPriceProfiles()
    {
        return GetOpenDatabase().GetCollection<AvgPriceProfileEntity>("avgprice_profile");
    }

    public ILiteCollection<AvgPriceLineEntity> GetAvgPriceLines()
    {
        var collection = GetOpenDatabase().GetCollection<AvgPriceLineEntity>("avgprice_line");

        collection.EnsureIndex(x => x.ProfileId);
        collection.EnsureIndex(x => x.Date);
        collection.EnsureIndex(x => x.DisplayOrder);

        return collection;
    }

    #endregion

    #region Budget module

    public ILiteCollection<AccountEntity> GetAccounts()
    {
        return GetOpenDatabase().GetCollection<AccountEntity>("budget_accounts");
    }

    public ILiteCollection<AccountGroupEntity> GetAccountGroups()
    {
        return GetOpenDatabase().GetCollection<AccountGroupEntity>("budget_accountgroups");
    }

    public ILiteCollection<AccountCacheEntity> GetAccountCaches()
    {
        return GetOpenDatabase().GetCollection<AccountCacheEntity>("budget_accounts_cache");
    }

    public ILiteCollection<TransactionTermEntity> GetTransactionTerms()
    {
        var collection = GetOpenDatabase().GetCollection<TransactionTermEntity>("transaction_terms");

        collection.EnsureIndex(x => x.Name);
        collection.EnsureIndex(x => x.Count);
        collection.EnsureIndex(x => x.CategoryId);

        return collection;
    }

    public ILiteCollection<CategoryEntity> GetCategories()
    {
        return GetOpenDatabase().GetCollection<CategoryEntity>("budget_categories");
    }

    public ILiteCollection<FixedExpenseEntity> GetFixedExpenses()
    {
        return GetOpenDatabase().GetCollection<FixedExpenseEntity>("budget_fixedexpenses");
    }

    public ILiteCollection<FixedExpenseRecordEntity> GetFixedExpenseRecords()
    {
        var collection = GetOpenDatabase().GetCollection<FixedExpenseRecordEntity>("budget_fixedexpenserecords");

        collection.EnsureIndex(x => x.ReferenceDate);
        collection.EnsureIndex(x => x.Transaction);
        collection.EnsureIndex(x => x.FixedExpense);

        return collection;
    }

    public ILiteCollection<TransactionEntity> GetTransactions()
    {
        var collection = GetOpenDatabase().GetCollection<TransactionEntity>("budget_transactions");

        collection.EnsureIndex(x => x.Date);
        collection.EnsureIndex(x => x.FromAccountId);
        collection.EnsureIndex(x => x.ToAccountId);
        collection.EnsureIndex(x => x.CategoryId);
        collection.EnsureIndex(x => x.SatAmountStateId);
        collection.EnsureIndex(x => x.Type);

        return collection;
    }

    #endregion

    #region Configuration module

    public ILiteCollection<ConfigurationEntity> GetConfiguration()
    {
        var collection = GetOpenDatabase().GetCollection<ConfigurationEntity>("system_config");

        collection.EnsureIndex(x => x.Key);

        return collection;
    }

    public ILiteCollection<SettingEntity> GetSettings()
    {
        var collection = GetOpenDatabase().GetCollection<SettingEntity>("system_settings");

        collection.EnsureIndex(x => x.Property);

        return collection;
    }

    #endregion

    #region Goals module

    public ILiteCollection<GoalEntity> GetGoals()
    {
        var collection = GetOpenDatabase().GetCollection<GoalEntity>("goals");

        collection.EnsureIndex(x => x.RefDate);
        collection.EnsureIndex(x => x.IsUpToDate);

        return collection;
    }

    #endregion

    public void Dispose()
    {
        CloseDatabase();
        _dbLock.Dispose();
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    #region Thread-safe database access for MCP

    /// <summary>
    /// Executes a database operation with thread-safe locking.
    /// Use this method for MCP tool operations that may run on a background thread.
    /// </summary>
    public async Task<T> ExecuteWithLockAsync<T>(Func<LiteDatabase, T> operation, CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            return operation(GetOpenDatabase());
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Executes a database operation with thread-safe locking (async version).
    /// </summary>
    public async Task<T> ExecuteWithLockAsync<T>(Func<LiteDatabase, Task<T>> operation, CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            return await operation(GetOpenDatabase());
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Executes a void database operation with thread-safe locking.
    /// </summary>
    public async Task ExecuteWithLockAsync(Action<LiteDatabase> operation, CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            operation(GetOpenDatabase());
        }
        finally
        {
            _dbLock.Release();
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}