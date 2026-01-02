using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.AvgPrice;

public class AvgPriceLineId : CommonId
{
    public AvgPriceLineId() : base(IdGenerator.Generate())
    {
    }

    public AvgPriceLineId(string value) : base(value)
    {
    }

    public static implicit operator string(AvgPriceLineId id) => id.Value;

    public static implicit operator AvgPriceLineId(string id) => new(id);
}