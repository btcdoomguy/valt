# Valt

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, external investments (assets), and displays values in bitcoin terms. As of v0.5, users can also mark assets as sold, keep a record in a dedicated Asset Sold History screen, and undo a sale to restore the asset to the active view. As of v0.6, the public Valt documentation site has been refreshed to reflect all shipped features, correct factual errors, and add missing pages including Settings & Configuration.

## Core Value

Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## Requirements

### Validated

- ✓ Track fiat and bitcoin accounts — Phase 1-3
- ✓ Record transactions between accounts and categories — Phase 1-3
- ✓ Manage asset groups and external investments (BTC, stocks, ETFs, real estate, etc.) — Phase 1-3
- ✓ BTC-backed loans with collateral, APR, LTV, and liquidation tracking — Phase 1-3
- ✓ Reports dashboard with wealth, leverage, and BTC loan summaries — Phase 1-3
- ✓ Custom BTC price simulation for reports — Phase 4-5
- ✓ Allow users to record BTC loan state changes over time (fee %, current debt, collateral, amount taken, effective date) — Phase 7-9
- ✓ Use the latest recorded loan state as the basis for current-value calculations, falling back to previous entries when the latest is deleted — Phase 6-7
- ✓ Provide an "Update Loan State" screen prefilled with current calculated totals, accessible from the Assets tab context menu — Phase 8
- ✓ Provide a loan state history screen listing all recorded changes with delete and add-new-state actions — Phase 9
- ✓ Auto-seed existing loans with an initial state entry derived from their setup values — Phase 6
- ✓ Localize all new user-facing strings across `language.resx`, `language.pt-BR.resx`, and `language.es.resx` — Phase 10
- ✓ Expose new loan-state commands/queries through MCP `AssetTools` — Phase 10
- ✓ Update `.claude/docs/assets.md` with loan state timeline behavior — Phase 10
- ✓ Complete end-to-end verification of add, update, delete, and fallback flows — Phase 10
- ✓ **HTTP-01**: `IHttpClientFactory` is registered in DI and all `new HttpClient()` sites use named/typed clients — Phase 13
- ✓ **HTTP-02**: Price providers and update checkers share a consistent HTTP client lifetime and configuration — Phase 13
- ✓ **HTTP-03**: Existing provider tests continue to pass against the factory-based clients — Phase 13
- ✓ Add a `Sold` flag and `Date Sold` to assets — v0.5 (Phase 29)
- ✓ Hide sold assets from the main Assets view and exclude them from totals/calculations — v0.5 (Phase 29)
- ✓ Provide Undo Sell action to restore the asset to the active view — v0.5 (Phase 29)
- ✓ Prompt for Date Sold when not provided during the sell action — v0.5 (Phase 29)
- ✓ Add a History button on the Assets toolbar to access sold assets — v0.5 (Phase 30)
- ✓ Build a History screen listing sold assets with their sale date — v0.5 (Phase 30)
- ✓ Show per-type asset details summary in the History screen when an asset is selected — v0.5 (Phase 30)
- ✓ Undo Sell action restores the asset to the active view and refreshes the Assets tab — v0.5 (Phase 30)
- ✓ **MCP-01**: AI assistant can mark an asset as sold via MCP tool — v0.5 (Phase 31)
- ✓ **MCP-02**: AI assistant can undo a sale via MCP tool — v0.5 (Phase 31)
- ✓ **MCP-03**: AI assistant can list sold assets via MCP tool — v0.5 (Phase 31)
- ✓ **DOCS-01**: All new Asset Sold History user-facing strings are localized in `language.resx`, `language.pt-BR.resx`, and `language.es.resx` — v0.5 (Phase 31)
- ✓ **DOCS-02**: `.claude/docs/assets.md` updated with sold-history behavior and MCP impact — v0.5 (Phase 31)
- ✓ Complete end-to-end verification of mark sold, history browse, details panel, undo, and totals refresh — v0.5 (Phase 31)
- ✓ **FXE-01**: Fixed Expenses page documents record states (Paid, ManuallyPaid, Ignored, Empty) — v0.6 (Phase 36)
- ✓ **FXE-02**: Fixed Expenses page documents the yearly overview and out-of-range detection — v0.6 (Phase 36)
- ✓ **FXE-03**: Fixed Expenses page clarifies account-vs-currency binding exclusivity — v0.6 (Phase 36)
- ✓ **MCP-01**: MCP Server page documents the AssetTools category (28 tools) — v0.6 (Phase 37)
- ✓ **MCP-02**: MCP Server page documents loan-state tools with parameters — v0.6 (Phase 37)
- ✓ **MCP-03**: MCP Server page documents sold-asset tools with parameters — v0.6 (Phase 37)
- ✓ **NAV-01**: `mkdocs.yml` navigation updated to include new Settings page — v0.6 (Phase 38)
- ✓ **NAV-02**: New page titles consistent in Portuguese and English navigation translations — v0.6 (Phase 38)
- ✓ **NAV-03**: Settings & Configuration page created and linked — v0.6 (Phase 38)
- ✓ **QA-01**: Every Portuguese content change mirrored in English `.en.md` files — v0.6 (Phases 32-38)
- ✓ **QA-02**: Documentation site builds strictly with no errors or broken internal links — v0.6 (Phases 32-38)
- ✓ **QA-03**: Content review checklist applied to all updated pages — v0.6 (Phase 38)

