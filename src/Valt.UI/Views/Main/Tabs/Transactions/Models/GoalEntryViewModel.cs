using System;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public class GoalEntryViewModel
{
    private readonly Goal _goal;

    public GoalEntryViewModel(Goal goal)
    {
        _goal = goal;
    }

    public string Id => _goal.Id.ToString();

    public string FriendlyName
    {
        get
        {
            var typeName = _goal.GoalType.TypeName switch
            {
                GoalTypeNames.StackBitcoin => language.GoalType_StackBitcoin,
                _ => _goal.GoalType.TypeName.ToString()
            };

            var periodDescription = _goal.Period switch
            {
                GoalPeriods.Monthly => $"{_goal.RefDate:MMM/yyyy}",
                GoalPeriods.Yearly => _goal.RefDate.Year.ToString(),
                _ => string.Empty
            };

            return $"{typeName} ({periodDescription})";
        }
    }

    public decimal Progress => _goal.Progress;

    public decimal ProgressPercentage => Math.Min(_goal.Progress * 100, 100);

    public string ProgressDisplay
    {
        get
        {
            var percentage = ProgressPercentage;
            return $"{percentage:F1}%";
        }
    }

    public string TargetDisplay
    {
        get
        {
            return _goal.GoalType switch
            {
                StackBitcoinGoalType stackBitcoin => CurrencyDisplay.FormatSatsAsBitcoin(stackBitcoin.TargetAmount.Sats),
                _ => string.Empty
            };
        }
    }

    public bool IsCompleted => _goal.State == GoalStates.MarkedAsCompleted || _goal.Progress >= 1m;

    public GoalStates State => _goal.State;

    public GoalPeriods Period => _goal.Period;

    public DateOnly RefDate => _goal.RefDate;
}
