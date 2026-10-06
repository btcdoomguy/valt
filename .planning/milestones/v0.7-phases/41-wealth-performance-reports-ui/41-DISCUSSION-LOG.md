# Phase 41: Wealth & Performance Reports & UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-10
**Phase:** 41-Wealth & Performance Reports & UI
**Areas discussed:** CAGR scope and start point, Fiat vs BTC allocation definition, Best/worst months ranking metric, Days under water presentation

---

## CAGR scope and start point

| Option | Description | Selected |
|--------|-------------|----------|
| Lifetime: from first transaction | Start from the user's first transaction or initial account balance. Shows lifetime growth of the whole tracked portfolio. | ✓ |
| Period: from selected range start | Start from the beginning of the selected date range. Shows growth within the filtered period only. | |
| Initial investment basis | Start from a fixed initial net worth amount. | |

**User's choice:** Lifetime: from first transaction
**Notes:** The date range will filter the displayed window, not the CAGR start point.

---

## CAGR net worth definition

| Option | Description | Selected |
|--------|-------------|----------|
| Gross wealth: accounts + includeInNetWorth assets | Reuse existing AllTimeHigh total-wealth calculation; loans not subtracted. | ✓ |
| Net worth: subtract liabilities / loans | Subtract BTC-backed loans and other liabilities. | |
| Accounts only | Only accounts, excluding external assets. | |

**User's choice:** Gross wealth: accounts + includeInNetWorth assets
**Notes:** Matches existing dashboard and AllTimeHigh report; excludes sold assets per existing filter.

---

## Fiat vs BTC allocation definition

| Option | Description | Selected |
|--------|-------------|----------|
| Balance-sheet allocation by value | Split total wealth into fiat-denominated and BTC-denominated portions at each month-end. | |
| Income flow allocation | Fraction of monthly income that arrived in fiat vs BTC. | |
| Accounts only | Simplify to just fiat-account balances vs BTC-account balances, ignoring external assets. | ✓ |

**User's choice:** Accounts only
**Notes:** External assets intentionally excluded; allocation is account-only.

---

## Allocation account classification

| Option | Description | Selected |
|--------|-------------|----------|
| BTC account type = BTC, all others = fiat | Accounts whose type is Bitcoin count as BTC; all others as fiat. | ✓ |
| Convert all balances to BTC and compute % | Convert every account balance to BTC at month-end rate. | |
| By account currency | BTC/SATS = BTC; everything else = fiat. | |

**User's choice:** BTC account type = BTC, all others = fiat
**Notes:** Provides a stable, easy-to-reason-about split.

---

## Best/worst months ranking metric

| Option | Description | Selected |
|--------|-------------|----------|
| Absolute fiat wealth delta | Rank by absolute change in gross wealth (fiat value). | ✓ |
| Percentage change | Rank by percentage change in net worth relative to previous month. | |
| BTC/sats delta | Rank by change in BTC/sats holdings. | |

**User's choice:** Absolute fiat wealth delta
**Notes:** Big-wealth months with large moves will dominate; early small-portfolio months will be less visible.

---

## Current month inclusion for best/worst months

| Option | Description | Selected |
|--------|-------------|----------|
| Include current incomplete month | Compare current month to previous month-end; ranking updates as month progresses. | |
| Complete months only | Exclude current month; ranking is stable. | ✓ |

**User's choice:** Complete months only
**Notes:** Consistent with Phase 39 panels that use complete months only.

---

## Days under water presentation

| Option | Description | Selected |
|--------|-------------|----------|
| Single dashboard card | Add a card showing "X days since ATH" plus decline %. | |
| Trend chart | Add a line chart showing drawdown % over time. | |
| Extend existing ATH panel | Extend the AllTimeHigh panel with the days-since-ATH number. | ✓ |

**User's choice:** Extend existing ATH panel
**Notes:** Keeps all ATH-related info in one place; no new chart panel.

---

## the agent's Discretion

- Exact panel ordering within the Reports tab.
- Chart type for allocation trend (stacked area, line, or dual line).
- Number of best/worst months shown (e.g., top 3 vs top 5).
- Specific CAGR edge-case handling (negative/zero starting wealth).
- Loading-state and empty-state visuals beyond the specified behaviors.

## Deferred Ideas

None — discussion stayed within phase scope.
