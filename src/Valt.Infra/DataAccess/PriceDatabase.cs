using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess.LiteDBMappers;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.DataAccess;

internal sealed class PriceDatabase : IPriceDatabase
{
    private readonly IClock _clock;
    private readonly INotificationPublisher _publisher;
    private LiteDatabase? _database;
    private bool _inMemoryDb = true;
    private string? _filePath;
    public bool HasDatabaseOpen => _database != null;


    public PriceDatabase(IClock clock, INotificationPublisher publisher)
    {
        _clock = clock;
        _publisher = publisher;
    }
    
    public bool DatabaseFileExists()
    {
        var filePath = Path.Combine(ValtEnvironment.AppDataPath, "prices.db");
        
        return File.Exists(filePath);
    }
    
    public void OpenDatabase()
    {
        if (HasDatabaseOpen)
            throw new InvalidOperationException("A price database is already open.");

        var filePath = Path.Combine(ValtEnvironment.AppDataPath, "prices.db");
        var connectionString = new ConnectionString(filePath) { Connection = ConnectionType.Direct };

        _database = new LiteDatabase(connectionString, CreateMapper());
        _filePath = filePath;
        _inMemoryDb = false;

        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    public void OpenInMemoryDatabase(Stream stream)
    {
        if (HasDatabaseOpen)
            throw new InvalidOperationException("A price database is already open.");

        _database = new LiteDatabase(stream, CreateMapper());
        _filePath = null;
        _inMemoryDb = true;
        
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    public void CloseDatabase()
    {
        _database?.Dispose();
        _database = null;
        _filePath = null;
        _inMemoryDb = true;
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    private BsonMapper CreateMapper()
    {
        var mapper = new BsonMapper();
        DateOnlyMapper.Register(mapper);
        return mapper;
    }

    #region DataSource module

    public ILiteCollection<BitcoinDataEntity> GetBitcoinData()
    {
        ArgumentNullException.ThrowIfNull(_database);

        if (!HasDatabaseOpen)
            throw new InvalidOperationException("Open a database first.");

        var collection = _database.GetCollection<BitcoinDataEntity>("datasource_bitcoin");

        collection.EnsureIndex(x => x.Date);

        return collection;
    }

    public ILiteCollection<FiatDataEntity> GetFiatData()
    {
        ArgumentNullException.ThrowIfNull(_database);

        if (!HasDatabaseOpen)
            throw new InvalidOperationException("Open a database first.");

        var collection = _database.GetCollection<FiatDataEntity>("datasource_fiat");

        collection.EnsureIndex(x => x.Date);

        return collection;
    }

    #endregion
    
    public void Dispose()
    {
        CloseDatabase();
        OnPropertyChanged(nameof(HasDatabaseOpen));
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}