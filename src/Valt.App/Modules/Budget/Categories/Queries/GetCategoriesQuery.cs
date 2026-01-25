using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.App.Modules.Budget.Categories.Queries;

public record GetCategoriesQuery : IQuery<CategoriesDTO>;
