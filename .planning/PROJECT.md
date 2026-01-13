# CSV Import Wizard

## What This Is

A wizard-style feature for Valt that allows users to import transactions from CSV files, enabling migration from other budget management tools. The wizard guides users through file selection, account mapping, category preview, and import execution.

## Core Value

Enable users to migrate their financial history from other tools into Valt without manual data entry.

## Requirements

### Validated

- ✓ Valt handles fiat and bitcoin accounts with transactions — existing
- ✓ Six transaction detail types cover all transfer scenarios — existing
- ✓ Categories support hierarchical organization — existing
- ✓ Modal system with wizard-style flows — existing
- ✓ CsvHelper library available for CSV parsing — existing

### Active

- [ ] Parse CSV files with strict column format (date, description, amount, account, to_account, to_amount, category)
- [ ] Generate sample template CSV demonstrating all transaction types
- [ ] 5-step wizard: File Selection → Account Mapping → Category Preview → Summary → Import Progress
- [ ] Infer transaction type from account types and to_account presence
- [ ] Block import if account names conflict with existing accounts
- [ ] Stop background jobs during import, restart after completion
- [ ] Localization for en-US and pt-BR

### Out of Scope

- CSV export from Valt — import only for v1
- Auto-detection of CSV column formats — strict format only, user must match template
- Merging with existing accounts — new account names required to avoid data conflicts
- Duplicate detection for transactions — user responsibility to avoid re-importing

## Context

Valt is a mature .NET 10 / Avalonia desktop app for personal bitcoin budget management. The existing architecture uses:
- Clean Architecture (Core → Infra → UI layers)
- MVVM with CommunityToolkit.Mvvm
- LiteDB for persistence
- Modal factory pattern for dialogs
- Background job system for async operations

The import feature integrates with existing patterns:
- New modal registered in `ApplicationModalNames` and factory
- Services in `Valt.Infra/Services/CsvImport/`
- Menu item in MainView dropdown

## Constraints

- **Architecture**: Must follow existing layered architecture (services in Infra, UI in Views/Main/Modals)
- **UI Framework**: Avalonia AXAML with compiled bindings
- **Localization**: All user-facing strings in .resx files (en-US primary, pt-BR secondary)
- **Testing**: Unit tests for parser and executor services

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Strict CSV format | Simplifies parsing, template provides clear guidance | — Pending |
| Block on account name conflict | Prevents accidental data merge/corruption | — Pending |
| Stop background jobs during import | Prevents race conditions with transaction processing | — Pending |
| Wizard-style modal | Guides users through complex multi-step process | — Pending |

---
*Last updated: 2026-01-13 after initialization*
