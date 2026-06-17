using System.Collections.Generic;
using System.Linq;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Services;

/// <summary>
/// Result of calculating selection state for transactions.
/// </summary>
public record TransactionSelectionResult(
    bool IsSingleItemSelected,
    TransactionViewModel? AnchorTransaction,
    string AmountHeader
);

/// <summary>
/// Calculates selection state (totals, headers) for a list of selected transactions.
/// </summary>
public interface ITransactionSelectionService
{
    TransactionSelectionResult CalculateSelection(
        IReadOnlyList<TransactionViewModel> selectedTransactions,
        AccountViewModel? selectedAccount);
}

public class TransactionSelectionService : ITransactionSelectionService
{
    public TransactionSelectionResult CalculateSelection(
        IReadOnlyList<TransactionViewModel> selectedTransactions,
        AccountViewModel? selectedAccount)
    {
        if (selectedTransactions.Count == 0)
        {
            return new TransactionSelectionResult(
                IsSingleItemSelected: false,
                AnchorTransaction: null,
                AmountHeader: language.Transactions_Columns_Amount);
        }

        var anchor = selectedTransactions.FirstOrDefault();

        if (selectedAccount == null || selectedTransactions.Count == 1)
        {
            return new TransactionSelectionResult(
                IsSingleItemSelected: selectedTransactions.Count == 1,
                AnchorTransaction: anchor,
                AmountHeader: language.Transactions_Columns_Amount);
        }

        // Multiple items selected with an account context - calculate total
        if (selectedAccount.IsBtcAccount)
        {
            long totalSats = 0;
            foreach (var item in selectedTransactions)
            {
                if (item.TransactionType is TransactionTypes.Credit or TransactionTypes.Debt)
                    totalSats += item.FromAmountSats.GetValueOrDefault();
                else
                {
                    if (item.FromAccountId == selectedAccount.Id)
                        totalSats += item.FromAmountSats.GetValueOrDefault();
                    else
                        totalSats += item.ToAmountSats.GetValueOrDefault();
                }
            }

            return new TransactionSelectionResult(
                IsSingleItemSelected: false,
                AnchorTransaction: anchor,
                AmountHeader: $"{language.Transactions_Columns_Amount}: {totalSats}");
        }
        else
        {
            decimal totalFiat = 0;
            foreach (var item in selectedTransactions)
            {
                if (item.TransactionType is TransactionTypes.Credit or TransactionTypes.Debt)
                    totalFiat += item.FromAmountFiat.GetValueOrDefault();
                else
                {
                    if (item.FromAccountId == selectedAccount.Id)
                        totalFiat += item.FromAmountFiat.GetValueOrDefault();
                    else
                        totalFiat += item.ToAmountFiat.GetValueOrDefault();
                }
            }

            return new TransactionSelectionResult(
                IsSingleItemSelected: false,
                AnchorTransaction: anchor,
                AmountHeader: $"{language.Transactions_Columns_Amount}: {totalFiat}");
        }
    }
}
