using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.Accounts;

public class AccountGroupId : CommonId
{
    public AccountGroupId() : base(IdGenerator.Generate())
    {
    }

    public AccountGroupId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(AccountGroupId id) => id.Value;

    public static implicit operator AccountGroupId(string id) => new(id);
}
