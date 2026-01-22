using Valt.Core.Common;
using Valt.Core.Kernel;
using Valt.Core.Modules.Budget.Accounts.Events;

namespace Valt.Core.Modules.Budget.Accounts;

public abstract class Account : AggregateRoot<AccountId>
{
    public AccountName Name { get; protected set; }
    public AccountCurrencyNickname CurrencyNickname { get; protected set; }
    public bool Visible { get; protected set; }
    public Icon Icon { get; protected set; }
    public int DisplayOrder { get; protected set; }
    public AccountGroupId? GroupId { get; protected set; }

    public abstract AccountTypes AccountType { get; }

    protected Account(AccountId id, AccountName name, bool visible, Icon icon, AccountCurrencyNickname currencyNickname, int displayOrder, AccountGroupId? groupId, int version)
    {
        Id = id;
        Name = name;
        Visible = visible;
        Icon = icon;
        CurrencyNickname = currencyNickname;
        DisplayOrder = displayOrder;
        GroupId = groupId;
        Version = version;

        if (Version == 0)
            AddEvent(new AccountCreatedEvent(this));
    }

    public void Rename(AccountName name)
    {
        if (Name == name)
            return;
        
        Name = name;

        AddEvent(new AccountUpdatedEvent(this));
    }
    
    public void ChangeCurrencyNickname(AccountCurrencyNickname nickname)
    {
        if (CurrencyNickname == nickname)
            return;
        
        CurrencyNickname = nickname;

        AddEvent(new AccountUpdatedEvent(this));
    }

    public void ChangeVisibility(bool visible)
    {
        if (Visible == visible)
            return;
        
        Visible = visible;

        AddEvent(new AccountUpdatedEvent(this));
    }

    public void ChangeIcon(Icon icon)
    {
        if (Icon == icon)
            return;
        
        Icon = icon;

        AddEvent(new AccountUpdatedEvent(this));
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        if (DisplayOrder == displayOrder)
            return;

        DisplayOrder = displayOrder;

        AddEvent(new AccountUpdatedEvent(this));
    }

    public void AssignToGroup(AccountGroupId? groupId)
    {
        if (GroupId == groupId)
            return;

        GroupId = groupId;

        AddEvent(new AccountUpdatedEvent(this));
    }
}