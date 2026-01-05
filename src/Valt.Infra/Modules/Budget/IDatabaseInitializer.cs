namespace Valt.Infra.Modules.Budget;

public interface IDatabaseInitializer
{
    Task InitializeAsync(string? initialDataLanguage = null, IEnumerable<string>? selectedCurrencies = null);
    Task MigrateAsync();
}