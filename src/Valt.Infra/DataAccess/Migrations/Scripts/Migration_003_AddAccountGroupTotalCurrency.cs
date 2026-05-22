using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.DataAccess.Migrations.Scripts;

/// <summary>
/// Migration to add TotalCurrency field to existing account groups.
/// Sets all existing groups to use the default fiat currency.
/// </summary>
internal class Migration_003_AddAccountGroupTotalCurrency : IMigrationScript
{
    private readonly ILocalDatabase _localDatabase;

    public Migration_003_AddAccountGroupTotalCurrency(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public int Version => 3;

    public Version MinimumAssemblyVersion => new(0, 2, 0, 2);

    public Task ExecuteAsync()
    {
        var collection = _localDatabase.GetAccountGroups();
        var items = collection.FindAll().ToList();

        foreach (var item in items)
        {
            // Set default value for existing groups
            if (string.IsNullOrWhiteSpace(item.TotalCurrency))
            {
                item.TotalCurrency = "DEFAULT";
                collection.Update(item);
            }
        }

        return Task.CompletedTask;
    }
}
