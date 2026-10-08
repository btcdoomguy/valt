using Valt.Core.Common;

namespace Valt.App.Modules.Goals.DTOs;

/// <summary>
/// A single transaction that contributes to a goal's progress, with the running
/// total after this row. <see cref="FiatAmount"/> and <see cref="SatsAmount"/> carry
/// magnitudes (always non-negative); the direction of each contribution (income +,
/// expense -) is expressed only by the deltas of <see cref="RunningTotal"/>, which
/// follows the goal's accumulation math.
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
    /// Fiat amount as a magnitude (always non-negative), in the transaction's original fiat
    /// currency. Zero for sats-only rows (no fiat leg). The sign of the contribution is
    /// expressed only in <see cref="RunningTotal"/>, never here.
    /// </summary>
    public required FiatValue FiatAmount { get; init; }

    /// <summary>ISO code of the fiat currency <see cref="FiatAmount"/> is expressed in.</summary>
    public required string FiatCurrencyCode { get; init; }

    /// <summary>
    /// Sats value as a magnitude (always non-negative), converted at the transaction-date
    /// BTC price (closest-date lookup with buffer), never at a live price. The sign of the
    /// contribution is expressed only in <see cref="RunningTotal"/>, never here.
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
