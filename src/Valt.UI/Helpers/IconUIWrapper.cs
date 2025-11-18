using Avalonia.Media;
using Valt.Core.Common;

namespace Valt.UI.Helpers;

public class IconUIWrapper
{
    private readonly Icon _icon;

    public IconUIWrapper(Icon icon)
    {
        _icon = icon;
    }

    public SolidColorBrush BrushColor => new(Color.FromArgb(_icon.Color.A, _icon.Color.R, _icon.Color.G, _icon.Color.B));

    public char Unicode => _icon.Unicode;
}