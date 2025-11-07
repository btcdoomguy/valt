using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Builders;

public class FiatAccountBuilder
{
    public AccountId Id { get; set; }
    public AccountName Name { get; set; }
    public FiatCurrency FiatCurrency { get; set; }
    public Icon Icon { get; set; } = Icon.Empty;
    public FiatValue Value { get; set; }
    public bool Visible { get; set; } = true;
    public int Version { get; set; } = 1;


    public AccountEntity Build()
    {
        return new AccountEntity()
        {
            Id = Id.ToObjectId(),
            Currency = FiatCurrency.Code,
            InitialAmount = Value,
            Icon = Icon.ToString(),
            Name = Name,
            AccountEntityType = AccountEntityType.Fiat,
            Version = Version,
            Visible = Visible
        };
    }
}