### Active

- ✓ v0.7 Insights & Metrics Expansion — sats earned/month, sats spent (month + per category), stack velocity — Phase 40

### Active

- [ ] v0.7 Insights & Metrics Expansion — savings rate, burn rate, fixed vs variable ratio
- [ ] v0.7 Insights & Metrics Expansion — net worth CAGR, fiat vs BTC allocation %, best/worst months, days under water
- [ ] v0.7 Insights & Metrics Expansion — interest/fees paid (total + monthly), liquidation-price distance trend
- [ ] v0.8 BTC Loan Simulator — input loan parameters (collateral BTC, amount taken, liquidation LTV, start/end dates, interest rate, fees)
- [ ] v0.8 BTC Loan Simulator — simple or compound interest mode selection
- [ ] v0.8 BTC Loan Simulator — results panel with total to repay, interest/fees breakdown, fiat and sats values
- [ ] v0.8 BTC Loan Simulator — cost-over-time schedule until end date
- [ ] v0.8 BTC Loan Simulator — load existing BTC-backed loan from Assets to prefill fields

## Out of Scope

- v0.4 quality/hardening items (async void cleanup, HttpClient factory, background job throttling, LiteDB index centralization, god-VM refactor, live-API test isolation, handler unit tests) — still deferred to a future quality milestone; v0.6 did not address app code
- Recording sale price, capital gains, or tax lot information — out of scope for v0.5; sold history is for record-keeping only
- Real-time price streaming or automated sell detection — manual sale action only
- Mobile or web port — not in scope
- Changes to the Valt application code itself — v0.6 was documentation-only; no new app features

## Current State

- **v0.5 Asset Sold History shipped** on 2026-07-14.
- **v0.6 Documentation Site Refresh shipped** on 2026-07-17 — all public Valt docs updated through v0.5 features, factual errors corrected, missing pages added (Settings & Configuration), navigation updated, and a full content review checklist applied to 18 documentation files.
- **Deferred items:** 7 (see STATE.md Deferred Items) — v0.4 quality/hardening work and three debug sessions/quick tasks carried forward.
- **v0.6 milestone complete** — 7 of 7 phases, 17 of 17 plans, 42 tasks.
- **Phase 41 (WLT-04) complete** — asset-aware All Time High report with active net-worth assets and a "days under water" row in the existing ATH dashboard panel.

## Current Milestone: v0.8 BTC Loan Simulator

