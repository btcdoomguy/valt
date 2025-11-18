using System.Drawing;

namespace Valt.Infra.Modules.Budget.Categories.Queries.DTOs;

public record CategoryDTO
{
    public string Id { get; init; } = null!;
    public string Name { get; set; } = null!;
    public string SimpleName { get; set; } = null!;
    public string? Icon { get; set; }
    public char Unicode { get; set; }
    public Color Color { get; set; }

    public override string ToString() => SimpleName;
}