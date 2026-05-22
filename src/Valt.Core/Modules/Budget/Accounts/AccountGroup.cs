using Valt.Core.Kernel;

namespace Valt.Core.Modules.Budget.Accounts;

public class AccountGroup : Entity<AccountGroupId>
{
    public AccountGroupName Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }
    public AccountGroupTotalCurrency TotalCurrency { get; private set; }

    private AccountGroup(AccountGroupId id, AccountGroupName name, int displayOrder, int version, AccountGroupTotalCurrency totalCurrency)
    {
        Id = id;
        Name = name;
        DisplayOrder = displayOrder;
        Version = version;
        TotalCurrency = totalCurrency;
    }

    public static AccountGroup New(AccountGroupName name)
        => new(new AccountGroupId(), name, int.MaxValue, 0, AccountGroupTotalCurrency.DefaultFiat());

    public static AccountGroup Create(AccountGroupId id, AccountGroupName name, int displayOrder, int version, AccountGroupTotalCurrency totalCurrency)
        => new(id, name, displayOrder, version, totalCurrency);

    public void Rename(AccountGroupName name)
    {
        if (Name == name)
            return;

        Name = name;
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        if (DisplayOrder == displayOrder)
            return;

        DisplayOrder = displayOrder;
    }

    public void ChangeTotalCurrency(AccountGroupTotalCurrency totalCurrency)
    {
        if (TotalCurrency == totalCurrency)
            return;

        TotalCurrency = totalCurrency;
    }
}
