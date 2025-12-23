using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Tests.Builders;

public class AvgPriceLineBuilder
{
    private AvgPriceLineId? _id;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private int _displayOrder = 0;
    private AvgPriceLineTypes _type = AvgPriceLineTypes.Buy;
    private decimal _quantity = 0;
    private FiatValue _bitcoinUnitPrice = FiatValue.Empty;
    private string _comment = string.Empty;
    private LineTotals _totals = LineTotals.Empty;

    public AvgPriceLineBuilder WithId(AvgPriceLineId id)
    {
        _id = id;
        return this;
    }

    public AvgPriceLineBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public AvgPriceLineBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public AvgPriceLineBuilder WithType(AvgPriceLineTypes type)
    {
        _type = type;
        return this;
    }

    public AvgPriceLineBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    public AvgPriceLineBuilder WithUnitPrice(FiatValue bitcoinUnitPrice)
    {
        _bitcoinUnitPrice = bitcoinUnitPrice;
        return this;
    }

    public AvgPriceLineBuilder WithComment(string comment)
    {
        _comment = comment;
        return this;
    }

    public AvgPriceLineBuilder WithTotals(LineTotals totals)
    {
        _totals = totals;
        return this;
    }

    public AvgPriceLine Build()
    {
        return AvgPriceLine.Create(_id ?? new AvgPriceLineId(), _date, _displayOrder, _type, _quantity, _bitcoinUnitPrice, _comment, _totals);
    }

    public static AvgPriceLineBuilder ABuyLine() => new AvgPriceLineBuilder().WithType(AvgPriceLineTypes.Buy);
    public static AvgPriceLineBuilder ASellLine() => new AvgPriceLineBuilder().WithType(AvgPriceLineTypes.Sell);
    public static AvgPriceLineBuilder ASetupLine() => new AvgPriceLineBuilder().WithType(AvgPriceLineTypes.Setup);
}
