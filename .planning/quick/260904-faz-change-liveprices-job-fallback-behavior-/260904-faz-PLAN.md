---
phase: quick/260904-faz-change-liveprices-job-fallback-behavior-
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
  - tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs
autonomous: true
requirements:
  - QUICK-260904-FAZ
must_haves:
  truths:
    - When a live price fetch fails after at least one successful cycle, the published LivePriceUpdateMessage carries the previously fetched live BTC/fiat prices (not stored price-database rates), with IsUpToDate=false
    - When the very first live price fetch fails (never a successful response), stored price-database rates are still published as the fallback
    - Steady-state message gating is preserved (no republish churn during a continued outage)
  artifacts:
    - src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs
    - tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs
  key_links:
    - LivePricesUpdaterJob catch block -> re-publish of _btcPrice/_fiatUsdPrice fields captured from the last successful cycle
    - Seed block (stored rates published when _hasPublishedLiveRates is false) -> unchanged first-run fallback path
---

<objective>
Change the LivePricesUpdaterJob fallback behavior so that a failed price request after a previously successful request keeps the last live prices, while stored price-database data is used only when there has never been a successful live response.

Purpose: During a transient outage, the UI currently flips to old stored-database rates, causing visible price jumps; keeping the last known live price is the honest, stable behavior. Stored data remains the cold-start fallback.
Output: Modified job + updated tests proving both fallback paths.
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
@/home/vmabellini/.config/opencode/gsd-core/templates/summary.md
</execution_context>

<context>
@AGENTS.md

Read before implementing:
- src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs (current catch block at lines ~136-149 publishes storedRates on failure; fields _fiatUsdPrice/_btcPrice hold the last successful response; _hasPublishedLiveRates gates steady state)
- tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs (existing fallback and steady-state tests)
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Pin the new fallback behavior with failing tests</name>
  <files>tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs</files>
  <behavior>
    - Steady-state outage (existing test Should_Publish_Single_Message_Per_Cycle_In_Steady_State, Run 3): after providers start failing, the single published message must carry the LAST LIVE prices (BTC USD price 20000m, BRL 5.2m) and IsUpToDate=false — NOT the stored-database rates (BTC 10000m, BRL 5m). Strengthen the existing assertions to check prices, not just message count and IsUpToDate.
    - Cold-start failure (existing test Should_Publish_Stored_Rates_When_Live_Api_Fails): first cycle with failing providers must still publish stored rates (BTC USD 10000m, BRL 5m) — this test should remain passing unchanged; use it to confirm the stored fallback is preserved for the never-succeeded case.
    - New test Should_Keep_Previous_Live_Prices_When_Subsequent_Fetch_Fails: one successful cycle (live BTC 20000m, BRL 5.2m), then providers fail; assert exactly one new message is published containing the previous live prices (20000m / 5.2m) and no message containing stored rates (10000m / 5m) is published.
  </behavior>
  <action>
    Update tests/Valt.Tests/Jobs/LivePricesUpdaterJobTests.cs: (1) in Should_Publish_Single_Message_Per_Cycle_In_Steady_State Run 3, add assertions that messages[0].Btc USD item Price == 20000m and the BRL fiat item Price == 5.2m; (2) add the new test Should_Keep_Previous_Live_Prices_When_Subsequent_Fetch_Fails following the same Substitute-provider pattern, switching providers to failing via a captured bool after the first RunAsync; assert a single new message with the previous live prices. Do NOT modify production code in this task. Run the tests and confirm the strengthened/new assertions FAIL against the current implementation (the job currently publishes storedRates on failure).
  </action>
  <verify>
    <automated>dotnet test --filter "FullyQualifiedName~LivePricesUpdaterJobTests" 2>&1 | tail -20</automated>
  </verify>
  <done>New/strengthened tests exist and fail for the right reason: the outage message currently contains stored rates (BTC 10000m / BRL 5m) instead of previous live prices (20000m / 5.2m).</done>
</task>

<task type="auto" tdd="true">
  <name>Task 2: Keep last live prices on failure; stored fallback only for cold start</name>
  <files>src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs</files>
  <behavior>
    - Catch block, steady state (_hasPublishedLiveRates true and _fiatUsdPrice/_btcPrice non-null): publish one LivePriceUpdateMessage built from the stored fields _btcPrice and _fiatUsdPrice with isUpToDate=false, set _offlineNotified=true. Message published at most once per outage (existing _offlineNotified gate preserved).
    - Catch block, never-succeeded case: no new publication from the catch block — the seed block earlier in RunAsync already published storedRates; just return.
    - Successful path (merge, publish, _hasPublishedLiveRates/_offlineNotified resets) unchanged.
  </behavior>
  <action>
    In LivePricesUpdaterJob.RunAsync, rewrite the catch block: when _hasPublishedLiveRates is true AND _fiatUsdPrice is not null AND _btcPrice is not null AND !_offlineNotified, publish new LivePriceUpdateMessage(_btcPrice, _fiatUsdPrice, isUpToDate: false), set _offlineNotified = true, and log that previous live prices are kept due to fetch failure. Remove the storedRates publication from the catch path (stored rates remain the cold-start fallback via the unchanged seed block at the top of the try). Keep the catch returning early and keep all other behavior (merge, last-closing-price logic, steady-state gating) identical. Follow AGENTS.md conventions: Lock class for any new synchronization (none expected), no attributes in domain classes, NUnit + NSubstitute test style.
  </action>
  <verify>
    <automated>dotnet test --filter "FullyQualifiedName~LivePricesUpdaterJobTests" && dotnet build Valt.sln</automated>
  </verify>
  <done>All LivePricesUpdaterJobTests pass (including the strengthened steady-state assertions and the new keep-previous-prices test); solution builds clean; failure path after a success publishes previous live prices once with IsUpToDate=false, and cold-start failure still publishes stored rates.</done>
</task>

</tasks>

<verification>
- dotnet test --filter "FullyQualifiedName~LivePricesUpdaterJobTests" — all green
- dotnet build Valt.sln — no warnings introduced
</verification>

<success_criteria>
Failed fetch after a successful cycle publishes the previous live prices (IsUpToDate=false) instead of stored-database rates; first-ever failure still falls back to stored rates; no republish churn during continued outages.
</success_criteria>

<output>
Create `.planning/quick/260904-faz-change-liveprices-job-fallback-behavior-/260904-faz-SUMMARY.md` when done
</output>
