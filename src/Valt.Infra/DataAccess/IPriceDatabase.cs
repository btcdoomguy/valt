using System.ComponentModel;
using LiteDB;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.DataAccess;

/// <summary>
/// The price database is a segregated repository to store the price data. It is shared by all Valt dbs.
/// </summary>
public interface IPriceDatabase : INotifyPropertyChanged, IDisposable
{
    bool HasDatabaseOpen { get; }

    bool DatabaseFileExists();
    void OpenDatabase();
    void OpenInMemoryDatabase(Stream stream);
    void CloseDatabase();
    
    ILiteCollection<BitcoinDataEntity> GetBitcoinData();
    ILiteCollection<FiatDataEntity> GetFiatData();
    
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
}