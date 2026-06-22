using System.Collections.Generic;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.UI.Services;

/// <summary>
/// Immutable form-state snapshot used to build a <see cref="TransactionDetailsDto"/>.
/// </summary>
public sealed record TransactionFormSnapshot(
    TransactionTypes SelectedMode,
    AccountDTO FromAccount,
    AccountDTO? ToAccount,
    BtcValue? FromAccountBtcValue,
    FiatValue? FromAccountFiatValue,
    BtcValue? ToAccountBtcValue,
    FiatValue? ToAccountFiatValue,
    bool AccountsAreSameTypeAndCurrency);

/// <summary>
/// Values loaded from a <see cref="TransactionDetailsDto"/> that the ViewModel applies to its form fields.
/// </summary>
public sealed record TransactionFormValues(
    TransactionTypes SelectedMode,
    AccountDTO? ToAccount,
    BtcValue? FromAccountBtcValue,
    FiatValue? FromAccountFiatValue,
    BtcValue? ToAccountBtcValue,
    FiatValue? ToAccountFiatValue);

/// <summary>
/// Builds transaction-details DTOs from form state and loads form values from existing DTOs.
/// </summary>
public interface ITransactionDetailsBuilder
{
    TransactionDetailsDto BuildDto(TransactionFormSnapshot snapshot);

    TransactionFormValues LoadFromDto(
        TransactionDetailsDto dto,
        IReadOnlyList<AccountDTO> availableAccounts);
}
