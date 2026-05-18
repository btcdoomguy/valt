using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Assets;

public class AssetGroupId : CommonId
{
    public AssetGroupId() : base(IdGenerator.Generate())
    {
    }

    public AssetGroupId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(AssetGroupId id) => id.Value;

    public static implicit operator AssetGroupId(string id) => new(id);
}
