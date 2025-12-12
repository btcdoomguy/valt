using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.AvgPrice;

public class AvgPriceProfileId : CommonId
{
    public AvgPriceProfileId() : base(IdGenerator.Generate())
    {
    }

    public AvgPriceProfileId(string value) : base(value)
    {
    }

    public static implicit operator string(AvgPriceProfileId id) => id.Value;

    public static implicit operator AvgPriceProfileId(string id) => new(id);
}