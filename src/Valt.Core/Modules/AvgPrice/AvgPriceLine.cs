using Valt.Core.Common;
using Valt.Core.Kernel;

namespace Valt.Core.Modules.AvgPrice;

public class AvgPriceLine : Entity<AvgPriceLineId>
{
    public DateOnly Date { get; protected set; }
    public int DisplayOrder { get; protected set; }
    public AvgPriceLineTypes Type { get; protected set; }
    public decimal Quantity { get; protected set; }
    public FiatValue Amount { get; protected set; }
    public FiatValue UnitPrice => Quantity != 0 ? Amount.Value / Quantity : FiatValue.Empty;
    public string Comment { get; protected set; }
    public LineTotals Totals { get; protected set; }

    private AvgPriceLine(AvgPriceLineId id, DateOnly date, int displayOrder, AvgPriceLineTypes type, decimal quantity,
        FiatValue amount, string comment, LineTotals totals)
    {
        Id = id;
        Date = date;
        DisplayOrder = displayOrder;
        Type = type;
        Quantity = quantity;
        Amount = amount;
        Comment = comment;
        Totals = totals;
    }

    public static AvgPriceLine Create(AvgPriceLineId id, DateOnly date, int displayOrder, AvgPriceLineTypes type,
        decimal quantity,
        FiatValue amount, string comment, LineTotals totals)
    {
        return new AvgPriceLine(id, date, displayOrder, type, quantity, amount, comment, totals);
    }

    public static AvgPriceLine New(DateOnly date, int displayOrder, AvgPriceLineTypes type, decimal quantity,
        FiatValue amount, string comment)
    {
        return new AvgPriceLine(new AvgPriceLineId(), date, displayOrder, type, quantity, amount, comment,
            LineTotals.Empty);
    }

    internal void SetLineTotals(LineTotals totals)
    {
        Totals = totals;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}