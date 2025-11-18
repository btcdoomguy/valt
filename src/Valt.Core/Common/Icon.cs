using System.Drawing;

namespace Valt.Core.Common;

public record Icon(string Source, string Name, char Unicode, Color Color)
{
    public static Icon Empty = new("", "", char.MinValue, Color.FromArgb(255, 255, 255, 255));

    public static Icon RestoreFromId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Empty;

        var parts = id.Split(";");

        if (parts.Length != 4)
            return Empty;

        return new Icon(parts[0], parts[1], parts[2][0], Color.FromArgb(int.Parse(parts[3])));
    }

    private string ToId() => $"{Source};{Name};{Unicode};{Color.ToArgb()}";

    public override string ToString() => ToId();
}