using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.SimulatedPricesConfig;

public partial class SimulatedPriceLineViewModel : ObservableObject
{
    [ObservableProperty] private SimulatedPriceTypeOption _selectedTypeOption;
    [ObservableProperty] private string _valueText = string.Empty;

    public static List<SimulatedPriceTypeOption> TypeOptions { get; } =
    [
        new(SimulatedPriceType.Percentage, language.SimulatedPrices_Config_Type_Percentage),
        new(SimulatedPriceType.Fixed, language.SimulatedPrices_Config_Type_Fixed)
    ];

    public SimulatedPriceLineViewModel()
    {
        _selectedTypeOption = TypeOptions[0];
        ValueText = "100";
    }

    public SimulatedPriceLineViewModel(SimulatedPriceLineConfig config)
    {
        _selectedTypeOption = config.Type == SimulatedPriceType.Fixed ? TypeOptions[1] : TypeOptions[0];
        ValueText = config.Value.ToString("G");
    }

    public SimulatedPriceLineConfig? ToConfig()
    {
        if (!decimal.TryParse(ValueText, out var value) || value <= 0)
            return null;

        if (SelectedTypeOption.Type == SimulatedPriceType.Percentage && value < 5)
            return null;

        return new SimulatedPriceLineConfig(SelectedTypeOption.Type, value);
    }
}

public record SimulatedPriceTypeOption(SimulatedPriceType Type, string DisplayName)
{
    public override string ToString() => DisplayName;
}
