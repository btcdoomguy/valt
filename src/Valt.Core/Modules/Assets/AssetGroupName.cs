namespace Valt.Core.Modules.Assets;

public record AssetGroupName
{
    public string Value { get; }

    private AssetGroupName(string value)
    {
        Value = value;
    }

    public static AssetGroupName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Asset group name cannot be empty.", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("Asset group name must be 50 characters or less.", nameof(value));

        return new AssetGroupName(value);
    }

    public static implicit operator string(AssetGroupName name) => name.Value;

    public static implicit operator AssetGroupName(string name) => AssetGroupName.New(name);
}
