namespace Valt.Infra.Kernel.Extensions;

public record EnumItem(int Value, string Name)
{
    public override string ToString() => Name;
}