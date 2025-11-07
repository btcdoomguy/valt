namespace Valt.Infra.DataAccess.Migrations.Scripts;

internal interface IMigrationScript
{
    int Version { get; }
    
    Task ExecuteAsync();
}