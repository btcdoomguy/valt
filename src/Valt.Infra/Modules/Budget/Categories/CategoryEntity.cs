using LiteDB;

namespace Valt.Infra.Modules.Budget.Categories;

public class CategoryEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;

    [BsonField("name")] public string Name { get; set; } = null!;

    [BsonField("icon")] public string? Icon { get; set; }
    
    [BsonField("parentId")] public ObjectId? ParentId { get; set; }
}