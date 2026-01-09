namespace Valt.Infra.DataAccess.Migrations.Scripts;

/// <summary>
/// Migration to reset the entire prices database.
/// The reset is handled by MigrationManager based on the ResetPricesToVersion property.
/// After reset, the prices.db will be repopulated by background jobs.
/// </summary>
internal class Migration_002_ResetPricesDatabase : IMigrationScript
{
    public int Version => 2;

    public Version MinimumAssemblyVersion => new(0, 2, 0, 2);

    public int? ResetPricesToVersion => 1;

    public Task ExecuteAsync() => Task.CompletedTask;
}
