# Phase 42: Loans & Leverage Reports & UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-11
**Phase:** 42-Loans & Leverage Reports & UI
**Areas discussed:** Interest/fees monthly breakdown calculation, Liquidation-price distance trend metric, Trend period and active-loan filter, UI grouping and placement

---

## Interest/fees monthly breakdown calculation

| Option | Description | Selected |
|--------|-------------|----------|
| Snapshot-delta month | Assign the whole delta between snapshots to the month containing the newer snapshot. | |
| APR-apportioned month | Split daily APR accrual across calendar months; assign one-time fees to the snapshot month. | ✓ |
| Lifetime total only | Show only a lifetime total plus a snapshot timeline table. | |

**User's choice:** APR-apportioned month
**Notes:** Recommended option was selected. The user wants a true per-month cost view even when loan-state updates are sporadic.

---

## Combined vs separate interest and fees series

| Option | Description | Selected |
|--------|-------------|----------|
| Combined cost | Single stacked-bar series showing total carrying cost (interest + fees). | ✓ |
| Separate interest and fees | Two separate series showing the split. | |

**User's choice:** Combined cost
**Notes:** The user preferred a simpler chart that answers total monthly carrying cost, despite the recommendation to split.

---

## Include current month in interest/fees trend

| Option | Description | Selected |
|--------|-------------|----------|
| Include current month | Interest accrued up to today is included. | ✓ |
| Complete months only | Only closed months are shown. | |

**User's choice:** Include current month
**Notes:** Recommended option was selected. Keeps the view up-to-date as interest accrues daily.

---

## Liquidation-price distance trend metric

| Option | Description | Selected |
|--------|-------------|----------|
| Worst-case (closest) distance | Single line showing the smallest distance-to-liquidation across all active loans per month. | ✓ |
| Per-loan series | Separate line for every active loan. | |
| Debt-weighted average distance | Single debt-weighted average line. | |

**User's choice:** Worst-case (closest) distance
**Notes:** Recommended option was selected. Aligns with the existing BTC Loans dashboard's "closest distance" row.

---

## Distance measurement

| Option | Description | Selected |
|--------|-------------|----------|
| LTV percentage points | Liquidation LTV minus current LTV, matching existing dashboard semantics. | ✓ |
| BTC price drop % to liquidation | Percentage fall needed to trigger liquidation. | |
| Absolute BTC price distance | Dollar distance above liquidation price. | |

**User's choice:** LTV percentage points
**Notes:** Recommended option was selected. Reuses existing `CalculateDistanceToLiquidation` semantics.

---

## Risk-band coloring

| Option | Description | Selected |
|--------|-------------|----------|
| Color-coded risk bands | Green/yellow/red based on margin-call/liquidation thresholds. | ✓ |
| Plain line | Single-color line without risk bands. | |

**User's choice:** Color-coded risk bands
**Notes:** User wants the trend to visually communicate risk, reusing existing dashboard color thresholds.

---

## Trend period and active-loan filter

| Option | Description | Selected |
|--------|-------------|----------|
| Respect Reports date range | Use the existing date-range selector; only active loans on month-end contribute. | ✓ |
| Full loan lifetime | Show all months from earliest loan start to today regardless of filter. | |
| Per-loan start date | Start each loan at its own start date. | |

**User's choice:** Respect Reports date range
**Notes:** Recommended option was selected. Keeps the new charts consistent with the other v0.7 analytics.

---

## Mid-range loan counting

| Option | Description | Selected |
|--------|-------------|----------|
| Month-end active only | Count a loan only if active on the month-end. | ✓ |
| Active any day in month | Count if active any day, prorating by active days. | |
| Whole month if active at all | Count the whole month if active at any point. | |

**User's choice:** Month-end active only
**Notes:** Recommended option was selected. Simplifies the implementation and matches the liquidation-distance computation.

---

## UI grouping and placement

| Option | Description | Selected |
|--------|-------------|----------|
| New section after Stack velocity | New "Loans & Leverage Reports" expander inside the monthly totals group, after Stack velocity and before Categories. | ✓ |
| Extend BTC Loans dashboard card | Extend the existing dashboard card with summary rows/charts. | |
| Split into two sections | Separate interest/fees and liquidation distance sections. | |

**User's choice:** New section after Stack velocity
**Notes:** Recommended option was selected. Keeps the new v0.7 analytics together and shares the existing date-range selector.

---

## Show both charts or toggle

| Option | Description | Selected |
|--------|-------------|----------|
| Two charts in one section | Combined interest+fees stacked-bar chart plus liquidation-distance line chart. | ✓ |
| Toggle between metrics | Toggle buttons switching between the two charts. | |

**User's choice:** Two charts in one section
**Notes:** Recommended option was selected. Both metrics are visible at a glance.

---

## the agent's Discretion

- Exact chart styling, tooltip formatting, legend placement, and empty-state wording.
- Whether to introduce a new `LoanReports` App/Infra module or add the new queries inside the existing Assets module.
- Whether to use a single combined query DTO or separate DTOs for the two charts, provided the UI contract remains simple.

## Deferred Ideas

None — all discussion stayed within the Phase 42 boundary.
