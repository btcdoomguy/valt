# Valt

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET 9 and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, external investments (assets), and displays values in bitcoin terms.

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

### Active

- [ ] Allow users to record BTC loan state changes over time (fee %, current debt, collateral, amount taken, effective date)
- [ ] Use the latest recorded loan state as the basis for current-value calculations, falling back to previous entries when the latest is deleted
- [ ] Provide an "Update Loan State" screen prefilled with current calculated totals, accessible from the Assets tab context menu
- [ ] Provide a loan state history screen listing all recorded changes with delete and add-new-state actions
- [ ] Auto-seed existing loans with an initial state entry derived from their current setup values

### Out of Scope

- Interest compounding or daily accrual between recorded states — only the latest snapshot is used for calculations
- Editing initial loan setup values from the new state screens — initial values remain immutable
- Supporting non-BTC loans in the state timeline — scope is BTC-backed loans only
- Exporting loan state history — can be added later if needed

## Context

Valt uses a layered architecture: Valt.Core (domain), Valt.App (CQRS), Valt.Infra (LiteDB persistence), and Valt.UI (Avalonia). BTC loans are modeled as `Asset` aggregates with `BtcLoanDetails`. Current value is calculated from fields such as `LoanAmount`, `CollateralSats`, `Apr`, `Fees`, `LoanStartDate`, and an optional `FixedTotalDebt`.

The previous milestone (Phase 4-5) added a custom BTC price simulation bar to the Reports tab. That work exposed how loan dashboard calculations flow through `GetAssetSummaryQuery` and `GetBtcLoansDashboardQuery`.

Users now need a timeline of loan state snapshots because real-world loans change constantly: additional capital is drawn, fees are renegotiated, and collateral is adjusted. A single set of initial values cannot reflect the true current debt or collateral position.

## Constraints

- **Tech stack**: .NET 9, Avalonia UI, LiteDB, CommunityToolkit.Mvvm — changes must fit existing patterns
- **Persistence**: Loan state history must be stored in the existing LiteDB database without breaking existing loans
- **UI conventions**: New screens must follow the modal registration pattern (`ApplicationModalNames`, modal factory, `ValtModalViewModel`, `CustomTitleBar`, `SystemDecorations=None`)
- **MCP impact**: Any new commands/queries should be exposed through MCP `AssetTools` and services forwarded in `McpServerService`
- **Localization**: New strings must be added to all three language files (`en`, `pt-BR`, `es`)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Latest snapshot wins for calculations | Matches user expectation that manually recorded current state overrides initial setup | — Pending |
| Immutable initial loan setup values | Prevents accidental edits that would corrupt historical calculations | — Pending |
| Auto-seed existing loans on load/migration | Avoids a one-shot migration script and keeps every loan queryable through the same timeline model | — Pending |
| Store history inside `BtcLoanDetails` JSON | Reuses existing `AssetDetailsSerializer` mechanism; avoids new collection complexity | — Pending |

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

## Current Milestone: v0.3 Loan State Timeline

**Goal:** Let users record the evolving state of BTC loans over time and use the latest recorded snapshot as the single source of truth for current-value calculations.

**Target features:**
- Domain model for loan state timeline entries
- Commands and queries to add, delete, and query state snapshots
- "Update Loan State" modal prefilled with current calculated totals
- "Loan State History" modal listing all recorded changes
- Auto-seeding of existing loans with an initial state snapshot
- MCP tool exposure and localization updates

---
*Last updated: 2026-06-15 after milestone v0.3 started*
