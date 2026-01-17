using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Goals;

public class GoalId : CommonId
{
    public GoalId() : base(IdGenerator.Generate())
    {
    }

    public GoalId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(GoalId id) => id.Value;

    public static implicit operator GoalId(string id) => new(id);
}