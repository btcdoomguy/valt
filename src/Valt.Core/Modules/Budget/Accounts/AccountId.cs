using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.Accounts;

public class AccountId : CommonId
{
    public AccountId() : base(IdGenerator.Generate())
    {
    }

    public AccountId(string value) : base(value)
    {
    }

    public static implicit operator string(AccountId id) => id.Value;

    public static implicit operator AccountId(string id) => new(id);
}