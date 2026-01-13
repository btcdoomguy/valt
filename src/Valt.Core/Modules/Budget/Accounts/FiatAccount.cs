using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts.Events;

namespace Valt.Core.Modules.Budget.Accounts;

public sealed class FiatAccount : Account
{
    public FiatCurrency FiatCurrency { get; private set; }
    public FiatValue InitialAmount { get; private set; }
    public override AccountTypes AccountType => AccountTypes.Fiat;

    private FiatAccount(AccountId id, AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, FiatCurrency fiatCurrency,
        FiatValue initialAmount, int displayOrder, int version)
        : base(id, name, visible, icon, currencyNickname, displayOrder, version)
    {
        FiatCurrency = fiatCurrency;
        InitialAmount = initialAmount;
    }

    public static FiatAccount New(AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, FiatCurrency fiatCurrency,
        FiatValue initialAmount)
        => new(new AccountId(), name, currencyNickname, visible, icon, fiatCurrency, initialAmount, int.MaxValue, 0);

    public static FiatAccount Create(AccountId id, AccountName name, AccountCurrencyNickname currencyNickname, bool visible, Icon icon, FiatCurrency fiatCurrency,
        FiatValue initialAmount,
        int displayOrder,
        int version)
        => new(id, name, currencyNickname, visible, icon, fiatCurrency, initialAmount, displayOrder, version);

    public void ChangeCurrency(FiatCurrency fiatCurrency)
    {
        if (FiatCurrency == fiatCurrency)
            return;
        
        FiatCurrency = fiatCurrency;

        AddEvent(new AccountUpdatedEvent(this));
    }

    public void ChangeInitialAmount(FiatValue initialAmount)
    {
        if (InitialAmount == initialAmount)
            return;
        
        var previousInitialAmount = InitialAmount;
        InitialAmount = initialAmount;

        AddEvent(new AccountInitialAmountChangedEvent(this, null, previousInitialAmount));
        AddEvent(new AccountUpdatedEvent(this));
    }
}