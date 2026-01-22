using Valt.Core.Kernel;

namespace Valt.Core.Modules.Budget.Accounts;

public class AccountGroup : Entity<AccountGroupId>
{
    public AccountGroupName Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }

    private AccountGroup(AccountGroupId id, AccountGroupName name, int displayOrder, int version)
    {
        Id = id;
        Name = name;
        DisplayOrder = displayOrder;
        Version = version;
    }

    public static AccountGroup New(AccountGroupName name)
        => new(new AccountGroupId(), name, int.MaxValue, 0);

    public static AccountGroup Create(AccountGroupId id, AccountGroupName name, int displayOrder, int version)
        => new(id, name, displayOrder, version);

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
}
