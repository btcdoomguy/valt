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

    /// <summary>
    /// If set, indicates that the prices.db should be reset to this version.
    /// The reset only occurs if the current prices.db version is lower than this value.
    /// After reset, the prices.db version will be set to this value.
    /// </summary>
    int? ResetPricesToVersion => null;

    Task ExecuteAsync();
}