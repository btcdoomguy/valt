using Avalonia;

namespace Valt.UI.Helpers;

/// <summary>
/// A helper class that allows binding to a DataContext from resources.
/// This is useful for context menus and popups that are in a different visual tree.
/// </summary>
public class BindingProxy : AvaloniaObject
{
    public static readonly StyledProperty<object?> DataProperty =
        AvaloniaProperty.Register<BindingProxy, object?>(nameof(Data));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
}
