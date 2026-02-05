namespace Valt.Core.Modules.Assets;

public sealed class AssetName
{
    public string Value { get; }

    public AssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Asset name cannot be null or empty", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("Asset name cannot exceed 100 characters", nameof(value));

        Value = value.Trim();
    }

    public static implicit operator string(AssetName name) => name.Value;

    public static implicit operator AssetName(string name) => new(name);

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is AssetName other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
