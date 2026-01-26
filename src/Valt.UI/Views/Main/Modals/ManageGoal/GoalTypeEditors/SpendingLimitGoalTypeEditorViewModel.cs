using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Settings;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class SpendingLimitGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly CurrencySettings? _currencySettings;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    public string Description => language.GoalType_SpendingLimit_Description;

    public string MainFiatCurrency =>
        _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    public SpendingLimitGoalTypeEditorViewModel()
    {
    }

    public SpendingLimitGoalTypeEditorViewModel(CurrencySettings currencySettings)
    {
        _currencySettings = currencySettings;
    }

    public IGoalType CreateGoalType()
    {
        return new SpendingLimitGoalType(TargetFiatAmount.Value);
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is SpendingLimitGoalType spendingLimit)
        {
            return new SpendingLimitGoalType(TargetFiatAmount.Value, spendingLimit.CalculatedSpending);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is SpendingLimitGoalType spendingLimit)
        {
            TargetFiatAmount = FiatValue.New(spendingLimit.TargetAmount);
        }
    }

    public GoalTypeInputDTO CreateGoalTypeDTO()
    {
        return new SpendingLimitGoalTypeDTO { TargetAmount = TargetFiatAmount.Value };
    }

    public void LoadFromDTO(GoalTypeOutputDTO goalType)
    {
        if (goalType is SpendingLimitGoalTypeOutputDTO spendingLimit)
        {
            TargetFiatAmount = FiatValue.New(spendingLimit.TargetAmount);
        }
    }
}
