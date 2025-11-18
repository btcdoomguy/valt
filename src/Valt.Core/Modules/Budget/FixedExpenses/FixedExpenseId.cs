using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.FixedExpenses;

public class FixedExpenseId : CommonId
{
    public FixedExpenseId() : base(IdGenerator.Generate())
    {
    }

    public FixedExpenseId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(FixedExpenseId id) => id.Value;

    public static implicit operator FixedExpenseId(string id) => new(id);
}