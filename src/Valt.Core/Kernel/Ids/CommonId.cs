namespace Valt.Core.Kernel.Ids;

public abstract class CommonId : EntityId<string>
{
    protected CommonId(string value) : base(value)
    {
    }
}