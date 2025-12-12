using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Tests.Builders;

public class AvgPriceProfileBuilder
{
    private AvgPriceProfileId _id = new();
    private AvgPriceProfileName _name = "Test Profile";
    private bool _visible = true;
    private Icon _icon = Icon.Empty;
    private FiatCurrency _currency = FiatCurrency.Usd;
    private AvgPriceCalculationMethod _calculationMethod = AvgPriceCalculationMethod.BrazilianRule;
    private List<AvgPriceLine> _avgPriceLines = new();

    public AvgPriceProfileBuilder WithId(AvgPriceProfileId id)
    {
        _id = id;
        return this;
    }

    public AvgPriceProfileBuilder WithName(AvgPriceProfileName name)
    {
        _name = name;
        return this;
    }

    public AvgPriceProfileBuilder WithVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public AvgPriceProfileBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public AvgPriceProfileBuilder WithCurrency(FiatCurrency currency)
    {
        _currency = currency;
        return this;
    }

    public AvgPriceProfileBuilder WithCalculationMethod(AvgPriceCalculationMethod calculationMethod)
    {
        _calculationMethod = calculationMethod;
        return this;
    }

    public AvgPriceProfileBuilder WithLines(params AvgPriceLine[] lines)
    {
        _avgPriceLines = lines.ToList();
        return this;
    }

    public AvgPriceProfileBuilder WithLines(IEnumerable<AvgPriceLine> lines)
    {
        _avgPriceLines = lines.ToList();
        return this;
    }

    public AvgPriceProfile Build()
    {
        return AvgPriceProfile.Create(_id, _name, _visible, _icon, _currency, _calculationMethod, _avgPriceLines);
    }

    public static AvgPriceProfileBuilder AProfile() => new AvgPriceProfileBuilder();

    public static AvgPriceProfileBuilder ABrazilianRuleProfile() =>
        new AvgPriceProfileBuilder()
            .WithCalculationMethod(AvgPriceCalculationMethod.BrazilianRule)
            .WithCurrency(FiatCurrency.Brl);
}
