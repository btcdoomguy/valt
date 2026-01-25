using System.Drawing;

namespace Valt.App.Modules.Budget.Categories.DTOs;

public record CategoryDTO
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SimpleName { get; init; }
    public string? IconId { get; init; }
    public char Unicode { get; init; }
    public Color Color { get; init; }

    public override string ToString() => SimpleName;
}
