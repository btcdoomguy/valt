using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Configuration;

public class ConfigurationManager
{
    private readonly ILocalDatabase _localDatabase;

    public ConfigurationManager(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<int> GetMigrationVersionAsync()
    {
        var latestVersion = _localDatabase.GetConfiguration().
            FindOne(x => x.Key == ConfigurationKeys.MigrationVersion);
        
        if (latestVersion is null)
            return Task.FromResult(0);
        
        return Task.FromResult(int.Parse(latestVersion.Value));
    }

    public Task SetMigrationVersionAsync(int version)
    {
        var latestVersion = _localDatabase.GetConfiguration().
            FindOne(x => x.Key == ConfigurationKeys.MigrationVersion);
        
        if (latestVersion is null)
            latestVersion = new ConfigurationEntity() { Key = ConfigurationKeys.MigrationVersion };
        
        latestVersion.Value = version.ToString();
        
        _localDatabase.GetConfiguration().Upsert(latestVersion);
        
        return Task.CompletedTask;
    }
}