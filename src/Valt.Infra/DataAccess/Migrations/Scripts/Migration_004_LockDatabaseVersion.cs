namespace Valt.Infra.DataAccess.Migrations.Scripts;

/// <summary>
/// Locks the database to the current application version.
/// Prevents older Valt versions from opening databases migrated to 0.2.12.4 or later.
/// </summary>
internal class Migration_004_LockDatabaseVersion : IMigrationScript
{
    public int Version => 4;

    public Version MinimumAssemblyVersion => new(0, 2, 12, 4);

    public Task ExecuteAsync()
    {
        // No schema changes required. The migration itself updates the minimum
        // assembly version stored in the database via MigrationManager.
        return Task.CompletedTask;
    }
}
