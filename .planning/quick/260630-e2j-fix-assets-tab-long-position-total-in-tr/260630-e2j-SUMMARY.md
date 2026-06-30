---
phase: quick
plan: 260630-e2j
subsystem: assets
tags: [dotnet, avalonia, assets, leveraged-positions, btc-collateral, pnl, asset-summary, tests]

requires: []
provides:
  - Correct LONG BTC-collateral leveraged position summary value using PnL only
  - Unit tests for GetAssetSummaryHandler leveraged position totals
affects:
  - Assets tab total value
  - Transactions tab wealth summary (derives from AssetSummary)

tech-stack:
  added: []
  patterns: []

key-files:
  created:
    - tests/Valt.Tests/Application/Assets/Queries/GetAssetSummaryHandlerTests.cs
  modified:
    - src/Valt.Infra/Modules/Assets/Queries/AssetQueries.cs

key-decisions:
  - "LONG BTC-collateral leveraged positions contribute only PnL to asset summary so BTC collateral is not double-counted (it is already tracked as BTC holdings)"
  - "SHORT BTC-collateral leveraged positions preserve existing CalculateCurrentValue behavior"
  - "Fiat-collateral leveraged positions continue to use CalculatePnL as before"

patterns-established: []

requirements-completed: []

duration: 18min
completed: 2026-06-30
status: complete
---

# Quick Task 260630-e2j: Fix Assets tab LONG position total in Transactions tab totals summary

**LONG BTC-collateral leveraged positions now contribute only PnL to asset and Transactions-tab wealth totals, with unit tests verifying the corrected summary behavior.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-06-30T00:00:00Z
- **Completed:** 2026-06-30T00:18:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Fixed `AssetQueries.GetValueForSummary` to return `CalculatePnL` for LONG BTC-collateral leveraged positions instead of `CalculateCurrentValue`.
- Preserved existing behavior for SHORT BTC-collateral positions (full current value) and fiat-collateral positions (PnL only).
- Updated XML documentation on `GetValueForSummary` to describe the new LONG BTC-collateral behavior.
- Added `GetAssetSummaryHandlerTests` with three assertions proving:
  - Combined LONG leveraged positions use PnL-only totals.
  - LONG BTC-collateral position's contribution equals its PnL, which is less than its current value.
  - LONG fiat-collateral position continues to contribute only its PnL.

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix LONG BTC-collateral leveraged position summary value** - `ea3405e` (fix)
2. **Task 2: Add unit tests for asset summary leveraged position totals** - `0364ffe` (test)

## Files Created/Modified

- `src/Valt.Infra/Modules/Assets/Queries/AssetQueries.cs` - Updated `GetValueForSummary` logic and XML comment for LONG BTC-collateral leveraged positions.
- `tests/Valt.Tests/Application/Assets/Queries/GetAssetSummaryHandlerTests.cs` - New unit tests verifying PnL-only summary totals for LONG BTC-collateral and fiat-collateral leveraged positions.

## Decisions Made

- Followed the plan's specified behavior exactly: LONG BTC-collateral uses PnL only, SHORT BTC-collateral keeps current value, fiat-collateral keeps PnL.
- Used per-asset satoshi conversion expectations in tests to match the implementation's rounding behavior.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Initial test assertion for `TotalValueInSats` used a single combined conversion and expected 35,000,000 sats, but the implementation converts each asset's value to sats individually and sums the truncated results, yielding 34,999,999 sats. Updated the test to compute per-asset expected sats using the same rounding semantics.
- Full test suite has two pre-existing network-dependent failures (`BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` and `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date`) returning HTTP 403. These are unrelated to this change and already documented as deferred live-API debt for Phase 25.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Asset summary calculation is now consistent with the intended net-worth semantics for LONG BTC-collateral leveraged positions.
- No blockers.

## Self-Check: PASSED

- [x] `src/Valt.Infra/Modules/Assets/Queries/AssetQueries.cs` modified and committed (`ea3405e`).
- [x] `tests/Valt.Tests/Application/Assets/Queries/GetAssetSummaryHandlerTests.cs` created and committed (`0364ffe`).
- [x] `dotnet build Valt.sln` succeeds with no errors.
- [x] `dotnet test --filter "FullyQualifiedName~Valt.Tests.Application.Assets.Queries.GetAssetSummaryHandlerTests"` passes (3/3).

---
*Quick task: 260630-e2j*
*Completed: 2026-06-30*
