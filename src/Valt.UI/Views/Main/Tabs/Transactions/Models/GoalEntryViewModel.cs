using System;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public partial class GoalEntryViewModel : ObservableObject, IDisposable
{
    private Goal _goal;
    private readonly string _mainFiatCurrency;
    private Timer? _animationTimer;
    private decimal _animationStartValue;
    private decimal _animationTargetValue;
    private DateTime _animationStartTime;
    private const int AnimationDurationMs = 3000;
    private const int AnimationIntervalMs = 16; // ~60fps

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressDisplay))]
    private decimal _animatedProgressPercentage;

    public GoalEntryViewModel(Goal goal, string mainFiatCurrency)
    {
        _goal = goal;
        _mainFiatCurrency = mainFiatCurrency;
        _animatedProgressPercentage = Math.Min(goal.Progress, 100);
    }

    public string Id => _goal.Id.ToString();

    public string FriendlyName
    {
        get
        {
            var typeName = _goal.GoalType switch
            {
                ReduceExpenseCategoryGoalType reduceExpense => $"{language.GoalType_ReduceExpenseCategory}: {reduceExpense.CategoryName}",
                _ => _goal.GoalType.TypeName switch
                {
                    GoalTypeNames.StackBitcoin => language.GoalType_StackBitcoin,
                    GoalTypeNames.SpendingLimit => language.GoalType_SpendingLimit,
                    GoalTypeNames.Dca => language.GoalType_Dca,
                    GoalTypeNames.IncomeFiat => language.GoalType_IncomeFiat,
                    GoalTypeNames.IncomeBtc => language.GoalType_IncomeBtc,
                    GoalTypeNames.BitcoinHodl => language.GoalType_BitcoinHodl,
                    _ => _goal.GoalType.TypeName.ToString()
                }
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
            var percentage = AnimatedProgressPercentage;
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
                SpendingLimitGoalType spendingLimit => CurrencyDisplay.FormatFiat(spendingLimit.TargetAmount, _mainFiatCurrency),
                DcaGoalType dca => dca.TargetPurchaseCount.ToString(),
                IncomeFiatGoalType incomeFiat => CurrencyDisplay.FormatFiat(incomeFiat.TargetAmount, _mainFiatCurrency),
                IncomeBtcGoalType incomeBtc => CurrencyDisplay.FormatSatsAsBitcoin(incomeBtc.TargetAmount.Sats),
                ReduceExpenseCategoryGoalType reduceExpense => CurrencyDisplay.FormatFiat(reduceExpense.TargetAmount, _mainFiatCurrency),
                BitcoinHodlGoalType bitcoinHodl => bitcoinHodl.MaxSellableSats == 0
                    ? language.GoalTarget_NoSales
                    : CurrencyDisplay.FormatSatsAsBitcoin(bitcoinHodl.MaxSellableSats),
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
                    CurrencyDisplay.FormatFiat(Math.Min(spendingLimit.CalculatedSpending, spendingLimit.TargetAmount), _mainFiatCurrency),
                    CurrencyDisplay.FormatFiat(spendingLimit.TargetAmount, _mainFiatCurrency)),
                DcaGoalType dca => string.Format(
                    language.GoalDescription_Dca,
                    Math.Min(dca.CalculatedPurchaseCount, dca.TargetPurchaseCount),
                    dca.TargetPurchaseCount),
                IncomeFiatGoalType incomeFiat => string.Format(
                    language.GoalDescription_IncomeFiat,
                    CurrencyDisplay.FormatFiat(Math.Min(incomeFiat.CalculatedIncome, incomeFiat.TargetAmount), _mainFiatCurrency),
                    CurrencyDisplay.FormatFiat(incomeFiat.TargetAmount, _mainFiatCurrency)),
                IncomeBtcGoalType incomeBtc => string.Format(
                    language.GoalDescription_IncomeBtc,
                    CurrencyDisplay.FormatSatsAsNumber(Math.Min(incomeBtc.CalculatedSats, incomeBtc.TargetAmount.Sats)),
                    CurrencyDisplay.FormatSatsAsNumber(incomeBtc.TargetAmount.Sats)),
                ReduceExpenseCategoryGoalType reduceExpense => string.Format(
                    language.GoalDescription_ReduceExpenseCategory,
                    CurrencyDisplay.FormatFiat(Math.Min(reduceExpense.CalculatedSpending, reduceExpense.TargetAmount), _mainFiatCurrency),
                    CurrencyDisplay.FormatFiat(reduceExpense.TargetAmount, _mainFiatCurrency)),
                BitcoinHodlGoalType bitcoinHodl => GetBitcoinHodlDescription(bitcoinHodl),
                _ => string.Empty
            };
        }
    }

    private static string GetBitcoinHodlDescription(BitcoinHodlGoalType bitcoinHodl)
    {
        if (bitcoinHodl.MaxSellableSats == 0)
        {
            return bitcoinHodl.CalculatedSoldSats == 0
                ? language.GoalDescription_BitcoinHodl_NoSales
                : string.Format(language.GoalDescription_BitcoinHodl_Failed, CurrencyDisplay.FormatSatsAsNumber(bitcoinHodl.CalculatedSoldSats));
        }

        return string.Format(
            language.GoalDescription_BitcoinHodl_WithLimit,
            CurrencyDisplay.FormatSatsAsNumber(Math.Min(bitcoinHodl.CalculatedSoldSats, bitcoinHodl.MaxSellableSats)),
            CurrencyDisplay.FormatSatsAsNumber(bitcoinHodl.MaxSellableSats));
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

    /// <summary>
    /// Updates the goal data and animates the progress bar to the new value over 3 seconds
    /// </summary>
    public void UpdateGoal(Goal newGoal)
    {
        var oldProgress = AnimatedProgressPercentage;
        var newProgress = Math.Min(newGoal.Progress, 100);

        _goal = newGoal;

        // Notify that derived properties may have changed
        OnPropertyChanged(nameof(FriendlyName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(TargetDisplay));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(IsMarkedAsComplete));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsProgressComplete));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanConclude));
        OnPropertyChanged(nameof(CanReopen));
        OnPropertyChanged(nameof(ShowProgressBar));

        // If progress changed, animate it
        if (oldProgress != newProgress)
        {
            StartProgressAnimation(oldProgress, newProgress);
        }
    }

    private void StartProgressAnimation(decimal fromValue, decimal toValue)
    {
        // Stop any existing animation
        _animationTimer?.Dispose();

        _animationStartValue = fromValue;
        _animationTargetValue = toValue;
        _animationStartTime = DateTime.UtcNow;

        _animationTimer = new Timer(OnAnimationTick, null, 0, AnimationIntervalMs);
    }

    private void OnAnimationTick(object? state)
    {
        var elapsed = (DateTime.UtcNow - _animationStartTime).TotalMilliseconds;
        var progress = Math.Min(elapsed / AnimationDurationMs, 1.0);

        // Cubic ease-out: 1 - (1 - t)^3
        var easedProgress = 1 - Math.Pow(1 - progress, 3);

        var currentValue = _animationStartValue + (decimal)easedProgress * (_animationTargetValue - _animationStartValue);

        Dispatcher.UIThread.Post(() =>
        {
            AnimatedProgressPercentage = currentValue;
        });

        if (progress >= 1.0)
        {
            _animationTimer?.Dispose();
            _animationTimer = null;
        }
    }

    public void Dispose()
    {
        _animationTimer?.Dispose();
        _animationTimer = null;
    }
}
