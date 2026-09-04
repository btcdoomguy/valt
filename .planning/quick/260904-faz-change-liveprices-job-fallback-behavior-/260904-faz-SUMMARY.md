---
phase: quick/260904-faz-change-liveprices-job-fallback-behavior-
plan: 01
status: complete
subsystem: infrastructure/background-jobs
tags: [live-prices, background-job, fallback, offline]
dependency-graph:
  requires: []
  provides:
    - Outage after success publishes previous live prices (IsUpToDate=false) instead of stored-database rates
    - Stored rates remain cold-start fallback for first-ever failure
    - Steady-state gating (no republish churn) preserved
  affects:
    - src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
    - tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs
tech-stack:
  added: []
  patterns:
    - Last-known-good cache via _fiatUsdPrice/_btcPrice fields; offline notification gated by _offlineNotified
key-files:
  created: []
  modified:
    - src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
    - tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs
decisions:
  - Republish captured live-price fields on failure instead of stored rates; stored rates stay cold-start-only via the unchanged seed block
metrics:
  duration: 6 min
  completed: 2026-09-04
  tasks: 2
  commits: 2
actuals:
  tokens: 800
  tasks: 2
  commits: 2
---

# Quick Task 260904-faz: LivePricesUpdaterJob Fallback Behavior Summary

LivePricesUpdaterJob now keeps the last successfully fetched live BTC/fiat prices during a transient outage (published once with IsUpToDate=false), while stored price-database rates remain the fallback only when no live response has ever succeeded (cold start).

## What Was Built

- **Steady-state outage behavior changed:** the catch block in `RunAsync` previously republished stored price-database rates when `_hasPublishedLiveRates` was true. It now republishes the captured live-price fields `_btcPrice`/`_fiatUsdPrice` with `IsUpToDate: false`, keeping the `_offlineNotified` gate so at most one offline message is published per outage.
- **Cold-start fallback preserved:** when the very first fetch fails, the seed block (lines ~96-101) still publishes stored rates before the failing fetch; the catch block makes no further publication in that case.
- **Successful path unchanged:** merge logic, last-closing-price logic, and `_hasPublishedLiveRates`/`_offlineNotified` resets are untouched.

## Tasks Executed

| Task | Name | Commit |
|------|------|--------|
| 1 | Pin new fallback behavior with failing tests (TDD RED) | e5f570e |
| 2 | Keep last live prices on failure; stored fallback only for cold start | ca71f63 |

## Verification

- `dotnet test --filter "FullyQualifiedName~LivePricesUpdaterJobTests"` — **Passed: 5, Failed: 0** (including strengthened steady-state outage assertions checking BTC 20000m / BRL 5.2m, the new `Should_Keep_Previous_Live_Prices_When_Subsequent_Fetch_Fails` test, and the unchanged cold-start stored-rates test).
- RED gate confirmed before implementation: the 2 new/strengthened assertions failed with stored rates (10000m / 5m) published instead of previous live prices (20000m / 5.2m).
- `dotnet build Valt.sln` — 0 warnings, 0 errors.

## Success Criteria

- [x] Failed fetch after a successful cycle publishes previous live prices (IsUpToDate=false) instead of stored-database rates
- [x] First-ever failure still falls back to stored rates
- [x] No republish churn during continued outages

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Named argument casing mismatch**
- **Found during:** Task 2
- **Issue:** Used `isUpToDate: false` named argument, but the `LivePriceUpdateMessage` positional record parameter is `IsUpToDate` (capital I), causing `error CS1739`.
- **Fix:** Changed to `IsUpToDate: false`.
- **Files modified:** src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
- **Commit:** ca71f63

## Known Stubs

None.

## Self-Check: PASSED

- FOUND: src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs (modified in ca71f63)
- FOUND: tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs (modified in e5f570e)
- FOUND: commits e5f570e and ca71f63 in git log
