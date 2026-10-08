# Phase 43: MCP, Localization, Documentation & Verification - Context

**Gathered:** 2026-08-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Make the v0.7 Insights & Metrics Expansion features (already built in Phases 39-42) accessible to AI assistants, fully localized, documented, and verified end-to-end.

**In scope:**
1. Expose the new v0.7 metrics through MCP report tools so AI assistants can query them.
2. Localize all new user-facing strings introduced in Phases 39-42 into English, Portuguese (pt-BR), and Spanish.
3. Update `.claude/docs/reports.md` to document the new reports, UI panels, and MCP tools.
4. Verify that the full test suite is green and that every new Reports tab panel works end-to-end with real data.

**Out of scope:**
- Adding new metrics or changing the definitions of the metrics already built in Phases 39-42.
- Refactoring existing MCP tools beyond what is required to add the new ones.
- Changing the UI layout or behavior of the new panels; only localization strings and documentation wording may change.
- Any feature work deferred to the v2 backlog or the future quality milestone (e.g., v0.4 hardening).

</domain>

<decisions>
## Implementation Decisions

### MCP tool exposure
- **D-01:** New v0.7 metrics are exposed through four category-level tools in `ReportTools.cs`: `GetSpendingAnalytics`, `GetBtcDenominatedMetrics`, `GetWealthPerformanceMetrics`, `GetLoanReports`. — **Reversibility:** one-way — changing the number/shape of public MCP tools after release breaks AI assistant callers and would require a deprecation/migration cycle.
- **D-02:** Each tool accepts only the parameters that make sense for its category (tailored parameters), not a uniform parameter set across all tools. — **Reversibility:** costly — changing the parameter contract after release requires updating callers and the [Description] attributes published to the MCP layer.
- **D-03:** Each tool returns a single nested combined DTO with sub-objects for each panel/metric in the category (e.g., `SpendingAnalyticsResultDto` containing savings-rate, burn-rate, and fixed-vs-variable sections). — **Reversibility:** one-way — the DTO shape is a public contract for MCP consumers; changing it breaks existing AI assistants.
- **D-04:** The new tools and their DTOs are added to the existing `src/Valt.Infra/Mcp/Tools/ReportTools.cs` file, keeping all report tools in one place. — **Reversibility:** reversible — moving to a separate file later is a local refactor that does not change the public MCP contract.

### Cross-cutting conventions (carried forward from Phases 39-42, not re-discussed)
- **D-05:** New user-facing strings added in Phases 39-42 were English-only; full pt-BR/es localization lands in this phase. All three language files (`language.resx`, `language.pt-BR.resx`, `language.es.resx`) must contain the same keys, with English fallback where native translations are not yet available.
- **D-06:** New queries and reports follow the existing App-layer CQRS pattern per `AGENTS.md`.
- **D-07:** Full test suite must be green, and every new Reports tab panel must be verified end-to-end with real data before the phase is marked complete.

### the agent's Discretion
- **Localization handoff:** How the pt-BR and es translations are produced (e.g., AI-generated + user review, user-provided exact strings, or English placeholders) and the quality bar for acceptance.
- **Documentation depth:** How detailed the `.claude/docs/reports.md` update should be for each new metric category (concise user-facing summary vs. code-level interface/DTO/algorithm detail vs. minimal reference to code and prior CONTEXT.md files).
- **End-to-end verification strategy:** The exact mix of automated integration tests, UI panel rendering tests, and manual smoke tests used to satisfy "verified end-to-end with real data".
- **Localization tooling:** Whether to use a design-time `.resx` generator script, manual file editing, or another workflow; the existing three-file pattern must be preserved.
- **DTO field naming and nesting:** The exact property names and section boundaries inside the nested combined DTOs, provided the public contract is clear and matches the UI panel structure.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope and requirements
- `.planning/ROADMAP.md` §Phase 43 — goal, success criteria, phase boundary, and dependencies on Phases 39-42.
- `.planning/REQUIREMENTS.md` §v0.7 Requirements — SPA-01..SPA-03, BTC-01..BTC-03, WLT-01..WLT-04, LON-01..LON-02 definitions.
- `.planning/PROJECT.md` — project constraints (tech stack, backward compatibility, vertical-slice UI rule, English-only-then-localize pattern).
- `.planning/phases/39-spending-analytics-reports-ui/39-CONTEXT.md` — prior phase decisions on CQRS, filters, English-only strings, panel patterns, and chart wiring.
- `.planning/phases/40-btc-denominated-metrics-reports-ui/40-CONTEXT.md` — prior phase decisions on monthly BTC conversion, filter handling, and section placement.
- `.planning/phases/41-wealth-performance-reports-ui/41-CONTEXT.md` — prior phase decisions on Reports tab layout, date-range handling, and dashboard conventions.
- `.planning/phases/42-loans-leverage-reports-ui/42-CONTEXT.md` — prior phase decisions on loan cost and liquidation-distance calculations, UI grouping, and conditional visibility.

