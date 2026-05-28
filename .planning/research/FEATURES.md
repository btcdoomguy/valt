# Features Research — Spending Evolution Module

**Date:** 2026-05-27
**Context:** Personal finance app for bitcoiners

## Table Stakes (Must Have)

1. **Time-range selection** — Users must be able to choose analysis period (12/24/36/48/60 months)
2. **Category filtering** — Multi-select category tree to narrow analysis scope
3. **Dual-currency display** — Show totals in both fiat and bitcoin (sats) simultaneously
4. **Trend indicators** — Percentage change over the selected period
5. **Interactive chart** — Line chart that responds to filtering changes in real-time

## Differentiators (Competitive Advantage)

1. **Bitcoin-denominated cost of living** — Showing spending trends in sats is unique to bitcoiner-focused apps
2. **Dual-axis visualization** — Fiat inflation vs bitcoin deflation in one view
3. **Context menu integration** — Right-click on transaction → "Analyze evolution" with category pre-selected
4. **Real-time recalculation** — Instant updates as categories are selected/deselected

## Anti-Features (Deliberately NOT Building)

1. **Predictive forecasting** — No ML/AI predictions of future spending
2. **Budget comparison** — Not comparing against budget targets (that's the Budget/Goals module)
3. **Transaction-level detail** — Not drilling down to individual transactions (keep it high-level)
4. **Export functionality** — No CSV/PDF export in v1
5. **Shared/comparison views** — No comparing against other users or benchmarks

## Dependencies

- Requires existing Transaction data with `PriceInSats` field populated
- Depends on existing Category hierarchy
- Uses existing fiat currency preference
- Leverages existing price history for historical conversions
