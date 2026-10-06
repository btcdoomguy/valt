# Phase 40: BTC-Denominated Metrics Reports & UI - Context

**Gathered:** 2026-08-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Add three BTC-denominated metrics to the Reports tab so users see their cash flow denominated in sats:

1. **Sats earned per month** — fiat income converted to sats at the BTC price on each transaction date, plus native bitcoin income
2. **Sats spent per month and per category** — fiat expenses converted to sats at receipt-date BTC rates, plus native bitcoin expenses, with a category breakdown
3. **Stack velocity** — net sats accumulated per month, shown as a trend chart

All new panels honor the existing Reports tab filters (date range, accounts, categories) and render sensible zero/empty states. New capabilities beyond these three metrics belong to other phases (MCP/localization/docs → Phase 43).

</domain>

<decisions>
## Implementation Decisions

### Earned/Spent Scope
- **D-01:** "Sats earned" and "sats spent" are scoped to income and expense flows only. BTC purchases, BTC sales, and internal transfers are excluded from both metrics.
  - Sats earned = fiat income converted at receipt-date BTC rate + native bitcoin income.
  - Sats spent = fiat expenses converted at receipt-date BTC rate + native bitcoin expenses.
  — **Reversibility:** costly — changes the fundamental definition of the metrics and would require re-labeling UI panels.
- **D-02:** Native bitcoin transactions are classified by amount sign: positive BTC inflow counts as earned, negative BTC outflow counts as spent. Category type is not used for this distinction.
- **D-03:** The sats earned/spent trend includes the current incomplete month. No special "in progress" styling is required.
- **D-04:** Internal transfers between the user's own accounts are excluded from both sats earned and sats spent. A transfer is identified when both the source and destination accounts exist in the user's account list.

### Receipt-Date Rate Fallback
- **D-05:** Fiat income/expense is converted to sats using the BTC price on the transaction date.
- **D-06:** If a transaction date has no BTC price in the price database, the transaction is skipped entirely from the sats earned/spent calculation.
- **D-07:** Missing-rate transactions are silently excluded; the UI does not show a warning, drill-down list, or special indicator.
- **D-08:** For non-main-currency fiat amounts, convert directly via USD (source fiat → USD → BTC) using the existing currency conversion path. Do not route through the main fiat currency first.

### Stack Velocity Formula
- **D-09:** Stack velocity = sats earned − sats spent + BTC purchases − BTC sales. Internal transfers remain excluded. This captures the net change in the user's stack from external flows and trading.
  — **Reversibility:** costly — changes the formula and the chart's meaning; would need UI label updates.
- **D-10:** The stack velocity chart is a monthly line chart.
- **D-11:** Months with zero stack velocity are rendered as zero on the line chart (not omitted or gapped).
- **D-12:** The stack velocity trend includes the current incomplete month, consistent with sats earned/spent.

### UI Grouping
- **D-13:** Sats earned and sats spent are combined into a single panel with grouped bars per month (one bar for earned, one bar for spent).
- **D-14:** Sats spent per category is shown via a toggle inside the earned/spent panel that switches between the monthly view and the category view. It is not a separate drill-down or month-specific view in this phase.
- **D-15:** Stack velocity is a separate panel below the combined earned/spent panel.
- **D-16:** The new BTC-denominated panels are placed after the existing Phase 39 spending-analytics panels in the Reports tab.

### Cross-Cutting Conventions (carried forward from Phase 39, not re-discussed)
- **D-17:** New queries follow the App-layer CQRS pattern per AGENTS.md: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
- **D-18:** New panels plug into the existing `DashboardGridPanel` / charts region and honor the existing Reports tab filters.
- **D-19:** New user-facing strings may be English-only in this phase; full pt-BR/es localization lands in Phase 43 (same pattern as v0.5 Phases 30→31 and Phase 39).
- **D-20:** Panels render sensible zero/empty states when no transaction data exists for the period.

