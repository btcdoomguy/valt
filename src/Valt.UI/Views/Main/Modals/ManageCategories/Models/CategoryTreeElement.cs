using System.Drawing;
using Avalonia.Collections;

namespace Valt.UI.Views.Main.Modals.ManageCategories.Models;

public record CategoryTreeElement
{
    public AvaloniaList<CategoryTreeElement> SubNodes { get; set; } = [];
    public string Id { get; }
    public string? ParentId { get; set; }
    public string Name { get; set; }
    public char Unicode { get; set; }
    public Color Color { get; set; }
  
    public CategoryTreeElement(string id, string? parentId, string name, char unicode, Color color)
    {
        Id = id;
        ParentId = parentId;
        Name = name;
        Unicode = unicode;
        Color = color;
    }

    public CategoryTreeElement(string id, string? parentId, string name, char unicode, Color color, AvaloniaList<CategoryTreeElement> subNodes)
    {
        Id = id;
        ParentId = parentId;
        Name = name;
        Unicode = unicode;
        Color = color;
        SubNodes = subNodes;
    }
}