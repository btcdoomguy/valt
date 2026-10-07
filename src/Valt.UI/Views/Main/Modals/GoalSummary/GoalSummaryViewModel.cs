using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Modules.Goals.DTOs;
using Valt.Infra.Kernel;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Modals.GoalSummary;

public partial class GoalSummaryViewModel : ValtModalViewModel
{
    [ObservableProperty] private string _windowTitle = language.GoalSummary_Title;
    [ObservableProperty] private string _goalName = string.Empty;
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _finalTotalFormatted = string.Empty;
    [ObservableProperty] private bool _hasRows;

    public AvaloniaList<RowItemViewModel> Rows { get; set; } = new();

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public GoalSummaryViewModel()
    {
        if (!Design.IsDesignMode) return;

        GoalName = language.GoalType_StackBitcoin;
        PeriodLabel = "(01/25)";
        FinalTotalFormatted = "750 000";
        HasRows = true;

        Rows.Add(new RowItemViewModel
        {
            DateFormatted = "1/15/2025",
            Description = "Monthly buy",
            AccountName = "Checking",
            CategoryName = "Bitcoin",
            FiatFormatted = "$ 500.00",
            SatsFormatted = string.Empty,
            RunningTotalFormatted = "500.00 USD",
            IsContributionPositive = true,
            IsContributionNegative = false,
            ContributionForeground = ResolveBrush("SemanticPositive200Brush")
        });

        Rows.Add(new RowItemViewModel
        {
            DateFormatted = "2/15/2025",
            Description = "Weekly DCA",
            AccountName = "Savings",
            CategoryName = string.Empty,
            FiatFormatted = string.Empty,
            SatsFormatted = "250 000 sats",
            RunningTotalFormatted = "250 000",
            IsContributionPositive = false,
            IsContributionNegative = true,
            ContributionForeground = ResolveBrush("SemanticNegative200Brush")
        });
    }

    public override Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return Task.CompletedTask;

        GoalName = request.GoalName;
        PeriodLabel = request.PeriodLabel;

        // request.Result is the already-fetched Phase 49 query result; the modal
        // never loads async (query is awaited before IModalFactory.CreateAsync).
        if (request.Result is not GoalContributingTransactionsResult.Supported supported)
            return Task.CompletedTask; // unreachable: CanViewSummary gates NetWorthBtc out

        HasRows = supported.Rows.Count > 0;

        var positiveBrush = ResolveBrush("SemanticPositive200Brush");
        var negativeBrush = ResolveBrush("SemanticNegative200Brush");

        decimal previousRunningTotal = 0m;
        foreach (var (row, index) in supported.Rows.Select((r, i) => (r, i)))
        {
            // The ONLY sign source: RunningTotal deltas (FiatAmount/SatsAmount are non-negative magnitudes)
            var contribution = index == 0 ? row.RunningTotal : row.RunningTotal - previousRunningTotal;
            previousRunningTotal = row.RunningTotal;

            var isPositive = contribution > 0m;
            var isNegative = contribution < 0m;

            Rows.Add(new RowItemViewModel
            {
                DateFormatted = row.Date.ToShortDateString(),
                Description = row.Description,
                AccountName = row.AccountName,
                CategoryName = row.CategoryName ?? string.Empty,
                FiatFormatted = row.FiatAmount.Value == 0m
                    ? string.Empty // sats-only row: empty cell, never a fabricated 0 (Phase 49 Q3)
                    : CurrencyDisplay.FormatFiat(row.FiatAmount.Value, row.FiatCurrencyCode),
                SatsFormatted = row.SatsAmount.Sats == 0
                    ? string.Empty // fiat-only row: empty cell
                    : $"{CurrencyDisplay.FormatSatsAsNumber(row.SatsAmount.Sats)} {language.SatsLabel}",
                RunningTotalFormatted = FormatRunningTotal(row.RunningTotal, request.StrategyUnit, request.MainCurrencyCode),
                IsContributionPositive = isPositive,
                IsContributionNegative = isNegative,
                ContributionForeground = isPositive ? positiveBrush : isNegative ? negativeBrush : null
            });
        }

        if (supported.Rows.Count > 0)
        {
            // Final reconciled total = last row's RunningTotal, never recomputed in the UI
            FinalTotalFormatted = FormatRunningTotal(supported.Rows[^1].RunningTotal, request.StrategyUnit, request.MainCurrencyCode);
        }

        return Task.CompletedTask;
    }

    private static string FormatRunningTotal(decimal value, GoalStrategyUnit unit, string mainCurrencyCode)
    {
        return unit switch
        {
            GoalStrategyUnit.Fiat => CurrencyDisplay.FormatFiat(value, mainCurrencyCode),
            GoalStrategyUnit.Sats => CurrencyDisplay.FormatSatsAsNumber((long)value),
            GoalStrategyUnit.Count => string.Format("{0:N0}", value),
            GoalStrategyUnit.Percentage => string.Format("{0:N1}%", value),
            _ => string.Empty
        };
    }

    private static IBrush? ResolveBrush(string key)
    {
        if (Application.Current?.TryGetResource(key, ThemeVariant.Default, out var resource) == true)
            return resource as IBrush;

        return null;
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Request
    {
        // GoalId intentionally omitted: the query result is pre-fetched by
        // ViewSummaryCommand, so the modal never re-queries by id.
        public required string GoalName { get; init; }
        public required string PeriodLabel { get; init; }
        public required string MainCurrencyCode { get; init; }
        public required GoalContributingTransactionsResult Result { get; init; }
        public required GoalStrategyUnit StrategyUnit { get; init; }
    }

    public record Response
    {
    }

    public record RowItemViewModel
    {
        public required string DateFormatted { get; init; }
        public required string Description { get; init; }
        public required string AccountName { get; init; }
        public required string CategoryName { get; init; }
        public required string FiatFormatted { get; init; }
        public required string SatsFormatted { get; init; }
        public required string RunningTotalFormatted { get; init; }
        public required bool IsContributionPositive { get; init; }
        public required bool IsContributionNegative { get; init; }
        public required IBrush? ContributionForeground { get; init; }
    }
}
