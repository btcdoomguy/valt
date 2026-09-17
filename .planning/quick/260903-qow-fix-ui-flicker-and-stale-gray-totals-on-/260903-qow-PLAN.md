# Quick Task 260903-qow: Fix UI flicker and stale gray totals on Transaction tab during price refresh

**Description:** During background price refreshes (LivePricesUpdaterJob runs every 30s), the UI flickers as if offline for a few seconds and the totals on the Transaction tab become gray showing old data, then turn back with the new data.

## Root Cause (verified in code)

`LivePricesUpdaterJob.RunAsync` publishes **two** `LivePriceUpdateMessage`s per cycle:

1. **Pre-fetch seed** (LivePricesUpdaterJob.cs lines 92-97): `_ratesProvider.GetLatestRatesAsync()` returns last price-database entries and is published immediately, with `IsUpToDate = false` hardcoded (PriceDatabaseRatesProvider.cs lines 49-52). These are last stored/closing rates — older than the current live rates.
2. **Live publish** (line 170): after the API fetch completes, merged live rates with computed `isUpToDate`.

Downstream, `RatesState.Receive` sets `IsUpToDate = message.IsUpToDate` and overwrites `BitcoinPrice`/`FiatRates` from each message; `TransactionsViewModel.AccountsTotalStateOnPropertyChanged` sets `IsRatesLive = _ratesState.IsUpToDate` (line 251), and `TransactionsView.axaml` lines 79/91/103 dim total opacity via `IsRatesLive`. So every 30s cycle: seed → gray + stale DB-closing prices → live publish → back to normal. Exactly the reported flicker.

The seed publish was introduced by quick task 260826-f66 to guarantee `RatesState` is never empty (prevents `KeyNotFoundException` in `AccountsTotalState.CalculateCurrentWealth`). It is only needed when no live rates have been published yet (startup); in steady state it is pure harm.

## Goal

In steady state, each `LivePricesUpdaterJob` cycle publishes exactly **one** `LivePriceUpdateMessage` (the live one), so the UI never transiently downgrades to offline/stale. Startup seeding and honest offline indication after API failures are preserved (no regression of 260826-f66).

## must_haves

- After a successful live publish, subsequent cycles do NOT publish the stored-rates seed before the live fetch (one message per cycle in steady state).
- On the first cycle (or before any successful live publish), stored rates are still published so `RatesState` is never empty (260826-f66 behavior kept).
- When the live API fails after a previous success, stored rates are published once with `IsUpToDate = false` so the UI honestly shows offline; repeated failures do not republish every 30s (no churn).
- Existing `LivePricesUpdaterJobTests` all still pass; a new regression test proves single-message-per-cycle in steady state.

## Tasks

### 1. Add regression test: single message per cycle in steady state

**Files:**
- `tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs`

**Action:**
- New test `Should_Publish_Single_Message_Per_Cycle_In_Steady_State`:
  - Register a `WeakReferenceMessenger` handler collecting all `LivePriceUpdateMessage`s into a `List<LivePriceUpdateMessage>` (unregister in `finally`).
  - Resolve the job ONCE from `_serviceProvider` (it is registered as singleton in `IntegrationTest`, so state carries across runs).
  - Run 1: good fiat + BTC providers (full rates, `UpToDate = true`, all configured currencies USD+BRL). Assert exactly 2 messages (seed + live — acceptable on the very first cycle, state was empty).
  - Clear the list. Run 2: good providers again. Assert exactly **1** message and `IsUpToDate == true`. THIS assertion currently fails (seed + live = 2 messages) — it is the RED regression test for the flicker.
  - Clear the list. Run 3: replace providers with failing ones (`HttpRequestException` / `TimeoutException` like the existing failing test). Assert exactly 1 message and `IsUpToDate == false` (honest offline after outage starts).
  - Clear the list. Run 4: failing providers again. Assert **0** new messages (offline published once, no 30s churn).
- Follow existing test style in this file (NSubstitute `ReplaceService`, same seed data from `SeedDatabase`).

**Verify:**
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.Jobs.LivePricesUpdaterJobTests" — the new test FAILS on the run-2 assertion (2 messages instead of 1), all pre-existing tests pass.

### 2. Gate seed publish and add offline-once semantics in LivePricesUpdaterJob

**Files:**
- `src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs`

**Action:**
- Add two private bool fields: `_hasPublishedLiveRates` and `_offlineNotified`.
- Pre-fetch block (current lines 92-97): still call `_ratesProvider.GetLatestRatesAsync` every cycle (the merged result needs `storedRates`), but only `PublishAsync(storedRates)` when `storedRates is not null && !_hasPublishedLiveRates`.
- In the live-fetch `catch` block (current lines 132-137): before returning, if `storedRates is not null && _hasPublishedLiveRates && !_offlineNotified`, publish `storedRates` and set `_offlineNotified = true`. Log as today. Do NOT throw — preserve current behavior of returning without a message when there is nothing to publish.
- After the successful live publish (line 170): set `_hasPublishedLiveRates = true` and `_offlineNotified = false`.
- Do NOT change the merge logic, `isUpToDate` computation, or `PreviousPrice` handling — 260826-f66 behavior must remain intact.
- Keep using `System.Threading.Lock` conventions; no new dependencies.

**Verify:**
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.Jobs.LivePricesUpdaterJobTests"` — all tests pass, including the new one (GREEN).
- `dotnet build Valt.sln` succeeds with no warnings introduced.

### 3. Audit remaining publish paths and run full suite

**Files:**
- (read-only audit; no source changes expected)

**Action:**
- Grep for all `PublishAsync` / messenger sends of `LivePriceUpdateMessage` across `src/` — confirm the only remaining producers are `LivePricesUpdaterJob` and `DatabaseLifecycleService.SeedRatesFromPriceDatabaseAsync` (startup / DB-reopen only, which is correct and fires before the job cycle).
- Re-read `RatesState.Receive` and confirm that with a single message per cycle it only updates atomically (BTC price, fiat rates merge, `IsUpToDate`, then one `RatesUpdated`) — no additional changes needed there. Do NOT change `IsUpToDate` semantics of the live publish (providers legitimately reporting stale keep the persistent-gray behavior, which is intended, not flicker).
- Run the full test suite: `dotnet test` — all green.

**Verify:**
- `dotnet test` full suite passes.
- `grep -rn "PublishAsync" src/Valt.Infra/Crawlers src/Valt.UI/Services` shows only the two expected producers.

## Notes for executor

- The failing-API pre-existing test `Should_Publish_Stored_Rates_When_Live_Api_Fails` uses a fresh job on its first cycle, so the gated seed publish (first cycle) still satisfies it — expect it to pass unchanged.
- `PriceDatabaseRatesProvider` and `DatabaseLifecycleService` must NOT be modified — they are the startup seed from 260826-f66.
- If run-1 of the new test yields only 1 message in some DI setup instead of 2 (e.g., if seed returns null), make the run-1 assertion tolerant: assert `>= 1` and that the last message has `IsUpToDate == true`; run-2 must be exactly 1 — that is the hard gate.