**Goal:** Add a BTC Loan Simulator tool — a what-if calculator for BTC-backed loans, mirroring the Leverage Simulator layout (inputs left, results right).

**Target features:**
- Loan inputs: collateral (BTC), amount taken, liquidation LTV, start date, interest rate, fees, end date
- Interest mode selection: simple or compound
- Results: total to repay (principal + interest + fees), interest/fees breakdown, in fiat and sats
- Cost-over-time schedule showing how the debt accrues until the end date
- Load an existing BTC-backed loan from Assets to prefill the simulator

## Next Milestone Goals

- Future candidate: address the v0.4 quality/hardening deferred items (async void cleanup, HttpClient factory centralization, background job throttling, LiteDB index centralization, god-VM refactor, live-API test isolation, handler unit tests).
- Deferred v0.7 candidates (v2): spending patterns heatmap, top-N expenses, category trend lines, hindsight cost, DCA consistency, rolling averages, debt-to-wealth ratio, goal forecasting, cost-basis/profit reports, account analytics.

## Context

Valt uses a layered architecture: Valt.Core (domain), Valt.App (CQRS), Valt.Infra (LiteDB persistence), and Valt.UI (Avalonia). The v0.4 milestone addressed quality and performance hardening. v0.5 introduced a user-facing Asset Sold History feature that lets users retain records of disposed assets without losing them entirely. The feature spans the domain (sold-state properties), application (commands and queries), infrastructure (persistence and price-update filtering), UI (History modal and Date Sold prompt), MCP tooling, localization, and documentation.

## Constraints

