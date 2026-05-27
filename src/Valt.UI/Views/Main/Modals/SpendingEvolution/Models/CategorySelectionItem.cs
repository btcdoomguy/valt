using System.Drawing;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.UI.Views.Main.Modals.SpendingEvolution.Models;

public partial class CategorySelectionItem : ObservableObject
{
    public AvaloniaList<CategorySelectionItem> SubNodes { get; set; } = new();
    public string Id { get; }
    public string? ParentId { get; set; }
    public string Name { get; set; }
    public char Unicode { get; set; }
    public Color Color { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    public CategorySelectionItem(string id, string? parentId, string name, char unicode, Color color)
    {
        Id = id;
        ParentId = parentId;
        Name = name;
        Unicode = unicode;
        Color = color;

        // When IsSelected changes, propagate to children
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IsSelected))
            {
                foreach (var child in SubNodes)
                    child.IsSelected = IsSelected;
            }
        };
    }
}
