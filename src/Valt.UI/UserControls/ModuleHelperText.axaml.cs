using Avalonia;
using Avalonia.Controls;

namespace Valt.UI.UserControls;

public partial class ModuleHelperText : UserControl
{
    public static readonly StyledProperty<object?> HelpContentProperty =
        AvaloniaProperty.Register<ModuleHelperText, object?>(nameof(HelpContent));

    public object? HelpContent
    {
        get => GetValue(HelpContentProperty);
        set => SetValue(HelpContentProperty, value);
    }

    public ModuleHelperText()
    {
        InitializeComponent();
    }
}
