namespace Valt.App.Modules.Assets.DTOs;

public record AssetGroupDTO
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int DisplayOrder { get; init; }
}
