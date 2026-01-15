using System;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.TransactionEditor;

namespace Valt.UI.Views.Main.Modals.FixedExpenseHistory;

public partial class FixedExpenseHistoryViewModel : ValtModalViewModel
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;
    private readonly IModalFactory _modalFactory;

    [ObservableProperty] private string _fixedExpenseName = string.Empty;
    [ObservableProperty] private TransactionHistoryItemViewModel? _selectedTransaction;

    public AvaloniaList<TransactionHistoryItemViewModel> Transactions { get; set; } = new();
    public AvaloniaList<PriceHistoryItemViewModel> PriceHistory { get; set; } = new();

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public FixedExpenseHistoryViewModel()
    {
        if (!Design.IsDesignMode) return;

        FixedExpenseName = "Internet Bill";

        Transactions.Add(new TransactionHistoryItemViewModel
        {
            TransactionId = "1",
            Date = new DateOnly(2024, 1, 15),
            Name = "Internet January",
            Amount = "R$ 150,00",
            CategoryName = "Bills",
            CategoryIcon = Icon.Empty,
            AccountName = "Main Account",
            AccountIcon = Icon.Empty,
            ReferenceDate = new DateOnly(2024, 1, 15)
        });

        PriceHistory.Add(new PriceHistoryItemViewModel
        {
            PeriodStart = new DateOnly(2024, 1, 1),
            Amount = "R$ 150,00",
            Period = "Monthly",
            Day = 15
        });
    }

    public FixedExpenseHistoryViewModel(
        IFixedExpenseQueries fixedExpenseQueries,
        IModalFactory modalFactory)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
        _modalFactory = modalFactory;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        await FetchHistoryAsync(request.FixedExpenseId);
    }

    private async Task FetchHistoryAsync(string fixedExpenseId)
    {
        var history = await _fixedExpenseQueries.GetFixedExpenseHistoryAsync(new FixedExpenseId(fixedExpenseId));

        if (history is null)
            return;

        FixedExpenseName = history.FixedExpenseName;

        Transactions.Clear();
        foreach (var transaction in history.Transactions)
        {
            Transactions.Add(new TransactionHistoryItemViewModel
            {
                TransactionId = transaction.TransactionId,
                Date = transaction.Date,
                Name = transaction.Name,
                Amount = transaction.Amount,
                CategoryName = transaction.CategoryName,
                CategoryIcon = transaction.CategoryIcon is not null
                    ? Icon.RestoreFromId(transaction.CategoryIcon)
                    : Icon.Empty,
                AccountName = transaction.AccountName,
                AccountIcon = transaction.AccountIcon is not null
                    ? Icon.RestoreFromId(transaction.AccountIcon)
                    : Icon.Empty,
                ReferenceDate = transaction.ReferenceDate
            });
        }

        PriceHistory.Clear();
        foreach (var range in history.PriceHistory)
        {
            PriceHistory.Add(new PriceHistoryItemViewModel
            {
                PeriodStart = range.PeriodStart,
                Amount = range.Amount,
                Period = range.Period,
                Day = range.Day
            });
        }
    }

    [RelayCommand]
    private async Task EditTransaction()
    {
        if (SelectedTransaction is null)
            return;

        var request = new TransactionEditorViewModel.Request
        {
            TransactionId = new TransactionId(SelectedTransaction.TransactionId)
        };

        var currentWindow = GetWindow!();
        var modal = (TransactionEditorView)await _modalFactory.CreateAsync(
            ApplicationModalNames.TransactionEditor,
            currentWindow,
            request)!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(currentWindow);

        if (result is not null && Parameter is Request originalRequest)
        {
            await FetchHistoryAsync(originalRequest.FixedExpenseId);
        }
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Request
    {
        public required string FixedExpenseId { get; init; }
    }

    public record TransactionHistoryItemViewModel
    {
        public required string TransactionId { get; init; }
        public DateOnly Date { get; init; }
        public required string Name { get; init; }
        public required string Amount { get; init; }
        public required string CategoryName { get; init; }
        public required Icon CategoryIcon { get; init; }
        public required string AccountName { get; init; }
        public required Icon AccountIcon { get; init; }
        public DateOnly ReferenceDate { get; init; }
    }

    public record PriceHistoryItemViewModel
    {
        public DateOnly PeriodStart { get; init; }
        public required string Amount { get; init; }
        public required string Period { get; init; }
        public int Day { get; init; }
    }
}
