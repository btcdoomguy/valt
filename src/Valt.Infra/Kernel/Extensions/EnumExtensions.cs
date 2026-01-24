namespace Valt.Infra.Kernel.Extensions;

public static class EnumExtensions
{
    public static List<EnumItem> ToList<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .Select(e => new EnumItem((int)(object)e, e.ToString()))
            .ToList();
    }

    public static List<EnumItem> ToList(this Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException("Type must be an enum.", nameof(enumType));

        return Enum.GetValues(enumType)
            .Cast<object>()
            .Select(e => new EnumItem(Convert.ToInt32(e), e.ToString() ?? string.Empty))
            .ToList();
    }
}