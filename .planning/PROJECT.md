# Valt

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, external investments (assets), and displays values in bitcoin terms.

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
- ✓ Add a `Sold` flag and `Date Sold` to assets — Phase 29
- ✓ Hide sold assets from the main Assets view and exclude them from totals/calculations — Phase 29
- ✓ Provide Undo Sell action to restore the asset to the active view — Phase 29
- ✓ Prompt for Date Sold when not provided during the sell action — Phase 29

### Active

- [ ] Add a History button on the Assets toolbar to access sold assets
- [ ] Build a History screen listing sold assets with their sale date
- [ ] Show per-type asset details summary in the History screen when an asset is selected
- [ ] Update Assets documentation and MCP tool exposure for sold assets


### Out of Scope

- v0.4 quality/hardening items (async void cleanup, HttpClient factory, background job throttling, LiteDB index centralization, god-VM refactor, live-API test isolation, handler unit tests) — deferred to a future quality milestone; v0.5 is focused on user-facing Asset Sold History
- Replacing LiteDB or Avalonia — would require its own roadmap
- Full MCP server redesign — security review only; redesign deferred to a later milestone
- Recording sale price, capital gains, or tax lot information — out of scope for v0.5; sold history is for record-keeping only
- Real-time price streaming or automated sell detection — manual sale action only
- Mobile or web port — not in scope

## Context

Valt uses a layered architecture: Valt.Core (domain), Valt.App (CQRS), Valt.Infra (LiteDB persistence), and Valt.UI (Avalonia). The v0.4 milestone addressed quality and performance hardening. v0.5 introduces a user-facing Asset Sold History feature that lets users retain records of disposed assets without losing them entirely.

Key focus areas for this milestone: adding a `Sold` flag and `Date Sold` to the asset domain, filtering sold assets out of active queries and totals, building a History screen with per-type details and an Undo Sell action, and updating documentation and MCP tooling accordingly.

## Constraints

- **Tech stack**: .NET, Avalonia UI, LiteDB, CommunityToolkit.Mvvm — changes must fit existing patterns
- **Test framework**: NUnit with NSubstitute; builders and `DatabaseTest`/`IntegrationTest` bases must remain supported
- **Backward compatibility**: Existing user databases and price databases must continue to work without migration
- **Scope discipline**: This is a hardening/quality milestone; new features are explicitly out of scope
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

## Current Milestone: v0.5 Asset Sold History

**Goal:** Let users mark assets as sold, record the sale date, hide them from the active Assets view and calculations, browse sold assets in a dedicated History screen, inspect per-type details, and undo a sale to restore the asset.

**Target features:**
- Mark assets as sold with a recorded Date Sold
- Hide sold assets from the main Assets tab and exclude them from totals/calculations
- Add a History button on the Assets toolbar for accessing sold assets
- History screen lists sold assets with their sale date
- Selecting a sold asset shows a full details summary panel tailored to the asset type
- Undo Sell action restores the asset to the main view
- Optional Date Sold prompt when not provided at sale time

---
*Last updated: 2026-07-13 after Phase 29 completion — domain sold-state foundation, active-view filtering, and gap fixes delivered*
