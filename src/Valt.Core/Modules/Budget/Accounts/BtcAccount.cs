using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts.Events;

namespace Valt.Core.Modules.Budget.Accounts;

public sealed class BtcAccount : Account
{
    public BtcValue InitialAmount { get; private set; }
    public override AccountTypes AccountType => AccountTypes.Bitcoin;

    private BtcAccount(AccountId id, AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, BtcValue initialAmount,
        int displayOrder, int version) :
        base(id, name, visible, icon, currencyNickname, displayOrder, version)
    {
        InitialAmount = initialAmount;
    }

    public static BtcAccount New(AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, BtcValue initialAmount)
        => new(new AccountId(), name, currencyNickname, visible, icon, initialAmount, int.MaxValue, 0);

    public static BtcAccount Create(AccountId id, AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, BtcValue initialAmount,
        int displayOrder,
        int version)
        => new(id, name, currencyNickname, visible, icon, initialAmount, displayOrder, version);

    public void ChangeInitialAmount(BtcValue initialAmount)
    {
        if (InitialAmount == initialAmount)
            return;
        
        var previousInitialAmount = InitialAmount;
        InitialAmount = initialAmount;

        AddEvent(new AccountInitialAmountChangedEvent(this, previousInitialAmount, null));
        AddEvent(new AccountUpdatedEvent(this));
    }
}