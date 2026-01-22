namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

/// <summary>
/// Represents a group header in the accounts list.
/// </summary>
public class AccountGroupHeaderViewModel : IAccountListItem
{
    public string Id { get; }
    public string Name { get; }

    public AccountGroupHeaderViewModel(string id, string name)
    {
        Id = id;
        Name = name;
    }
}
