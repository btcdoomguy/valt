using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Settings;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class SaveFiatGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly CurrencySettings? _currencySettings;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    public string Description => language.GoalType_SaveFiat_Description;

    public string MainFiatCurrency =>
        _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    public SaveFiatGoalTypeEditorViewModel()
    {
    }

    public SaveFiatGoalTypeEditorViewModel(CurrencySettings currencySettings)
    {
        _currencySettings = currencySettings;
    }

    public IGoalType CreateGoalType()
    {
        return new SaveFiatGoalType(TargetFiatAmount.Value);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is SaveFiatGoalType saveFiat)
        {
            return new SaveFiatGoalType(TargetFiatAmount.Value, saveFiat.CalculatedSavings);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is SaveFiatGoalType saveFiat)
        {
            TargetFiatAmount = FiatValue.New(saveFiat.TargetAmount);
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new SaveFiatGoalTypeDTO { TargetAmount = TargetFiatAmount.Value };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is SaveFiatGoalTypeOutputDTO saveFiat)
        {
            TargetFiatAmount = FiatValue.New(saveFiat.TargetAmount);
        }
    }
}