### Module and feature documentation
- `.claude/docs/reports.md` — Reports module: `IReportDataProvider`, existing reports, `ReportsViewModel`, chart/dashboard patterns, DI registration, and file structure. Must be updated in this phase.
- `.claude/docs/assets.md` — BTC-backed loan and asset details; reference for loan-state semantics used by `GetLoanReports`.
- `.claude/docs/budget.md` — accounts, transactions, categories; reference for spending-analytics semantics.

### MCP layer
- `src/Valt.Infra/Mcp/Tools/ReportTools.cs` — existing report MCP tools and DTO patterns; the new v0.7 tools must extend this file per D-04.
- `src/Valt.Infra/Mcp/Server/McpServerService.cs` — server lifecycle and DI forwarding; any new services consumed by MCP tools must be added to `ForwardServicesFromMainApp()`.
- `src/Valt.Infra/Mcp/Server/McpServerState.cs` — MCP server state tracking.

### Application-layer report contracts (queries and DTOs to expose)
- `src/Valt.App/Modules/SpendingAnalytics/` — `GetBurnRateQuery`, `GetFixedVsVariableQuery`, `BurnRateDataDto`, `FixedVsVariableDataDto`, `IBurnRateQueries`, `IFixedVsVariableQueries`.
- `src/Valt.App/Modules/BtcDenominatedMetrics/` — `GetBtcDenominatedMetricsQuery`, `BtcDenominatedMetricsDataDto`, `IBtcDenominatedMetricsQueries`.
- `src/Valt.App/Modules/LoanReports/` — `GetLoanReportsQuery`, `LoanReportsDataDto`, `LoanCostMonthDto`, `LiquidationDistanceMonthDto`, `ILoanReportsQueries`.
- `src/Valt.Infra/Modules/Reports/` — `IReportDataProvider`, `ReportDataProvider`, `IReportDataProviderFactory`, and existing report implementations.
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/` — `IAllTimeHighReport`, `AllTimeHighReport`, `AllTimeHighData` (used by `GetWealthPerformanceMetrics` for days under water).
- `src/Valt.Infra/Modules/Reports/WealthOverview/` — `IWealthOverviewReport`, `WealthOverviewReport`, `WealthOverviewData`.
- `src/Valt.Infra/Modules/Reports/MonthlyTotals/` — `IMonthlyTotalsReport`, `MonthlyTotalsReport`, `MonthlyTotalsData`.
- `src/Valt.Infra/Modules/Reports/Statistics/` — `IStatisticsReport`, `StatisticsReport`, `StatisticsData`.

### UI and localization
- `src/Valt.UI/Lang/language.resx` — English source strings.
- `src/Valt.UI/Lang/language.pt-BR.resx` — Portuguese strings.
- `src/Valt.UI/Lang/language.es.resx` — Spanish strings.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — main orchestrator for fetching and caching report data; used for verification and for understanding panel wiring.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — panel layout and localization bindings.
- `src/Valt.UI/Views/Main/Tabs/Reports/Panels/` — existing panel ViewModels (e.g., `BurnRatePanelViewModel`, `BtcLoansPanelViewModel`) and new panel ViewModels added in Phases 39-42.

### Testing
- `tests/Valt.Tests/` — NUnit + NSubstitute test suite; `DatabaseTest` and `IntegrationTest` bases for verification.
- `AGENTS.md` — project conventions: update all three language files, use builders, and consider MCP impact when adding/modifying features.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ReportTools.cs` (`src/Valt.Infra/Mcp/Tools/ReportTools.cs`) already exposes `GetMonthlyTotals`, `GetWealthOverview`, `GetExpensesByCategory`, `GetIncomeByCategory`, `GetAllTimeHigh`, `GetMaxBtcStack`, and `GetStatistics` with a consistent pattern: create `IReportDataProvider`, call the report/query, map to a dedicated MCP DTO. The new v0.7 tools should reuse this pattern.
- `IReportDataProviderFactory` / `IReportDataProvider` provide pre-indexed, frozen collections and historical rate lookups, which the new tools can leverage.
- App-layer query modules (`SpendingAnalytics`, `BtcDenominatedMetrics`, `LoanReports`) provide the exact data contracts needed for the MCP DTOs; `WealthOverview` and `AllTimeHigh` reports already exist and cover the wealth-performance category.
- Existing localization files (`src/Valt.UI/Lang/language*.resx`) already contain keys for `BurnRate`, `StackVelocity`, `WealthOverview`, and `DaysUnderWater`; many other v0.7 strings (e.g., savings rate, fixed vs variable, sats earned/spent, loan cost, liquidation distance) exist as English-only placeholders or are missing and must be added.
- `.claude/docs/reports.md` documents the existing reports module structure, report types, UI layer, and DI registration; it is the primary target for the documentation update.

