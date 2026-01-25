using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Categories.Commands.CreateCategory;

public record CreateCategoryCommand : ICommand<CreateCategoryResult>
{
    public required string Name { get; init; }
    public required string IconId { get; init; }
    public string? ParentId { get; init; }
}

public record CreateCategoryResult(string CategoryId);
