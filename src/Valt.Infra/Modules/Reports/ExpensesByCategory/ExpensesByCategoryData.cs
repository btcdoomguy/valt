using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

public record ExpensesByCategoryData
{
    public required FiatCurrency MainCurrency { get; init; }
    public required IReadOnlyList<Item> Items { get; init; }
    
    public record Item
    {
        public required CategoryId CategoryId { get; init; }
        public required Icon Icon { get; init; }
        public required string CategoryName { get; init; }
        public required decimal FiatTotal { get; init; }
    }
}