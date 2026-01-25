using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Categories.Commands.EditCategory;

public record EditCategoryCommand : ICommand<Unit>
{
    public required string CategoryId { get; init; }
    public required string Name { get; init; }
    public required string IconId { get; init; }
}
