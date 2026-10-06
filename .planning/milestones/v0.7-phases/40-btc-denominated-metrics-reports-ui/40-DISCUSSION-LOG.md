# Phase 40: BTC-Denominated Metrics Reports & UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-05
**Phase:** 40-BTC-Denominated Metrics Reports & UI
**Areas discussed:** Earned/spent scope, Receipt-date rate fallback, Stack velocity formula, UI grouping

---

## Earned/spent scope

| Option | Description | Selected |
|--------|-------------|----------|
| Income/expense only | Sats earned = fiat income converted at receipt-date BTC rate + native bitcoin income; sats spent = fiat expenses converted at receipt-date BTC rate + native bitcoin expenses. BTC purchases/sales and internal transfers excluded. | ✓ |
| Include BTC trades | Sats earned includes BTC sold; sats spent includes BTC purchased. | |
| Net BTC flow | Any net BTC inflow = earned, any net BTC outflow = spent. | |

**User's choice:** Income/expense only
**Notes:** BTC purchases/sales and internal transfers are explicitly excluded from earned/spent; they resurface only in stack velocity.

### Native bitcoin classification

| Option | Description | Selected |
|--------|-------------|----------|
| Use amount sign | Positive BTC inflow = earned, negative BTC outflow = spent. | ✓ |
| Honor category type | Only Income-category inflows count as earned; only Expense-category outflows count as spent. | |

**User's choice:** Use amount sign
**Notes:** Category type is not used to classify native bitcoin transactions for this metric.

### Current incomplete month

| Option | Description | Selected |
|--------|-------------|----------|
| Include current month | Include the current incomplete month. | ✓ |
| Complete months only | Exclude the current incomplete month. | |
| Show as in progress | Include but label/gray as in progress. | |

**User's choice:** Include current month

### Own-account transfers

| Option | Description | Selected |
|--------|-------------|----------|
| Exclude own-account transfers | Exclude transfers between the user's own accounts from both earned and spent. | ✓ |
| Count all inflows/outflows | Count any transfer by amount sign. | |
| Use category semantics | Exclude based on transfer categories. | |

**User's choice:** Exclude own-account transfers
**Notes:** Interpreted as: exclude transactions where both source and destination accounts exist in the user's account list.

---

## Receipt-date rate fallback

| Option | Description | Selected |
|--------|-------------|----------|
| Nearest available date | Use the nearest available BTC price. | |
| Latest known rate | Use the most recent known rate. | |
| Skip transaction | Skip transactions whose exact date has no BTC price. | ✓ |
| Interpolate | Linearly interpolate between closest prices. | |

**User's choice:** Skip transaction

### Missing-rate UI behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Show warning note | Surface an inline note about skipped transactions. | |
| Silently exclude | Exclude missing-rate transactions without UI indication. | ✓ |
| Drill-down list | Show a list of skipped transactions. | |

**User's choice:** Silently exclude

### Non-main-currency conversion path

| Option | Description | Selected |
|--------|-------------|----------|
| Direct via USD | Convert source fiat → USD → BTC. | ✓ |
| Via main fiat | Convert source fiat → main fiat → BTC. | |
| You decide | Let implementation use existing wiring. | |

**User's choice:** Direct via USD

---

## Stack velocity formula

| Option | Description | Selected |
|--------|-------------|----------|
| Earned minus spent | Net from income/expense scope only. | |
| Net BTC holding change | Actual change in BTC holdings including all flows. | |
| Include trades | Earned − spent + BTC purchases − BTC sales; internal transfers excluded. | ✓ |

**User's choice:** Include trades

### Chart type

| Option | Description | Selected |
|--------|-------------|----------|
| Monthly bars | Bar per month, positive/negative. | |
| Monthly line | Line connecting monthly net values. | ✓ |
| Cumulative line | Cumulative total over the period. | |
| Both | Monthly bars and cumulative line. | |

**User's choice:** Monthly line

### Zero-velocity months

| Option | Description | Selected |
|--------|-------------|----------|
| Show as zero | Render zero-velocity months on the line at y=0. | ✓ |
| Omit/gap zero months | Skip zero months on the chart. | |
| Gray/dashed zero | Use dashed/gray line for zero months. | |

**User's choice:** Show as zero

### Current incomplete month

| Option | Description | Selected |
|--------|-------------|----------|
| Include current month | Include the current incomplete month. | ✓ |
| Complete months only | Exclude the current incomplete month. | |

**User's choice:** Include current month

---

## UI grouping

| Option | Description | Selected |
|--------|-------------|----------|
| Three separate panels | One panel each for sats earned, sats spent, and stack velocity. | |
| Earned+spent combined | Combine sats earned and sats spent into one panel; stack velocity separate. | ✓ |
| All-in-one panel | All three as series in a single panel. | |
| Velocity+earned combined | Combine velocity and earned; spent per category separate. | |

**User's choice:** Earned+spent combined

### Sats spent per category placement

| Option | Description | Selected |
|--------|-------------|----------|
| Separate category panel | Its own panel below the monthly view. | |
| Toggle inside panel | Toggle within the earned/spent panel to switch monthly/category views. | ✓ |
| Month drill-down | Click a month to see its category breakdown. | |

**User's choice:** Toggle inside panel

### Combined earned/spent chart type

| Option | Description | Selected |
|--------|-------------|----------|
| Dual line | Two lines over time. | |
| Grouped bars | Grouped bars per month. | ✓ |
| Diverging bars | Earned positive, spent negative on a single axis. | |

**User's choice:** Grouped bars

### Placement relative to Phase 39 panels

| Option | Description | Selected |
|--------|-------------|----------|
| After Phase 39 panels | Append new BTC panels after existing spending-analytics panels. | ✓ |
| Before Phase 39 panels | Place BTC panels at the top. | |
| Interleaved | Mix BTC panels with related Phase 39 panels. | |
| You decide | Let the planner choose. | |

**User's choice:** After Phase 39 panels

---

## the agent's Discretion

- Exact panel ordering within the "after Phase 39" region.
- Loading/empty-state visuals following existing patterns.
- Category-aggregation details for the per-category breakdown (top-N, "Other" grouping, etc.).

## Deferred Ideas

None — discussion stayed within phase scope.