### the agent's Discretion
- Exact ordering of the new dashboard card(s) and chart sections within the Reports tab, as long as they appear after Phase 39 panels.
- Loading-state and empty-state visuals beyond the behaviors specified above — follow existing panel patterns (`IsLoading` text, `IsLarge` row spans, zero labels).
- Specific category-aggregation algorithm for the per-category breakdown (e.g., top-N vs all categories, "Other" grouping threshold) as long as all selected categories are represented.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Scope & Requirements
- `.planning/ROADMAP.md` §Phase 40 — goal, success criteria, phase boundary
- `.planning/REQUIREMENTS.md` §BTC-01/BTC-02/BTC-03 — requirement definitions
- `.planning/PROJECT.md` — project constraints (tech stack, backward compatibility, vertical-slice UI rule)
- `.planning/phases/39-spending-analytics-reports-ui/39-CONTEXT.md` — prior phase decisions that carry forward (CQRS, filters, English-only strings, panel patterns)

### Module Documentation
- `.claude/docs/reports.md` — Reports module: `IReportDataProvider`, existing reports, `ReportsViewModel`, chart/dashboard patterns, DI registration
- `.claude/docs/budget.md` — Accounts, transactions, categories

### Code Anchors (patterns to reuse)
- `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs` — frozen dictionaries of accounts, categories, transactions-by-date, BTC/fiat rates with binary-search lookups
- `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs` — existing monthly income/expense aggregation and BTC-price conversion; tracks `BitcoinIncome`, `BitcoinExpense`, `BitcoinPurchased`, `BitcoinSold`
- `src/Valt.App/Modules/SpendingEvolution/` + `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` — reference implementation of App-contract + Infra-queries pattern with filter support and sat amount handling
- `src/Valt.Core/Common/BtcValue.cs` — sats semantics and arithmetic
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — `DashboardGridPanel` card layout and chart placement
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — `FetchAllReportsAsync` parallel fetch, filter handling, cached `IReportDataProvider`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IReportDataProvider` (`src/Valt.Infra/Modules/Reports/IReportDataProvider.cs`): pre-indexed frozen collections of accounts, categories, transactions-by-date, and historical BTC/fiat rates with binary-search lookups — ideal backing store for receipt-date BTC conversions.
- `MonthlyTotalsReport` (`src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`): already computes `BitcoinIncome`, `BitcoinExpense`, `BitcoinPurchased`, `BitcoinSold`, and fiat income/expense per month. The calculator pattern can be extended for BTC-denominated metrics.
- `SpendingEvolutionQueries` (`src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs`): demonstrates per-month aggregation, category/account/date filtering, and use of `TransactionEntity.SatAmount` for native sats.
- `CurrencyConversionService` (used by `SpendingEvolutionQueries`): provides source → USD → target conversion with fallback handling for missing rates.
- `DashboardData` / `RowItem` / `DashboardGridPanel` / `DashboardPanelViewModel`: dashboard card plumbing for the stack-velocity panel.
- `MonthlyTotalsChartData` / `ExpensesByCategoryChartData`: LiveCharts chart-data wrappers to mirror for the earned/spent grouped bars and stack-velocity line chart.

### Established Patterns
- Reports consume a cached `IReportDataProvider` per tab lifetime; `ReportsViewModel.FetchAllReportsAsync()` fetches in parallel via `Task.WhenAll` with per-panel `IsXLoading` flags and debounced filter updates.
- New App-layer modules use query/handler + `I*Queries` contract + DTOs in `Valt.App`, implemented in `Valt.Infra` (SpendingEvolution is the freshest example; AGENTS.md mandates this for new features).
- Multi-currency conversion chain: source currency → USD → target currency. For sats, target = BTC.
- UI changes in vertical slices (VM + XAML + tests) per PROJECT.md constraints.

### Integration Points
- `ReportsView.axaml`: add a new combined earned/spent chart panel and a separate stack-velocity panel after Phase 39 panels.
- `ReportsViewModel`: new observable properties, fetch methods wired into `FetchAllReportsAsync`, loading flags.
- DI registration for new query implementations (see existing registrations in the Reports/SpendingEvolution modules).
- FilterState/ReportsViewModel filter selections must flow into the new queries.

</code_context>

<specifics>
## Specific Ideas

- Skip transactions with missing receipt-date BTC prices silently (D-06/D-07); no warning UI.
- Stack velocity deliberately includes BTC purchases/sales while sats earned/spent excludes them, so the three metrics tell different stories (cash flow vs stack growth).
- Sats earned/spent use grouped bars; stack velocity uses a monthly line chart.
- New panels are appended after Phase 39 panels rather than replacing or interleaving them.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 40-BTC-Denominated Metrics Reports & UI*
*Context gathered: 2026-08-05*
