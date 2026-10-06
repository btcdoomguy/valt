---
phase: 41-wealth-performance-reports-ui
plan: 01
subsystem: ui
tags: [avalonia, reports, all-time-high, assets, days-under-water]

requires:
  - phase: 40-btc-denominated-metrics-reports-ui
    provides: Reports tab dashboard/chart wiring and filter conventions used for the new ATH row.

provides:
  - Asset-aware AllTimeHighReport that includes active net-worth assets in daily gross wealth.
  - DaysUnderWater property on AllTimeHighData.
  - Days under water row in the existing All Time High dashboard panel.
  - Unit tests covering asset inclusion/exclusion and DaysUnderWater scenarios.

affects:
  - 41-wealth-performance-reports-ui (subsequent WLT-01/02/03 plans)
  - 43-mcp-localization-documentation-verification

actuals:
  tokens: 4700
  tasks: 3
  commits: 4

tech-stack:
  added: []
  patterns:
    - Infra report consumes App-layer IAssetQueries query contract (existing DI registration).
    - Source-currency -> USD -> target-currency conversion reused from account balance path.

key-files:
  created: []
  modified:
    - src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs
    - src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighData.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs
    - tests/Valt.Tests/Reports/AllTimeHighReportTests.cs

key-decisions:
  - "Asset value approximation: used AssetDTO.CurrentValue for every active day because historical asset prices are not available; documented as approximation in a code comment."
  - "English-only UI string added now; pt-BR/es localization deferred to Phase 43 per D-14."

requirements-completed:
  - WLT-04

coverage:
  - id: D1
    description: "Asset-aware AllTimeHighReport includes active net-worth assets and computes DaysUnderWater."
    requirement: WLT-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/AllTimeHighReportTests.cs#Should_Include_Active_NetWorth_Asset_In_AllTimeHigh, Should_Exclude_NonNetWorth_And_Sold_Assets_From_AllTimeHigh, Should_Calculate_DaysUnderWater_Based_On_Peak_Date"
        status: pass
    human_judgment: false
  - id: D2
    description: "Existing All Time High dashboard panel renders the new Days under water row."
    requirement: WLT-04
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs (build + existing VM tests pass)"
        status: pass
    human_judgment: true
    rationale: "No automated UI rendering test exists; visual confirmation and layout check belong to Phase 43 end-to-end verification."
  - id: D3
    description: "Tests verify asset inclusion/exclusion behavior and DaysUnderWater values."
    requirement: WLT-04
    verification:
      - kind: unit
        ref: "dotnet test --filter FullyQualifiedName~AllTimeHigh"
        status: pass
    human_judgment: false

duration: 14min
completed: 2026-08-10
status: complete
---

# Phase 41 Plan 01: WLT-04 Tracer Summary

**Asset-aware All Time High report with active net-worth assets and a Days under water row in the existing ATH dashboard panel.**

## Performance

- **Duration:** 14 min
- **Started:** 2026-08-10T17:52:06Z
- **Completed:** 2026-08-10T18:05:40Z
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments
- `AllTimeHighReport` now injects `IAssetQueries` and adds active net-worth assets to daily gross wealth using the same source-currency -> USD -> target-currency path as accounts.
- `AllTimeHighData` exposes `DaysUnderWater` computed as whole days between the report end date (yesterday) and the ATH date.
- The existing All Time High dashboard panel displays a new "Days under water" row immediately after the ATH date row.
- Added NSubstitute-based tests for active asset inclusion, non-net-worth/sold asset exclusion, and zero vs. positive DaysUnderWater scenarios.

## Task Commits

Each task was committed atomically:

1. **Task 1: WLT-04 tracer: Make AllTimeHighReport asset-aware and expose days under water** - `15ea2c9` (feat)
2. **Task 2: Wire days under water into the existing All Time High dashboard panel** - `5f485d3` (feat)
3. **Task 3: Verify asset-aware ATH and days-under-water with tests** - `208307e` (test)

**Environment cleanup:** `4b1b9a9` (chore) - added `.gsd/` to `.gitignore` for generated executor artifacts.

**Plan metadata:** *(pending final docs commit)*

## Files Created/Modified
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs` - Injects `IAssetQueries`, adds active asset value to daily total, computes `DaysUnderWater`.
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighData.cs` - Adds `DaysUnderWater` property.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - Adds "Days under water" `RowItem` after ATH date row.
- `src/Valt.UI/Lang/language.resx` - Adds English `Reports.AllTimeHigh.DaysUnderWater` string.
- `src/Valt.UI/Lang/language.Designer.cs` - Adds generated `Reports_AllTimeHigh_DaysUnderWater` property.
- `tests/Valt.Tests/Reports/AllTimeHighReportTests.cs` - Updates existing tests to mock `IAssetQueries`; adds three new asset/DaysUnderWater tests.
- `.gitignore` - Ignores `.gsd/` executor artifacts.

## Decisions Made
- Used `AssetDTO.CurrentValue` for every day an asset is active because historical asset prices are unavailable; commented this approximation in `AllTimeHighReport`.
- Added the English-only string `Reports.AllTimeHigh.DaysUnderWater` now; pt-BR/es translations deferred to Phase 43 per D-14.
- Updated the existing `AllTimeHighReportTests` constructors to supply a mock `IAssetQueries` so the build stayed green across the constructor change.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

### Pre-existing full-suite failures (out of scope)

`dotnet test` reports four failures unrelated to this plan:

| Test | Error | Likely cause |
|------|-------|--------------|
| `FrankfurterFiatHistoricalProviderTests.Should_Get_Prices` | Expected 257 prices, got 0 | External API unavailable / response empty |
| `BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` | 403 Forbidden | External API access denied |
| `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date` | 403 Forbidden | External API access denied |
| `FrankfurterFiatProviderTests.Should_Get_Prices` | HttpClient timeout | External API unreachable |

All four are live third-party API integration tests. They fail in the current environment and are not caused by the WLT-04 changes. The plan-specific `dotnet test --filter "FullyQualifiedName~AllTimeHigh"` and `dotnet test --filter "FullyQualifiedName~ReportsViewModel"` suites pass.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- WLT-04 (days under water) is implemented and tested.
- Subsequent Phase 41 plans can build on the asset-aware `AllTimeHighReport` for WLT-01 (CAGR), WLT-02 (allocation), and WLT-03 (best/worst months).
- The panel row is ready for Phase 43 localization and end-to-end visual verification.

## Self-Check: PASSED

- [x] `41-01-SUMMARY.md` created at `.planning/phases/41-wealth-performance-reports-ui/41-01-SUMMARY.md`
- [x] `deferred-items.md` created at `.planning/phases/41-wealth-performance-reports-ui/deferred-items.md`
- [x] Commit `15ea2c9` exists (Task 1)
- [x] Commit `5f485d3` exists (Task 2)
- [x] Commit `208307e` exists (Task 3)
- [x] `dotnet build Valt.sln` succeeds
- [x] `dotnet test --filter "FullyQualifiedName~AllTimeHigh"` passes (7 tests)
- [x] `dotnet test --filter "FullyQualifiedName~ReportsViewModel"` passes (12 tests)

---
*Phase: 41-wealth-performance-reports-ui*
*Completed: 2026-08-10*
