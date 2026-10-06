# Phase 39: Spending Analytics Reports & UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-04
**Phase:** 39-Spending Analytics Reports & UI
**Areas discussed:** Savings rate definition, Fixed vs variable semantics, Burn rate baseline, Panel layout & filters

---

## Savings Rate Definition

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse MonthlyTotals income | Reuse the Income/Expenses values MonthlyTotalsReport already computes per month | ✓ |
| Fiat income only | Only fiat-denominated income transactions count | |
| New income definition | Compute a separate income stream with its own rules | |

**User's choice:** Reuse MonthlyTotals income

| Option | Description | Selected |
|--------|-------------|----------|
| Skip month | Show nothing for zero-income months (gap in trend line) | ✓ |
| Show 0% | Display 0% for months with no income | |
| Show N/A label | Placeholder label for that month | |

**User's choice:** Skip month

| Option | Description | Selected |
|--------|-------------|----------|
| Show negative % | e.g. -25%; matches MonthlyReportItemViewModel's +/- formatting | ✓ |
| Clamp to 0% | Clamp at 0% and indicate overspending separately | |
| You decide | Researcher/planner picks | |

**User's choice:** Show negative %

| Option | Description | Selected |
|--------|-------------|----------|
| Complete months only | Exclude the current partial month | ✓ |
| Include current month | Include, marked as partial/in-progress | |
| You decide | Researcher/planner decides | |

**User's choice:** Complete months only

---

## Fixed vs Variable Semantics

| Option | Description | Selected |
|--------|-------------|----------|
| Bound transactions | Fixed = transactions bound to fixed expense records; variable = all other expenses | ✓ |
| Planned ranges | Fixed = expected amounts from registered FixedExpense ranges | |
| Show both | Planned vs actual as two series | |

**User's choice:** Bound transactions

| Option | Description | Selected |
|--------|-------------|----------|
| Paid only | Only records with an actual bound transaction count | ✓ |
| Paid + ManuallyPaid | ManuallyPaid also counts as fixed | |
| You decide | Researcher decides | |

**User's choice:** Paid only

| Option | Description | Selected |
|--------|-------------|----------|
| Split of total expenses | Fixed + variable = 100% of the month's spend | ✓ |
| Against income | Fixed/variable as % of income | |

**User's choice:** Split of total expenses

| Option | Description | Selected |
|--------|-------------|----------|
| 100% variable | Month had spending, none bound to fixed expenses | ✓ |
| Skip month | Exclude such months from the chart | |

**User's choice:** 100% variable

| Option | Description | Selected |
|--------|-------------|----------|
| Complete months only | Consistent with savings rate decision | ✓ |
| Include current month | Marked as in-progress | |

**User's choice:** Complete months only

| Option | Description | Selected |
|--------|-------------|----------|
| Empty state + hint | Prompt user to register fixed expenses to unlock the metric | ✓ |
| Always show chart | Everything renders as 100% variable | |

**User's choice:** Empty state + hint

---

## Burn Rate Baseline

| Option | Description | Selected |
|--------|-------------|----------|
| Current-month gauge | MTD spend / days elapsed; projected = avg daily × days in month | ✓ |
| Gauge + history trend | Also show historical per-month daily averages | |
| Historical only | No current-month projection | |

**User's choice:** Current-month gauge

| Option | Description | Selected |
|--------|-------------|----------|
| 12-month median | Reuse StatisticsReport's MedianMonthlyExpenses | ✓ |
| Filtered-range median | Median over the selected date range | |
| 3-month median | More responsive to recent changes | |

**User's choice:** 12-month median

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse MonthlyTotals expenses | Same Expenses figure as other panels | ✓ |
| Separate calculation | Computed separately for this panel | |

**User's choice:** Reuse MonthlyTotals expenses

| Option | Description | Selected |
|--------|-------------|----------|
| Hide projection early-month | e.g. projection starts day 3 | |
| Always project | Regardless of how early in the month | |
| Project only after at least 5 days | Before day 5: spend-so-far and avg daily only | ✓ |

**User's choice:** Project only after at least 5 days (freeform — user specified the 5-day threshold explicitly)

---

## Panel Layout & Filters

| Option | Description | Selected |
|--------|-------------|----------|
| DashboardGridPanel + charts | Cards plug into existing grid; trend charts below; burn rate as dashboard card | ✓ |
| Dedicated Spending section | Separate section between cards and charts | |
| Cards only | All three as dashboard cards, no new charts | |

**User's choice:** DashboardGridPanel + charts

| Option | Description | Selected |
|--------|-------------|----------|
| Honor all filters | Accounts, categories, date range — like existing reports | ✓ |
| Date range only | Always all accounts/categories | |
| No filters | Always full data | |

**User's choice:** Honor all filters

| Option | Description | Selected |
|--------|-------------|----------|
| Line + stacked bar | Savings rate = line chart of monthly %; fixed/variable = stacked bar per month | ✓ |
| Two line charts | Fixed% and variable% as two lines | |
| You decide | Researcher/planner picks | |

**User's choice:** Line + stacked bar

---

## the agent's Discretion

- Exact placement order of the new dashboard card within DashboardGridPanel and chart ordering
- Loading/empty-state visuals beyond specified behaviors (follow existing panel patterns)

## Deferred Ideas

None — discussion stayed within phase scope.
