using Valt.Core.Common;

namespace Valt.App.Modules.Goals.DTOs;

/// <summary>
/// A single transaction that contributes to a goal's progress, with the running
/// total after this row. Amounts are displayed with the natural sign convention
/// (income +, expense -); the running total follows the goal's accumulation math.
/// </summary>
public sealed record ContributingTransactionRow
{
    /// <summary>Date of the contributing transaction.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Transaction description (name).</summary>
    public required string Description { get; init; }

    /// <summary>Name of the account the amount left (or entered, when no origin account exists).</summary>
    public required string AccountName { get; init; }

    /// <summary>Category name, or null when the transaction is uncategorized.</summary>
    public required string? CategoryName { get; init; }

    /// <summary>
    /// Fiat amount with the natural display sign, in the transaction's original fiat currency.
    /// Zero for sats-only rows (no fiat leg).
    /// </summary>
    public required FiatValue FiatAmount { get; init; }

    /// <summary>ISO code of the fiat currency <see cref="FiatAmount"/> is expressed in.</summary>
    public required string FiatCurrencyCode { get; init; }

    /// <summary>
    /// Sats value converted at the transaction-date BTC price (closest-date lookup with buffer),
    /// never at a live price.
    /// </summary>
    public required BtcValue SatsAmount { get; init; }

    /// <summary>
    /// Cumulative total in the strategy's own unit, accumulated in decimal with no intermediate
    /// rounding; only the display values round. The final row equals the goal's calculated field:
    /// fiat for SpendingLimit/SaveFiat/IncomeFiat/ReduceExpenseCategory, percentage for SavingsRate,
    /// sats for StackBitcoin/IncomeBtc/BitcoinHodl, count for Dca.
    /// </summary>
    public required decimal RunningTotal { get; init; }
}
