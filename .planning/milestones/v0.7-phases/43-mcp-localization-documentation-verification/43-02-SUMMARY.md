---
phase: 43-mcp-localization-documentation-verification
plan: 02
subsystem: mcp
tags: [mcp, reports, btc-denominated-metrics, loan-reports, wealth-performance, integration-tests]

requires:
  - phase: 43-01
    provides: MCP report tool pattern, DTO conventions, and `ReportTools` test harness
  - phase: 40-btc-denominated-metrics-reports-ui
    provides: `GetBtcDenominatedMetricsQuery` and `BtcDenominatedMetricsDataDto`
  - phase: 41-wealth-performance-reports-ui
    provides: `IAllTimeHighReport`, `IWealthOverviewReport`, and related data models
  - phase: 42-loans-leverage-reports-ui
    provides: `GetLoanReportsQuery` and `LoanReportsDataDto`

provides:
  - `GetBtcDenominatedMetrics` MCP tool returning nested BTC-denominated metrics DTO
  - `GetLoanReports` MCP tool returning nested loan cost and liquidation-distance DTOs
  - `GetWealthPerformanceMetrics` MCP tool returning ATH and wealth-overview DTOs
  - Integration tests covering all three new tools in `ReportToolsTests.cs`

affects:
  - 43-03
  - 43-04
  - 43-05

actuals:
  tokens: 4330
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - Reuse existing App-layer query handlers via `IQueryDispatcher`
    - Reuse existing report interfaces via `IReportDataProviderFactory`
    - Nested `required init` DTOs inside `ReportTools` for the MCP public contract

key-files:
  created: []
  modified:
    - src/Valt.Infra/Mcp/Tools/ReportTools.cs
    - tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs

key-decisions:
  - Kept unimplemented v0.7 metrics (CAGR, fiat-vs-BTC allocation, best/worst months) out of the MCP surface; documented the limitation in the method XML doc and integration test.
  - Made `SeedPriceData` idempotent so multiple `ReportToolsTests` can run in the same fixture without duplicate-date LiteDB exceptions.

patterns-established:
  - "MCP report tools wrap existing App/Infra query and report services, exposing a single combined nested DTO per D-03."
  - "Integration tests seed the minimum realistic data set (accounts, transactions, loan asset, price data) and assert against concrete fields."

requirements-completed: []

coverage:
  - id: D1
    description: "GetBtcDenominatedMetrics MCP tool returns monthly sats earned, sats spent, and stack velocity"
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs#GetBtcDenominatedMetrics_WithIncomeExpenseAndBtcPurchase_ReturnsNonEmptyMonths"
        status: pass
    human_judgment: false
  - id: D2
    description: "GetLoanReports MCP tool returns active-loan flag, monthly cost breakdown, and liquidation-distance trend"
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs#GetLoanReports_WithActiveBtcLoan_ReturnsActiveLoanAndCostMonths"
        status: pass
    human_judgment: false
  - id: D3
    description: "GetWealthPerformanceMetrics MCP tool returns days-under-water from ATH and monthly wealth-overview items"
    verification:
      - kind: integration
        ref: "tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs#GetWealthPerformanceMetrics_WithTransactionData_ReturnsAthAndWealthOverview"
        status: pass
    human_judgment: false

duration: 16min
completed: 2026-08-12
status: complete
---

# Phase 43 Plan 02: v0.7 MCP Report Tools (BTC, Loan, Wealth) Summary

**Added three v0.7 category-level MCP report tools (`GetBtcDenominatedMetrics`, `GetLoanReports`, `GetWealthPerformanceMetrics`) with nested DTOs and passing integration tests.**

## Performance

- **Duration:** 16 min
- **Started:** 2026-08-12T18:53:03Z
- **Completed:** 2026-08-12T19:08:54Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Implemented `GetBtcDenominatedMetrics` dispatching `GetBtcDenominatedMetricsQuery` and mapping `BtcDenominatedMetricsDataDto` to a nested result DTO.
- Implemented `GetLoanReports` dispatching `GetLoanReportsQuery` and mapping cost and liquidation-distance month data to nested DTOs.
- Implemented `GetWealthPerformanceMetrics` calling `IAllTimeHighReport` and `IWealthOverviewReport` through `IReportDataProviderFactory`, mapping to a combined result DTO.
- Added integration tests that seed realistic data and assert the tools return non-empty, populated results.
- Verified `dotnet build Valt.sln` succeeds and `dotnet test --filter "FullyQualifiedName~ReportToolsTests"` passes all four tests.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add `GetBtcDenominatedMetrics` and `GetLoanReports` MCP tools** - `0640a3f` (feat)
2. **Task 2: Add `GetWealthPerformanceMetrics` MCP tool** - `6b40649` (feat)

**Plan metadata:** skipped (`.planning/` is gitignored — STATE.md, ROADMAP.md, and SUMMARY.md updated locally only)

## Files Created/Modified

- `src/Valt.Infra/Mcp/Tools/ReportTools.cs` - Added `GetBtcDenominatedMetrics`, `GetLoanReports`, and `GetWealthPerformanceMetrics` static methods plus nested DTO classes (`BtcDenominatedMetricsResultDto`, `BtcDenominatedMetricsMonthResultDto`, `LoanReportsResultDto`, `LoanCostMonthResultDto`, `LiquidationDistanceMonthResultDto`, `WealthPerformanceMetricsResultDto`, `AllTimeHighSectionDto`, `WealthOverviewSectionDto`, `WealthOverviewItemResultDto`).
- `tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs` - Added integration tests for the three new tools; made `SeedPriceData` idempotent across tests.

## Decisions Made

- Followed the established pattern from Plan 43-01: each new tool returns a single nested combined DTO, and integration tests run against the real DI container.
- Deliberately omitted unimplemented v0.7 wealth-performance metrics (CAGR, allocation, best/worst months) and documented the limitation in the XML doc and test comments.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Made `SeedPriceData` idempotent in `ReportToolsTests.cs`**
- **Found during:** Task 1 (integration test for `GetBtcDenominatedMetrics` and `GetLoanReports`)
- **Issue:** Multiple tests in the same `IntegrationTest` fixture call `SeedPriceData` with the same date; LiteDB throws on duplicate `BitcoinDataEntity.Date` insert when the second test runs.
- **Fix:** Added an existence check before inserting the BTC and fiat price entities so subsequent tests reuse already-seeded rows.
- **Files modified:** `tests/Valt.Tests/Infra/Mcp/Tools/ReportToolsTests.cs`
- **Verification:** `dotnet test --filter "FullyQualifiedName~ReportToolsTests"` passes all four tests.
- **Committed in:** `0640a3f` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Minor test-harness fix required to run the new integration tests reliably. No scope creep.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All four category-level v0.7 MCP report tools are now implemented and tested.
- Ready for Plan 43-03 (localization pass for new report strings) and Plan 43-04/43-05 (documentation/verification updates).

---
*Phase: 43-mcp-localization-documentation-verification*
*Completed: 2026-08-12*
