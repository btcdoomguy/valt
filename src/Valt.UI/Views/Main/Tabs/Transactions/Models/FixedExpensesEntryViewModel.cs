using System;
using Avalonia.Media;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public class FixedExpensesEntryViewModel(FixedExpenseProviderEntry entry, DateOnly currentDate)
{
    public FixedExpenseProviderEntry Entry { get; } = entry;
    public string Id => Entry.Id;
    public string Name => Entry.Name;
    public string? CategoryId => Entry.CategoryId;
    public DateOnly ReferenceDate => Entry.ReferenceDate;
    public string? DefaultAccountId => Entry.DefaultAccountId;
    public decimal? FixedAmount => Entry.FixedAmount;
    public decimal? RangedAmountMin => Entry.RangedAmountMin;
    public decimal? RangedAmountMax => Entry.RangedAmountMax;
    public string Currency => Entry.Currency;

    public decimal MinimumAmount => Entry.MinimumAmount;
    public decimal MaximumAmount => Entry.MaximumAmount;

    public string? TransactionId => Entry.TransactionId;
    public FixedExpenseRecordState State => Entry.State;

    public bool Paid => Entry.Paid;
    public bool Ignored => Entry.Ignored;
    public bool MarkedAsPaid => Entry.MarkedAsPaid;
    public bool Empty => Entry.Empty;

    public int Day => Entry.Day;
    
    public string DayFormatted => Entry.Day.ToString().PadLeft(2, '0');

    public bool IsLateOrCurrentDay => ReferenceDate <= currentDate;

    public bool IsUpcoming => !IsLateOrCurrentDay && ReferenceDate <= currentDate.AddDays(2);

    public string AmountDisplay
    {
        get
        {
            if (FixedAmount.HasValue)
                return CurrencyDisplay.FormatFiat(FixedAmount.Value, Currency);

            if (RangedAmountMin.HasValue && RangedAmountMax.HasValue)
                return $"{CurrencyDisplay.FormatFiat(RangedAmountMin.Value, Currency)} - {CurrencyDisplay.FormatFiat(RangedAmountMax.Value, Currency)}";

            return string.Empty;
        }
    }

    public SolidColorBrush CheckboxPaidColor =>
        Paid ? FixedExpenseListResources.PaidForeground : FixedExpenseListResources.DefaultForeground;

    public SolidColorBrush TextColor
    {
        get
        {
            if (Paid || Ignored)
                return FixedExpenseListResources.IgnoredForeground;

            if (IsLateOrCurrentDay)
                return FixedExpenseListResources.LateForeground;

            if (IsUpcoming)
                return FixedExpenseListResources.WarningForeground;

            return FixedExpenseListResources.DefaultForeground;
        }
    }
        
}