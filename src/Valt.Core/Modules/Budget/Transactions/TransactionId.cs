using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.Transactions;

public class TransactionId : CommonId
{
    public TransactionId() : base(IdGenerator.Generate())
    {
    }

    public TransactionId(string value) : base(value)
    {
    }

    public static implicit operator string(TransactionId id) => id.Value;

    public static implicit operator TransactionId(string id) => new(id);
}