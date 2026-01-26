using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.Transactions;

public class GroupId : CommonId
{
    public GroupId() : base(IdGenerator.Generate())
    {
    }

    public GroupId(string value) : base(value)
    {
    }

    public static implicit operator string(GroupId id) => id.Value;

    public static implicit operator GroupId(string id) => new(id);
}