### Established Patterns
- MCP tools are static methods on a `[McpServerToolType]` class, decorated with `[McpServerTool]` and `[Description]`, and receive dependencies via method parameters injected from DI.
- DTOs are declared in the same file as the tools (or in a linked partial file), using `required init` properties and PascalCase names.
- New report DTOs use `IReportDataProviderFactory` to create a provider, then call the appropriate report or query handler.
- Localization strings are stored in `.resx` files, with a matching `language.Designer.cs` static property for each key. All three language files must be kept in sync.
- Reports tab panels follow the `ReportsViewModel.FetchAllReportsAsync()` parallel fetch pattern with per-panel `IsXLoading` / `IsXEmpty` / `IsXError` flags and debounced filter updates.
- `McpServerService.ForwardServicesFromMainApp()` must explicitly forward any new service that the MCP tools depend on, because the MCP server runs in a separate host.

### Integration Points
- Extend `ReportTools.cs` with the four new tools and their nested DTOs per D-04.
- If the new tools require services not already forwarded to the MCP server (e.g., new `I*Queries` contracts or new report interfaces), add them to `McpServerService.ForwardServicesFromMainApp()`.
- Add the same localization keys to all three `.resx` files and regenerate `language.Designer.cs` so the UI and documentation can reference them.
- Update `.claude/docs/reports.md` with new sections describing the v0.7 report categories, the UI panels that expose them, and the MCP tools that make them AI-accessible.
- Verify the new panels by running the full test suite (`dotnet test`) and performing an end-to-end UI smoke test with a real database; update any tests that are broken by localization or DTO changes.

</code_context>

<specifics>
## Specific Ideas

- The user explicitly chose to keep all report tools in `ReportTools.cs` rather than splitting into a new file or one-file-per-tool (D-04).
- The MCP tool surface should be coarse-grained (one tool per v0.7 category) with nested DTOs, not fine-grained panel-level tools (D-01, D-03).
- Parameters should be tailored to each category (D-02): e.g., `GetSpendingAnalytics` takes a date range + optional account/category filters, `GetWealthPerformanceMetrics` takes a date range + optional account filter, `GetLoanReports` takes a date range + currency.
- The existing `GetWealthOverview` tool already covers broad wealth overview; the new `GetWealthPerformanceMetrics` tool should focus on the Phase 41 additions (CAGR, fiat vs BTC allocation, best/worst months, days under water) rather than duplicating `GetWealthOverview`.
- No new metrics or metric definitions should be introduced; this phase only surfaces, translates, documents, and tests what Phases 39-42 already built.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within the Phase 43 boundary. Any new metrics, UI restructuring, or quality-hardening work remains in the v2 backlog or the future quality milestone.

</deferred>

---

*Phase: 43-MCP, Localization, Documentation & Verification*
*Context gathered: 2026-08-12*
