using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Helpers;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class IncomeFiatGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly IConfigurationManager? _configurationManager;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    [ObservableProperty]
    private string _selectedCurrency = FiatCurrency.Usd.Code;

    public string Description => language.GoalType_IncomeFiat_Description;

    public List<ComboBoxValue> AvailableCurrencies =>
        (_configurationManager?.GetAvailableFiatCurrencies() ?? FiatCurrency.GetAll().Select(c => c.Code).ToList())
            .Select(c => new ComboBoxValue(c, c))
            .ToList();

    public IncomeFiatGoalTypeEditorViewModel()
    {
    }

    public IncomeFiatGoalTypeEditorViewModel(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
        SelectedCurrency = AvailableCurrencies.FirstOrDefault()?.Value ?? FiatCurrency.Usd.Code;
    }

    public IGoalType CreateGoalType()
    {
        return new IncomeFiatGoalType(TargetFiatAmount.Value, SelectedCurrency);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is IncomeFiatGoalType incomeFiat)
        {
            return new IncomeFiatGoalType(TargetFiatAmount.Value, SelectedCurrency, incomeFiat.CalculatedIncome);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is IncomeFiatGoalType incomeFiat)
        {
            TargetFiatAmount = FiatValue.New(incomeFiat.TargetAmount);
            SelectedCurrency = incomeFiat.Currency;
        }
    }
}
