using System;
using Avalonia.Controls;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class GoalsPanelViewModel
{
    public GoalsPanelViewModel()
    {
        if (!Design.IsDesignMode) return;

        var currentDate = DateOnly.FromDateTime(DateTime.Now);
        var mainFiatCurrency = FiatCurrency.Brl.Code;

        // Create sample goals for design-time
        var stackBitcoinGoalType = new StackBitcoinGoalType(BtcValue.ParseSats(1000000));
        var stackBitcoinGoal = Goal.New(currentDate, GoalPeriods.Monthly, stackBitcoinGoalType);
        stackBitcoinGoal.UpdateProgress(45.5m, stackBitcoinGoalType, DateTime.Now);

        var spendingLimitGoalType = new SpendingLimitGoalType(5000m);
        var spendingLimitGoal = Goal.New(currentDate, GoalPeriods.Monthly, spendingLimitGoalType);
        spendingLimitGoal.UpdateProgress(100.0m, spendingLimitGoalType, DateTime.Now);

        var dcaGoalType = new DcaGoalType(12);
        var dcaGoal = Goal.New(currentDate, GoalPeriods.Yearly, dcaGoalType);
        dcaGoal.UpdateProgress(50.0m, dcaGoalType, DateTime.Now);

        GoalEntries =
        [
            new GoalEntryViewModel(stackBitcoinGoal, mainFiatCurrency),
            new GoalEntryViewModel(spendingLimitGoal, mainFiatCurrency),
            new GoalEntryViewModel(dcaGoal, mainFiatCurrency),
        ];
    }
}
