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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public IReadOnlyCollection<AvgPriceLine> AvgPriceLines => _avgPriceLines;

    private AvgPriceProfile(AvgPriceProfileId id, AvgPriceProfileName name, bool visible, Icon icon,
        FiatCurrency currency, AvgPriceCalculationMethod calculationMethod, IEnumerable<AvgPriceLine> avgPriceLines,
        int version)
    {
        Id = id;
        Name = name;
        Visible = visible;
        Icon = icon;
        Currency = currency;
        CalculationMethod = calculationMethod;
        Version = version;

        _avgPriceLines = new HashSet<AvgPriceLine>(avgPriceLines);

        if (Version == 0)
            AddEvent(new AvgPriceProfileCreatedEvent(this));
    }

    public static AvgPriceProfile Create(AvgPriceProfileId id, AvgPriceProfileName name, bool visible, Icon icon,
        FiatCurrency currency, AvgPriceCalculationMethod calculationMethod, IEnumerable<AvgPriceLine> avgPriceLines,
        int version)
    {
        return new AvgPriceProfile(id, name, visible, icon, currency, calculationMethod, avgPriceLines, version);
    }

    public static AvgPriceProfile New(AvgPriceProfileName name, bool visible, Icon icon, FiatCurrency currency,
        AvgPriceCalculationMethod calculationMethod)
    {
        return new AvgPriceProfile(new AvgPriceProfileId(), name, visible, icon, currency, calculationMethod,
            Enumerable.Empty<AvgPriceLine>(), 0);
    }

    public void AddLine(DateOnly date, int displayOrder, AvgPriceLineTypes type, BtcValue btcValue, FiatValue fiatValue,
        string comment)
    {
        var copiedList = _avgPriceLines.ToList();

        var newLine = AvgPriceLine.New(date, displayOrder, type, btcValue, fiatValue, comment);
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
}