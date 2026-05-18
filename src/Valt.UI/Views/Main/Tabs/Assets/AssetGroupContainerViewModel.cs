using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Views.Main.Tabs.Assets.Models;

namespace Valt.UI.Views.Main.Tabs.Assets;

public partial class AssetGroupContainerViewModel : ObservableObject
{
    public string? Id { get; }
    public string? Title { get; }
    public string? Description { get; }

    [ObservableProperty]
    private AvaloniaList<AssetViewModel> _assets = new();

    public AssetGroupContainerViewModel(string? id, string? title, string? description)
    {
        Id = id;
        Title = title;
        Description = description;
    }
}

public record AssetGroupMenuItem(string Id, string Name)
{
    public override string ToString() => Name;
}
