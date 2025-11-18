using Valt.Infra.DataAccess.Migrations.Scripts;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.DataAccess.Migrations;

internal class MigrationManager
{
    private readonly IEnumerable<IMigrationScript> _migrationScripts;
    private readonly ConfigurationManager _configurationManager;

    public MigrationManager(IEnumerable<IMigrationScript> migrationScripts,
        ConfigurationManager configurationManager)
    {
        _migrationScripts = migrationScripts;
        _configurationManager = configurationManager;
    }

    public async Task RunMigrationsAsync()
    {
        var latestVersion = await _configurationManager.GetMigrationVersionAsync();

        var nextMigrations = _migrationScripts.Where(x => x.Version > latestVersion).ToList();

        if (nextMigrations.Count == 0)
            return;
        
        foreach (var migration in nextMigrations)
        {
            await migration.ExecuteAsync();
        }
        
        await _configurationManager.SetMigrationVersionAsync(nextMigrations.Max(x => x.Version));
    }
}