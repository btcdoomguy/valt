using Valt.Core.Common;
using Valt.Core.Kernel;
using Valt.Core.Modules.AvgPrice.CalculationStrategies;
using Valt.Core.Modules.AvgPrice.Events;

namespace Valt.Core.Modules.AvgPrice;

public class AvgPriceProfile : AggregateRoot<AvgPriceProfileId>
{
    private HashSet<AvgPriceLine> _avgPriceLines = new();
    private AvgPriceCalculationMethod _calculationMethod;
    private IAvgPriceCalculationStrategy _calculationStrategy;

    public AvgPriceProfileName Name { get; protected set; }
    public AvgPriceAsset Asset { get; protected set; }
    public bool Visible { get; protected set; }
    public Icon Icon { get; protected set; }
    public FiatCurrency Currency { get; protected set; }

    public AvgPriceCalculationMethod CalculationMethod
    {
        get => _calculationMethod;
        set
        {
            _calculationMethod = value;

            switch (_calculationMethod)
            {
                case AvgPriceCalculationMethod.BrazilianRule:
                    _calculationStrategy = new BrazilianRuleCalculationStrategy(this);
                    break;
                case AvgPriceCalculationMethod.Fifo:
                    _calculationStrategy = new FifoCalculationStrategy(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public IReadOnlyCollection<AvgPriceLine> AvgPriceLines => _avgPriceLines;

    private AvgPriceProfile(AvgPriceProfileId id, AvgPriceProfileName name, AvgPriceAsset asset, bool visible,
        Icon icon,
        FiatCurrency currency, AvgPriceCalculationMethod calculationMethod, IEnumerable<AvgPriceLine> avgPriceLines,
        int version)
    {
        Id = id;
        Name = name;
        Asset = asset;
        Visible = visible;
        Icon = icon;
        Currency = currency;
        CalculationMethod = calculationMethod;
        Version = version;

        _avgPriceLines = new HashSet<AvgPriceLine>(avgPriceLines);

        if (Version == 0)
            AddEvent(new AvgPriceProfileCreatedEvent(this));
    }

    public static AvgPriceProfile Create(AvgPriceProfileId id, AvgPriceProfileName name, AvgPriceAsset asset,
        bool visible, Icon icon,
        FiatCurrency currency, AvgPriceCalculationMethod calculationMethod, IEnumerable<AvgPriceLine> avgPriceLines,
        int version)
    {
        return new AvgPriceProfile(id, name, asset, visible, icon, currency, calculationMethod, avgPriceLines, version);
    }

    public static AvgPriceProfile New(AvgPriceProfileName name, AvgPriceAsset asset, bool visible, Icon icon,
        FiatCurrency currency,
        AvgPriceCalculationMethod calculationMethod)
    {
        return new AvgPriceProfile(new AvgPriceProfileId(), name, asset, visible, icon, currency, calculationMethod,
            Enumerable.Empty<AvgPriceLine>(), 0);
    }

    private void RecalculateAll()
    {
        var orderedList = _avgPriceLines.OrderBy(x => x.Date).ThenBy(x => x.DisplayOrder).ToList();

        Recalculate(orderedList);
    }

    public void AddLine(DateOnly date, int displayOrder, AvgPriceLineTypes type, decimal quantity, FiatValue fiatValue,
        string comment)
    {
        var copiedList = _avgPriceLines.ToList();

        var newLine = AvgPriceLine.New(date, displayOrder, type, quantity, fiatValue, comment);
        copiedList.Add(newLine);

        var orderedList = copiedList.OrderBy(x => x.Date).ThenBy(x => x.DisplayOrder).ToList();

        Recalculate(orderedList);

        _avgPriceLines.Add(newLine);

        AddEvent(new AvgPriceLineCreatedEvent(newLine));
    }

    public void RemoveLine(AvgPriceLine line)
    {
        _avgPriceLines.Remove(line);

        var orderedList = _avgPriceLines.OrderBy(x => x.Date).ThenBy(x => x.DisplayOrder).ToList();

        Recalculate(orderedList);

        AddEvent(new AvgPriceLineDeletedEvent(line));
    }

    public void MoveLineUp(AvgPriceLine line)
    {
        var linesFromSameDate = _avgPriceLines.Where(x => x.Date == line.Date).OrderBy(x => x.DisplayOrder).ToList();

        var indexOfLine = linesFromSameDate.IndexOf(line);

        linesFromSameDate.Remove(line);
        linesFromSameDate.Insert(indexOfLine - 1, line);
        
        if (RearrangeDisplayOrder(linesFromSameDate))
            RecalculateAll();
    }

    public void MoveLineDown(AvgPriceLine line)
    {
        var linesFromSameDate = _avgPriceLines.Where(x => x.Date == line.Date).OrderBy(x => x.DisplayOrder).ToList();

        var indexOfLine = linesFromSameDate.IndexOf(line);

        linesFromSameDate.Remove(line);
        linesFromSameDate.Insert(indexOfLine + 1, line);

        if (RearrangeDisplayOrder(linesFromSameDate))
            RecalculateAll();
    }

    private bool RearrangeDisplayOrder(IEnumerable<AvgPriceLine> lines)
    {
        var rearranged = false;
        
        var displayOrder = 0;
        foreach (var lineFromSameDate in lines)
        {
            if (lineFromSameDate.DisplayOrder != displayOrder)
            {
                lineFromSameDate.SetDisplayOrder(displayOrder);
                AddEvent(new AvgPriceLineUpdatedEvent(lineFromSameDate));
                rearranged = true;
            }

            displayOrder++;
        }

        return rearranged;
    }

    public void ChangeLineTotals(AvgPriceLine line, LineTotals lineTotals)
    {
        if (line.Totals == lineTotals)
            return;

        line.SetLineTotals(lineTotals);
        AddEvent(new AvgPriceLineUpdatedEvent(line));
    }

    private void Recalculate(IEnumerable<AvgPriceLine> orderedList)
    {
        _calculationStrategy.CalculateTotals(orderedList);
    }

    public void Rename(AvgPriceProfileName name)
    {
        if (Name == name)
            return;

        Name = name;

        AddEvent(new AvgPriceProfileUpdatedEvent(this));
    }

    public void ChangeIcon(Icon icon)
    {
        if (Icon == icon)
            return;

        Icon = icon;

        AddEvent(new AvgPriceProfileUpdatedEvent(this));
    }

    public void ChangeAsset(string assetName, int precision)
    {
        var newAsset = new AvgPriceAsset(assetName, precision);

        if (Asset == newAsset)
            return;

        Asset = newAsset;

        RecalculateAll();

        AddEvent(new AvgPriceProfileUpdatedEvent(this));
    }

    public void ChangeCalculationMethod(AvgPriceCalculationMethod calculationMethod)
    {
        if (CalculationMethod == calculationMethod)
            return;

        CalculationMethod = calculationMethod;

        RecalculateAll();

        AddEvent(new AvgPriceProfileUpdatedEvent(this));
    }

    public void ChangeVisibility(bool visible)
    {
        if (Visible == visible)
            return;

        Visible = visible;

        AddEvent(new AvgPriceProfileUpdatedEvent(this));
    }
}