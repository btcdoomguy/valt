using Valt.Core.Common;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Goals.Services;

/// <summary>
/// A single transaction contributing to a goal, with strategy-unit accumulation state.
/// </summary>
/// <param name="Transaction">The contributing transaction entity.</param>
/// <param name="SignedFiatAmount">
/// Fiat amount with its natural sign (negative for debits), in the original currency; 0 for sats-only rows.
/// </param>
/// <param name="FiatCurrencyCode">
/// ISO code of the original fiat currency; the main currency for sats-only rows (Q3 decision).
/// </param>
/// <param name="SatsAmount">
/// Sats value at the transaction-date price (native when the transaction carries a sat leg,
/// otherwise converted from fiat at closest-date rates).
/// </param>
/// <param name="Contribution">
/// The amount feeding the strategy's accumulation, in the strategy's own unit
/// (main fiat for reader-backed strategies).
/// </param>
/// <param name="RunningTotal">Cumulative contribution up to and including this row.</param>
public sealed record GoalContributionRow(
    TransactionEntity Transaction,
    decimal SignedFiatAmount,
    string FiatCurrencyCode,
    BtcValue SatsAmount,
    decimal Contribution,
    decimal RunningTotal);
