# Pitfalls Research — Spending Evolution Module

**Date:** 2026-05-27
**Context:** Adding spending analysis to bitcoin-denominated finance app

## Pitfall 1: Performance with Large Datasets

**Risk:** Aggregating 5 years of transactions across all categories could be slow if done naively.

**Warning signs:** UI freezes when changing category selection; chart takes >1s to update.

**Prevention:**
- Pre-calculate monthly aggregates (don't aggregate on every UI change)
- Cache results in ViewModel
- Use LiteDB indexes on Transaction.Date and Transaction.CategoryId

**Phase:** Implementation phase (Phase 2)

## Pitfall 2: Bitcoin Price Volatility Skews Analysis

**Risk:** Sats totals are based on historical BTC prices. If price data is missing or inaccurate, the analysis is misleading.

**Warning signs:** Sudden spikes/drops in sats line that don't correlate with spending changes.

**Prevention:**
- Validate PriceInSats is populated for all transactions in range
- Show warning indicator if some transactions lack price data
- Use existing background job (AutoSatAmountJob) to ensure data completeness

**Phase:** Implementation phase (Phase 2)

## Pitfall 3: Dual-Axis Chart Confusion

**Risk:** Users may misread which line corresponds to which axis.

**Warning signs:** User confusion in testing; support questions about chart interpretation.

**Prevention:**
- Clear axis labels with currency symbols
- Color-code lines to match axes
- Legend clearly identifies each series
- Follow existing Wealth Overview chart pattern

**Phase:** UI phase (Phase 3)

## Pitfall 4: Missing Historical Price Data

**Risk:** Transactions older than price history database won't have accurate sats values.

**Warning signs:** Flat lines at zero for early months; gaps in chart.

**Prevention:**
- Check date range against available price history
- Show notification/warning about incomplete data
- Consider using transaction's recorded price if historical data unavailable

**Phase:** Implementation phase (Phase 2)

## Pitfall 5: Inconsistent Category Totals

**Risk:** Category totals in evolution chart may not match Reports module totals, causing user distrust.

**Warning signs:** Users reporting discrepancies between modules.

**Prevention:**
- Reuse existing aggregation logic where possible
- Ensure same filtering rules (date ranges, category inclusion)
- Cross-reference with Reports queries for consistency

**Phase:** Testing/verification phase
