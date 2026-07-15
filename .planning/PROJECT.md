# Valt

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, external investments (assets), and displays values in bitcoin terms. As of v0.5, users can also mark assets as sold, keep a record in a dedicated Asset Sold History screen, and undo a sale to restore the asset to the active view.

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

### Active

- [ ] v0.6 milestone goals — Documentation Site Refresh: update public Valt docs to reflect v0.5 features, fix outdated content, and add missing pages.

## Out of Scope

- v0.4 quality/hardening items (async void cleanup, HttpClient factory, background job throttling, LiteDB index centralization, god-VM refactor, live-API test isolation, handler unit tests) — deferred to a future quality milestone; v0.6 is focused on documentation
- Recording sale price, capital gains, or tax lot information — out of scope for v0.5; sold history is for record-keeping only
- Real-time price streaming or automated sell detection — manual sale action only
- Mobile or web port — not in scope
- Changes to the Valt application code itself — documentation site only; no new app features

## Current Milestone: v0.6 Documentation Site Refresh

**Goal:** Bring the public Valt documentation site (`valt-docs`) up to date with all features shipped through v0.5, fix factual errors, and add missing pages.

**Target features:**
- Rewrite the **Assets** page to include Asset Sold History (Mark as Sold, Date Sold, History screen, Undo Sale) and BTC-backed Loans (collateral, APR, LTV, loan-state timeline).
- Update the **Reports** page with custom BTC price simulation and current dashboard/wealth/loan summaries.
- Complete the **Goals** page with missing goal types (`SaveFiat`, `SavingsRate`, `NetWorthBtc`) and price-data behavior.
- Update the **MCP Server** page with the full tool list including AssetTools, loan-state tools, and sold-asset tools.
- Fix factual errors in Installation (SQLite → LiteDB), FAQ, and Getting Started.
- Enhance the **Fixed Expenses** page with record states, yearly overview, and range handling.
- Consider new pages: Settings & Configuration, Asset Groups.

## Current State

- **v0.5 Asset Sold History shipped** on 2026-07-14.
- **Phase 32 complete** on 2026-07-15 — Factual errors corrected on Installation, FAQ, Getting Started, Assets, and Reports public docs; internal requirements and roadmap aligned; MkDocs site builds cleanly.
- **Phase 33 ready** — Assets Page Rewrite: document Asset Sold History, BTC-backed loans, and Asset Groups.
- **Deferred items:** 6 (see STATE.md Deferred Items).
- **v0.6 milestone in progress** — 1 of 7 phases complete.

## Next Milestone Goals

- **v0.7 — TBD** (to be defined after v0.6 documentation milestone completes).

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
*Last updated: 2026-07-15 after completing Phase 32 — Factual Fixes and Cross-Page Accuracy*
