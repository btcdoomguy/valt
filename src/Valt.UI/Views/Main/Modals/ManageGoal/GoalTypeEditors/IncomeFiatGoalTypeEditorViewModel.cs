using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Settings;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class IncomeFiatGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly CurrencySettings? _currencySettings;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    public string Description => language.GoalType_IncomeFiat_Description;

    public string MainFiatCurrency =>
        _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    public IncomeFiatGoalTypeEditorViewModel()
    {
    }

    public IncomeFiatGoalTypeEditorViewModel(CurrencySettings currencySettings)
    {
        _currencySettings = currencySettings;
    }

    public IGoalType CreateGoalType()
    {
        return new IncomeFiatGoalType(TargetFiatAmount.Value);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is IncomeFiatGoalType incomeFiat)
        {
            return new IncomeFiatGoalType(TargetFiatAmount.Value, incomeFiat.CalculatedIncome);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is IncomeFiatGoalType incomeFiat)
        {
            TargetFiatAmount = FiatValue.New(incomeFiat.TargetAmount);
        }
    }
}
