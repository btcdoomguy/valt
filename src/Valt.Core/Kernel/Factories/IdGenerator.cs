namespace Valt.Core.Kernel.Factories;

public static class IdGenerator
{
    private static IIdProvider? _idProvider;

    public static void Configure(IIdProvider idProvider)
    {
        _idProvider = idProvider;
    }

    public static string Generate()
    {
        if (_idProvider is null)
            throw new InvalidOperationException("Id provider not configured");

        return _idProvider.NewId();
    }
}