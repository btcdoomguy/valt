using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Categories.Commands.ChangeCategoryParent;

public record ChangeCategoryParentCommand : ICommand<Unit>
{
    public required string CategoryId { get; init; }
    public string? NewParentId { get; init; }
}