- **Tech stack**: .NET, Avalonia UI, LiteDB, CommunityToolkit.Mvvm — changes must fit existing patterns
- **Test framework**: NUnit with NSubstitute; builders and `DatabaseTest`/`IntegrationTest` bases must remain supported
- **Backward compatibility**: Existing user databases and price databases must continue to work without migration
- **Risk management**: UI restructuring must be done in vertical slices (VM + XAML + tests) to avoid broken bindings

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Latest recorded loan state snapshot wins for calculations | Matches user expectation that manually recorded current state overrides initial setup | Implemented in `BtcLoanDetails` calculations; `AssetQueries` selects `MaxBy(s => s.EffectiveDate)` — Phase 6-7 |
| Immutable initial loan setup values | Prevents accidental edits that would corrupt historical calculations | Initial setup fields remain unchanged; state snapshots are appended separately — Phase 6 |
| Auto-seed existing loans on load/migration | Avoids a one-shot migration script and keeps every loan queryable through the same timeline model | `AssetDetailsSerializer` seeds a snapshot from setup values when deserializing legacy loans — Phase 6 |
| Store history inside `BtcLoanDetails` JSON | Reuses existing `AssetDetailsSerializer` mechanism; avoids new collection complexity | Snapshots persisted as JSON array inside `BtcLoanDetailsDto` — Phase 6 |
| Two-step ViewModel simplification (services first, child-VMs second) | Builder extraction is low-risk and testable; child-VM restructuring requires XAML changes and carries binding risk | Phases 17-20 extract services; Phases 21-24 restructure VMs and XAML |
| Skip research for v0.4 | No new features or external integrations; scope is refactor and hardening of existing code | — |
| Add `IsSold` and `DateSold` as first-class properties on the `Asset` aggregate and `AssetEntity` | First-class properties keep sold-state behavior explicit and queryable; avoids hidden JSON blob semantics | Domain, persistence, and tests all use `IsSold`, `DateSold`, `PreviousVisibility` — v0.5 (Phase 29) |
| Filter sold assets at the query layer (`IAssetQueries`) | Centralizes semantics so the UI, reports, MCP, and background jobs all share the same active/sold definition | `GetAllAsync` returns active assets only; `GetSoldAsync` returns sold assets — v0.5 (Phase 29) |
| Reuse the existing `AssetViewModel` and `AssetDTO` mapping for the History details panel | Avoids duplicating per-type layout logic and keeps the History card consistent with the main asset card | `SoldAssetDetailsCard` binds to `AssetViewModel` — v0.5 (Phase 30) |
| Keep `IsSold` independent from `Visible` | Ensures undo restores the prior visibility state rather than conflating sold-state with visibility | `MarkAsSold` captures `PreviousVisibility`; `UndoSale` restores `Visible` — v0.5 (Phase 29) |
| Keep `Asset.New` backward-compatible with optional sold-state defaults | Existing call sites continue to compile without modification | `isSold: false`, `dateSold: null`, `previousVisibility: true` defaults — v0.5 (Phase 29) |
| Reuse `AssetUpdatedEvent` for sold-state changes | Avoids expanding the event surface in the foundational plan | `MarkAsSold` and `UndoSale` emit `AssetUpdatedEvent` — v0.5 (Phase 29) |
| Store `DateSold` as `DateOnly?` directly on `AssetEntity` | Matches the domain model; round-trip test proves LiteDB serialization | Date-only serialization verified — v0.5 (Phase 29) |
| Inject `IClock` into `MarkAssetAsSoldValidator` and `MarkAssetAsSoldHandler` | Keeps future-date rejection and default-to-today behavior testable and deterministic | Unit tests use `FakeClock` — v0.5 (Phase 29) |
| Place the `AssetPriceUpdaterJob` sold-asset skip guard in `ShouldUpdatePrice` | Keeps the `IAssetRepository` contract stable and the data-fetch behavior consistent with other consumers | `ShouldUpdatePrice` returns `false` when `asset.IsSold` — v0.5 (Phase 29) |
| Return structured dialog result from `SoldAssetHistory` modal | Caller-side refresh is explicit and avoids coupling `AssetsViewModel` to internal modal messages | `WasRestored` flag drives active-list refresh — v0.5 (Phase 30) |
| Use `SoldAssetHistory_DateSold_Title` with a literal colon via `Run` elements | Avoids an unplanned extra resource key and matches the existing Title+colon pattern | `_Label` key removed; 20-key set preserved — v0.5 (Phase 31) |
| Document public docs terminology from app language files to avoid UI/docs drift | Keeps English/Portuguese public docs aligned with the actual Valt UI strings | `.claude/docs/assets.md` and ROADMAP.md updated to match `language.resx`/`language.pt-BR.resx` — v0.6 (Phase 32) |
| Code wins over stale planning docs when conflicts arise | Ensures planning artifacts remain authoritative and accurate for future phases | Updated REQUIREMENTS.md, ROADMAP.md, and `.claude/docs/assets.md` to match code reality — v0.6 (Phase 32) |
| Use exact app-verbatim labels in documentation pages | Prevents drift between the UI and public docs; uses the app's language files as the source of truth | Fixed Expenses and Settings pages use labels directly from `language.resx`/`language.pt-BR.resx` — v0.6 (Phases 36, 38) |
| Replace drifted MCP tool tables as whole units from code-verified research | Full replacement removes stale descriptions and adds 32 missing real tools, landing documented count at 90 = code truth | 7 pre-existing category tables replaced in both languages; 28 phantom names eliminated — v0.6 (Phase 38) |
| Apply a 12-check content review matrix to all v0.6 documentation files | Satisfies QA-03 and creates an auditable artifact for future docs work | `38-QA-CHECKLIST.md` created with 18 files × 12 checks — v0.6 (Phase 38) |
| Reused `IsBtcMetricsEmpty` for both monthly and category breakdown views | Avoids a second empty-state property and keeps the existing XAML border binding unchanged | `ReportsViewModel` toggles `IsBtcMetricsEmpty` between `Months.Count` and `SpentByCategory.Count` — Phase 40 |
| Cache `_lastBtcMetricsData` for category/monthly toggle empty-state recompute | Switches views instantly without a database round-trip | Toggle handler recomputes empty state from cached DTO — Phase 40 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-08-13 after milestone v0.8 start*
