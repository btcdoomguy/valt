namespace Valt.Infra.Modules.Budget;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
    Task MigrateAsync();
}