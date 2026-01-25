using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand : ICommand<Unit>
{
    public required string CategoryId { get; init; }
}
