---
phase: quick
plan: 260903-qow
subsystem: LivePricesUpdaterJob / UI rates flicker
tags: [bugfix, background-jobs, rates, ui-flicker]
status: complete
requires:
  - 260826-f66 (startup seeding of RatesState from price database)
provides:
  - Single LivePriceUpdateMessage per job cycle in steady state
  - Honest offline-once indication on API failure after previous success
affects:
  - TransactionsView totals opacity (IsRatesLive)
  - RatesState.IsUpToDate
tech-stack:
  added: []
  patterns:
    - State-gated messenger publish (steady-state gating with latches)
key-files:
  created: []
  modified:
    - src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
    - tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs
decisions:
  - Gate the stored-rates seed publish behind _hasPublishedLiveRates instead of removing it, preserving 260826-f66 startup seeding
  - Publish stored rates once (IsUpToDate=false) when the live API fails after a previous success; latch with _offlineNotified to avoid 30s churn
  - Hoisted storedRates declaration out of the try block so the catch block can access it for offline-once publish
metrics:
  duration: 12 min
  completed: 2026-09-03
actuals:
  tokens: 9500
  tasks: 3
  commits: 2
---

# Quick Task 260903-qow: Fix UI flicker and stale gray totals on Transaction tab during price refresh

**Summary:** LivePricesUpdaterJob now publishes exactly one `LivePriceUpdateMessage` per cycle in steady state (the live one), eliminating the 30s gray/stale flicker on the Transaction tab; startup seeding (260826-f66) and honest offline indication after API failures are preserved.

## Root Cause

Every 30s cycle published two messages: a pre-fetch seed of stored/closing rates (`IsUpToDate = false`) and the merged live rates. `RatesState` downgraded to offline/gray on each seed, then recovered — the reported flicker.

## Changes Made

### Task 1 — Regression test (RED, commit `1a02afc`)

Added `Should_Publish_Single_Message_Per_Cycle_In_Steady_State` to `LivePricesUpdaterJobTests`. It resolves the job once and drives four cycles on the same instance:

1. First cycle: `>= 1` message, last is `IsUpToDate == true` (seed + live acceptable on empty state).
2. Steady state: **exactly 1** message, `IsUpToDate == true` — the hard gate (was 2, failing).
3. Providers start failing: exactly 1 message, `IsUpToDate == false` (honest offline).
4. Providers still failing: **0** messages (no churn).

Verified RED before the fix: new test failed on run-2 assertion (2 messages), all 3 pre-existing tests passed.

### Task 2 — Gating in LivePricesUpdaterJob (GREEN, commit `5913b7d`)

- Added `_hasPublishedLiveRates` and `_offlineNotified` bool fields.
- Seed publish now only fires when `storedRates is not null && !_hasPublishedLiveRates` (first cycle only).
- Live-fetch `catch` block publishes stored rates once (`IsUpToDate = false`) when a previous live publish succeeded and offline was not yet notified; does not throw otherwise.
- Successful live publish sets `_hasPublishedLiveRates = true` and `_offlineNotified = false`.
- Hoisted `storedRates` declaration above the `try` so the `catch` can access it.
- Merge logic, `isUpToDate` computation, and `PreviousPrice` handling untouched (260826-f66 intact).

### Task 3 — Publish-path audit + full suite (no code changes)

- Grep audit: only two `LivePriceUpdateMessage` producers remain — `LivePricesUpdaterJob` and `DatabaseLifecycleService.SeedRatesFromPriceDatabaseAsync` (startup/DB-reopen only). All other `PublishAsync` calls emit unrelated message types.
- `RatesState.Receive` confirmed atomic per message (BTC price, fiat merge, `IsUpToDate`, one `RatesUpdated`) — no change needed.
- Full suite: `dotnet test` → **1777 passed, 0 failed, 0 skipped**.

## Deviations from Plan

### 1. [Rule 3 - Blocking issue] Behavior switched via substitute flag instead of `ReplaceService` in the new test
- **Found during:** Task 1
- **Issue:** The plan suggested swapping in failing providers via `ReplaceService` for runs 3–4. However, `ReplaceService` calls `RebuildServiceProvider()`, and the job singleton is constructed per-provider with its providers captured at construction — replacing services after job resolution would leave the job using the old providers, and re-resolving would create a fresh job instance with reset gating state, defeating the test.
- **Fix:** Used NSubstitute conditional `Returns` driven by a closure `providersFailing` flag on the same substitutes, keeping a single job instance across all four runs. Semantics identical to the plan's intent (and consistent with the plan's "Notes for executor" tolerance for DI complications).
- **Files modified:** `tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs`
- **Commit:** `1a02afc`

## Test Results

- `dotnet test --filter "FullyQualifiedName~Valt.Tests.Jobs.LivePricesUpdaterJobTests"`: 4/4 passed (RED verified before fix: new test failed on run-2).
- `dotnet test` (full suite): 1777 passed, 0 failed, 0 skipped.
- `dotnet build` of Valt.Infra: no new warnings from the modified file (pre-existing CS8602 warnings in MCP tool files are out of scope).

## Self-Check: PASSED

- `src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs` — FOUND (modified, commit 5913b7d)
- `tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs` — FOUND (modified, commit 1a02afc)
- Commits `1a02afc`, `5913b7d` verified in `git log`
