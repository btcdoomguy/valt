using Valt.Core.Common;
using Valt.Core.Kernel;

namespace Valt.Core.Modules.AvgPrice;

public class AvgPriceLine : Entity<AvgPriceLineId>
{
    public DateOnly Date { get; protected set; }
    public int DisplayOrder { get; protected set; }
    public AvgPriceLineTypes Type { get; protected set; }
    public decimal Quantity { get; protected set; }
    public FiatValue UnitPrice { get; protected set; }
    public string Comment { get; protected set; }
    public LineTotals Totals { get; protected set; }

    private AvgPriceLine(AvgPriceLineId id, DateOnly date, int displayOrder, AvgPriceLineTypes type, decimal quantity,
        FiatValue unitPrice, string comment, LineTotals totals)
    {
        Id = id;
        Date = date;
        DisplayOrder = displayOrder;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Comment = comment;
        Totals = totals;
    }

    public static AvgPriceLine Create(AvgPriceLineId id, DateOnly date, int displayOrder, AvgPriceLineTypes type,
        decimal quantity,
        FiatValue unitPrice, string comment, LineTotals totals)
    {
        return new AvgPriceLine(id, date, displayOrder, type, quantity, unitPrice, comment, totals);
    }

    public static AvgPriceLine New(DateOnly date, int displayOrder, AvgPriceLineTypes type, decimal quantity,
        FiatValue unitPrice, string comment)
    {
        return new AvgPriceLine(new AvgPriceLineId(), date, displayOrder, type, quantity, unitPrice, comment,
            LineTotals.Empty);
    }

    internal void SetLineTotals(LineTotals totals)
    {
        Totals = totals;
    }
}