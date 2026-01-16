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
                GoalTypeNames.SpendingLimit => language.GoalType_SpendingLimit,
                GoalTypeNames.Dca => language.GoalType_Dca,
                _ => _goal.GoalType.TypeName.ToString()
            };

            return typeName;
        }
    }

    public decimal Progress => _goal.Progress;

    /// <summary>
    /// Progress is already stored as a percentage (0-100) by the calculator
    /// </summary>
    public decimal ProgressPercentage => Math.Min(_goal.Progress, 100);

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
                SpendingLimitGoalType spendingLimit => CurrencyDisplay.FormatFiat(spendingLimit.TargetAmount, spendingLimit.Currency),
                DcaGoalType dca => dca.TargetPurchaseCount.ToString(),
                _ => string.Empty
            };
        }
    }

    public string Description
    {
        get
        {
            return _goal.GoalType switch
            {
                StackBitcoinGoalType stackBitcoin => string.Format(
                    language.GoalDescription_StackBitcoin,
                    CurrencyDisplay.FormatSatsAsNumber(Math.Min(stackBitcoin.CalculatedSats, stackBitcoin.TargetAmount.Sats)),
                    CurrencyDisplay.FormatSatsAsNumber(stackBitcoin.TargetAmount.Sats)),
                SpendingLimitGoalType spendingLimit => string.Format(
                    language.GoalDescription_SpendingLimit,
                    CurrencyDisplay.FormatFiat(Math.Min(spendingLimit.CalculatedSpending, spendingLimit.TargetAmount), spendingLimit.Currency),
                    CurrencyDisplay.FormatFiat(spendingLimit.TargetAmount, spendingLimit.Currency)),
                DcaGoalType dca => string.Format(
                    language.GoalDescription_Dca,
                    Math.Min(dca.CalculatedPurchaseCount, dca.TargetPurchaseCount),
                    dca.TargetPurchaseCount),
                _ => string.Empty
            };
        }
    }

    public bool IsCompleted => _goal.State == GoalStates.MarkedAsCompleted || _goal.Progress >= 100m;

    public GoalStates State => _goal.State;

    public GoalPeriods Period => _goal.Period;

    public DateOnly RefDate => _goal.RefDate;

    // State-based UI properties
    public bool IsClosed => _goal.State == GoalStates.Closed;
    public bool IsMarkedAsComplete => _goal.State == GoalStates.MarkedAsCompleted;
    public bool IsOpen => _goal.State == GoalStates.Open;
    public bool IsProgressComplete => _goal.State == GoalStates.Completed || _goal.Progress >= 100m;

    // Context menu visibility
    public bool CanClose => _goal.State == GoalStates.Open;
    public bool CanConclude => _goal.State == GoalStates.Completed || (_goal.State == GoalStates.Open && _goal.Progress >= 100m);
    public bool CanReopen => _goal.State == GoalStates.MarkedAsCompleted || _goal.State == GoalStates.Closed;

    // Show progress bar only for Open and Completed states (not for MarkedAsCompleted or Closed)
    public bool ShowProgressBar => _goal.State != GoalStates.MarkedAsCompleted;
}
