using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Assets;

public class AssetId : CommonId
{
    public AssetId() : base(IdGenerator.Generate())
    {
    }

    public AssetId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(AssetId id) => id.Value;

    public static implicit operator AssetId(string id) => new(id);
}
