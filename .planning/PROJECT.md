# Valt

## Current Milestone: v1.1 Bitcoin Price Simulation

**Goal:** Allow users to override the current Bitcoin price in the Reports tab to simulate portfolio values at different BTC prices, helping them understand how price changes affect their wealth.

**Target features:**
- Fixed price bar in Resumo panel with BTC price display and "Simular" button
- Modal for inputting custom BTC price (non-negative, in main fiat currency)
- Dashboard calculations refactored to accept custom price without affecting other app areas
- Visual state changes: "Simular" → "Alterar Preço", add "Resetar" button when custom price is active
- Custom price resets on app close (no persistence across sessions)

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET 10 and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, and displays values in bitcoin terms. Users can manage accounts, categorize transactions, track cost basis, set financial goals, manage fixed expenses, and monitor external investments — all with real-time price data from multiple providers.

## Core Value

Users can understand their financial life denominated in bitcoin, making it clear how fiat inflation affects their purchasing power while maintaining a complete view of their wealth.

## Requirements

### Validated

- ✓ Budget module — Manage fiat and bitcoin accounts, transactions, and categories
- ✓ Reports module — Financial analysis, charts, and dashboards
- ✓ AvgPrice module — Cost basis tracking (BrazilianRule, FIFO)
- ✓ Goals module — Financial goal tracking with auto-progress
- ✓ Fixed Expenses module — Recurring expense management
- ✓ Assets module — External investments (stocks, ETFs, crypto, real estate, leveraged positions)
- ✓ MCP Server — AI assistant integration via embedded Kestrel server
- ✓ Price crawlers — Real-time BTC and fiat rate fetching from multiple providers
- ✓ Background jobs — Periodic tasks for prices, auto-sat calculations, account totals, goal progress
- ✓ CQRS architecture — Clean separation of commands and queries
- ✓ Multi-language support — English, Portuguese, Spanish
- ✓ Multi-platform builds — Windows, Linux, macOS via GitHub Actions
- ✓ Spending Evolution module — Track spending trends in both fiat and bitcoin (sats) over time (v1.0)
- ✓ Category selector with multi-select — Left panel with category tree selection (v1.0)
- ✓ Dual-axis line chart — Fiat total (left Y) and sats total (right Y) over time (v1.0)
- ✓ Time range selector — Dropdown with 12/24/36/48/60 month options (v1.0)
- ✓ Cost of living indicators — Percentage increase in fiat and BTC terms (v1.0)
- ✓ Right-click integration — "Analyze evolution" from debit transaction context menu (v1.0)
- ✓ Modal window — New modal following existing modal patterns (v1.0)

### Active

- [ ] BTC Price Simulation — Override current Bitcoin price in Reports tab to simulate portfolio values
- [ ] Fixed price bar — BTC price display with "Simular" button in Resumo panel
- [ ] Price input modal — Non-negative custom BTC price input in main fiat currency
- [ ] Service refactoring — Allow custom price injection without affecting other app areas
- [ ] Visual state changes — "Alterar Preço" / "Resetar" buttons when custom price is active

### Out of Scope

- Web/cloud version — Desktop-only by design, no plans for web deployment
- Mobile app — Desktop-only by design
- Bank/Plaid integration — Manual transaction entry only
- Multi-user collaboration — Single-user desktop application
- Cloud sync — Local-only data storage

## Context

**Brownfield project** with substantial existing codebase. Layered architecture with strict dependency rules enforced by architecture tests.

**Existing modules** cover most personal finance needs. Users have been requesting better visibility into spending trends over time, especially seeing how fiat inflation affects costs when denominated in bitcoin.

**Technical environment:**
- .NET 10 (preview/RC) with LangVersion 14
- Avalonia UI 11.3 with Fluent theme
- LiteDB embedded NoSQL with password protection
- LiveChartsCore.SkiaSharpView for charts
- CommunityToolkit.Mvvm for MVVM
- MCP (Model Context Protocol) server for AI integration

## Constraints

- **Tech stack**: .NET 10 / Avalonia UI / LiteDB — must follow existing patterns
- **Desktop only**: No web or mobile components
- **Local data**: All data stored locally, no cloud dependencies
- **Bitcoin-denominated**: All values must be viewable in bitcoin terms
- **Architecture compliance**: New features must pass NetArchTest.Rules architecture tests

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Layered architecture with CQRS | Clean separation, testability, maintainability | ✓ Good — scales well with new modules |
| LiteDB embedded database | No external database dependency, user-friendly | ✓ Good — single file, password protected |
| MCP server embedded in desktop app | AI assistant integration without external services | ✓ Good — local-first AI tools |
| Dual currency display (fiat + BTC) | Core value proposition for bitcoiners | ✓ Good — differentiating feature |

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
*Last updated: 2026-06-01 after v1.1 milestone start*
