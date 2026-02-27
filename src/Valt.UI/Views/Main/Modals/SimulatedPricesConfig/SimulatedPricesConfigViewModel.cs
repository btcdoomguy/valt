using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.SimulatedPricesConfig;

public partial class SimulatedPricesConfigViewModel : ValtModalViewModel
{
    private readonly IConfigurationManager _configurationManager = null!;

    public ObservableCollection<SimulatedPriceLineViewModel> Lines { get; } = new();

    [ObservableProperty] private bool _canAddLine = true;
    [ObservableProperty] private string? _errorMessage;

    private const int MaxLines = 6;

    public SimulatedPricesConfigViewModel() { }

    public SimulatedPricesConfigViewModel(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;

        var lines = _configurationManager.GetSimulatedPriceLines();
        foreach (var line in lines)
            Lines.Add(new SimulatedPriceLineViewModel(line));

        UpdateCanAddLine();
    }

    [RelayCommand]
    private void AddLine()
    {
        if (Lines.Count >= MaxLines)
            return;

        Lines.Add(new SimulatedPriceLineViewModel());
        UpdateCanAddLine();
    }

    [RelayCommand]
    private void RemoveLine(SimulatedPriceLineViewModel line)
    {
        Lines.Remove(line);
        UpdateCanAddLine();
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        var configs = Lines.Select(l => l.ToConfig()).ToList();

        if (configs.Any(c => c is null))
        {
            ErrorMessage = Lang.language.SimulatedPrices_Config_MinPercentage;
            return;
        }

        _configurationManager.SetSimulatedPriceLines(configs!);
        CloseDialog?.Invoke(new Response(true));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    private void UpdateCanAddLine()
    {
        CanAddLine = Lines.Count < MaxLines;
    }

    public record Response(bool Ok);
}
