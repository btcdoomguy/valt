using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Kernel.Exceptions;

namespace Valt.Infra.Modules.Budget.Accounts;

public static class Extensions
{
    public static Account AsDomainObject(this AccountEntity entity)
    {
        try
        {
            var accountEntityType = (AccountEntityType)entity.AccountEntityTypeId;
            return accountEntityType switch
            {
                AccountEntityType.Bitcoin => ConvertToBtcAccount(entity),
                AccountEntityType.Fiat => ConvertToFiatAccount(entity),
                _ => throw new ArgumentException("Account type not found")
            };
        }
        catch (Exception ex)
        {
            throw new BrokenConversionFromDbException(nameof(AccountEntity), entity.Id.ToString(), ex);
        }
    }

    private static BtcAccount ConvertToBtcAccount(AccountEntity entity)
    {
        return BtcAccount.Create(entity.Id.ToString(),
            entity.Name,
            entity.Visible,
            entity.Icon is not null ? Icon.RestoreFromId(entity.Icon) : Icon.Empty,
            BtcValue.New(Convert.ToInt64(entity.InitialAmount)),
            entity.DisplayOrder,
            entity.Version
        );
    }

    private static FiatAccount ConvertToFiatAccount(AccountEntity entity)
    {
        return FiatAccount.Create(entity.Id.ToString(),
            entity.Name,
            entity.Visible,
            entity.Icon is not null ? Icon.RestoreFromId(entity.Icon) : Icon.Empty,
            FiatCurrency.GetFromCode(entity.Currency!),
            FiatValue.New(entity.InitialAmount),
            entity.DisplayOrder,
            entity.Version
        );
    }

    public static AccountEntity AsEntity(this Account account)
    {
        try
        {
            return account switch
            {
                BtcAccount btcAccount => ConvertBtcAccountToEntity(btcAccount),
                FiatAccount fiatAccount => ConvertFiatAccountToEntity(fiatAccount),
                _ => throw new ArgumentException("Account type not found")
            };
        }
        catch (Exception ex)
        {
            throw new BrokenConversionToDbException(nameof(Account), account.Id.ToString(), ex);
        }
    }

    private static AccountEntity ConvertBtcAccountToEntity(BtcAccount btcAccount)
    {
        return new AccountEntity()
        {
            Id = new ObjectId(btcAccount.Id),
            AccountEntityTypeId = (int)AccountEntityType.Bitcoin,
            InitialAmount = btcAccount.InitialAmount.Sats,
            Icon = btcAccount.Icon.ToString(),
            Currency = null,
            Name = btcAccount.Name,
            Version = btcAccount.Version,
            DisplayOrder = btcAccount.DisplayOrder,
            Visible = btcAccount.Visible
        };
    }

    private static AccountEntity ConvertFiatAccountToEntity(FiatAccount fiatAccount)
    {
        return new AccountEntity()
        {
            Id = new ObjectId(fiatAccount.Id),
            AccountEntityTypeId = (int)AccountEntityType.Fiat,
            InitialAmount = fiatAccount.InitialAmount,
            Icon = fiatAccount.Icon.ToString(),
            Currency = fiatAccount.FiatCurrency.Code,
            Name = fiatAccount.Name,
            Version = fiatAccount.Version,
            DisplayOrder = fiatAccount.DisplayOrder,
            Visible = fiatAccount.Visible
        };
    }

    public static ObjectId AsObjectId(this AccountId accountId) => new(accountId);
}