using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Builders;

public class BtcAccountBuilder
{
    public AccountId Id { get; set; }
    public AccountName Name { get; set; }
    public Icon Icon = Icon.Empty;
    public BtcValue Value { get; set; }
    public bool Visible { get; set; } = true;
    public int Version { get; set; } = 1;

    public AccountEntity Build()
    {
        return new AccountEntity()
        {
            Id = new ObjectId(Id),
            Currency = null,
            InitialAmount = Value.Sats,
            Icon = Icon.ToString(),
            Name = Name,
            AccountEntityType = AccountEntityType.Bitcoin,
            Version = Version,
            Visible = Visible
        };
    }
}