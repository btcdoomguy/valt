namespace Valt.Infra.DataAccess.Migrations.Scripts;

internal interface IMigrationScript
{
    int Version { get; }

    /// <summary>
    /// The minimum assembly version required to run this migration.
    /// When migrations are applied, the highest MinimumAssemblyVersion among all executed migrations
    /// is stored in the database to prevent older app versions from opening newer databases.
    /// </summary>
    Version MinimumAssemblyVersion { get; }

    Task ExecuteAsync();
}