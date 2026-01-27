using System;
using Avalonia.Controls;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class GoalsPanelViewModel
{
    public GoalsPanelViewModel()
    {
        if (!Design.IsDesignMode) return;

        var currentDate = DateOnly.FromDateTime(DateTime.Now);
        var mainFiatCurrency = FiatCurrency.Brl.Code;

        // Create sample goal DTOs for design-time
        var stackBitcoinGoalDto = new GoalDTO
        {
            Id = "design-1",
            RefDate = currentDate,
            Period = (int)GoalPeriods.Monthly,
            Progress = 45.5m,
            State = (int)GoalStates.Open,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new StackBitcoinGoalTypeOutputDTO
            {
                TargetSats = 1000000,
                CalculatedSats = 455000
            }
        };

        var spendingLimitGoalDto = new GoalDTO
        {
            Id = "design-2",
            RefDate = currentDate,
            Period = (int)GoalPeriods.Monthly,
            Progress = 100.0m,
            State = (int)GoalStates.Completed,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new SpendingLimitGoalTypeOutputDTO
            {
                TargetAmount = 5000m,
                CalculatedSpending = 5000m
            }
        };

        var dcaGoalDto = new GoalDTO
        {
            Id = "design-3",
            RefDate = currentDate,
            Period = (int)GoalPeriods.Yearly,
            Progress = 50.0m,
            State = (int)GoalStates.Open,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new DcaGoalTypeOutputDTO
            {
                TargetPurchaseCount = 12,
                CalculatedPurchaseCount = 6
            }
        };

        GoalEntries =
        [
            new GoalEntryViewModel(stackBitcoinGoalDto, mainFiatCurrency),
            new GoalEntryViewModel(spendingLimitGoalDto, mainFiatCurrency),
            new GoalEntryViewModel(dcaGoalDto, mainFiatCurrency),
        ];
    }
}
