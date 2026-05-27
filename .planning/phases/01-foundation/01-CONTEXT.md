# Phase 01 Context: Foundation

**Phase:** 01 — Foundation
**Goal:** Wire up menu integration and modal shell
**Date:** 2026-05-27

## Domain

Set up menu entries, context menu handler, and modal window shell for the Spending Evolution module. This phase creates the structural foundation that Phase 2 (Core Implementation) will populate with actual functionality.

## Decisions

### Modal Structure
- **Window size:** 900x600 (starting size), resizable
  - Rationale: Complex analytical modal needs space for category selector (left panel ~250px), chart (right ~650px), and indicators (bottom row). Resizable to accommodate different screen sizes.
  - Pattern: Follow existing modal pattern with `SystemDecorations="None"` and `CustomTitleBar`

### Menu Integration
- **Location:** Under existing Tools submenu in main menu
  - Rationale: Grouped with other analytical tools (ConversionCalculator, LeverageSimulator, PriceHistory)
  - Pattern: Add `[RelayCommand]` method in `MainViewModel`, bind in `MainView.axaml` MenuFlyout

### Context Menu
- **Scope:** Available on any transaction type (not just debits)
  - Rationale: User wants to analyze spending evolution for any category, regardless of transaction type
  - Implementation: Add menu item to existing transaction context menu in `TransactionListView.axaml`
  - Pre-selection: When opened via context menu, pre-select the category of the clicked transaction

### Modal Parameter Passing
- **Parameter type:** `string?` — category ID to pre-select, or null for all categories
  - Rationale: Simple parameter to distinguish between "menu open" (null = all categories) and "context menu open" (category ID = specific category)
  - Pattern: Follow existing `ValtModalViewModel.Parameter` usage pattern

## Code Context

### Reusable Assets
- **Modal base class:** `ValtModalViewModel` — handles `OwnerWindow`, `Parameter`, `OnBindParameterAsync`
- **Modal factory:** `IModalFactory.CreateAsync(ApplicationModalNames, Window, object?)`
- **Modal registration:** Add enum value to `ApplicationModalNames`, add factory case in `Extensions.cs`
- **Custom title bar:** `CustomTitleBar` user control with `TitleBarPressed` and `CloseClick` handlers
- **Existing analytical modals:** `PriceHistoryView`, `ConversionCalculatorView`, `LeverageSimulatorView` — follow similar sizing and structure

### File Locations (New Files)
```
src/Valt.UI/Views/Main/Modals/SpendingEvolution/
├── SpendingEvolutionView.axaml
├── SpendingEvolutionView.axaml.cs
└── SpendingEvolutionViewModel.cs
```

### Files to Modify
- `src/Valt.UI/Views/ApplicationModalNames.cs` — add enum value
- `src/Valt.UI/Extensions.cs` — add factory mapping
- `src/Valt.UI/Views/Main/MainView.axaml` — add menu item
- `src/Valt.UI/Views/Main/MainViewModel.cs` — add command
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListView.axaml` — add context menu item

## Canonical Refs

- `.planning/PROJECT.md` — Project context and goals
- `.planning/REQUIREMENTS.md` — Requirements INT-04, INT-05, UI-01, UI-02
- `.planning/ROADMAP.md` — Phase 1 goal and success criteria
- `src/Valt.UI/Views/Main/Modals/Settings/SettingsView.axaml` — Modal pattern example
- `src/Valt.UI/Views/Main/MainViewModel.cs` — Menu command pattern
- `src/Valt.UI/Views/Main/MainView.axaml` — Menu XAML pattern
- `src/Valt.UI/Services/IModalFactory.cs` — Modal factory interface
- `src/Valt.UI/Extensions.cs` — Modal registration pattern

## Deferred Ideas

None (all scope items fit within Phase 1)

## Notes

- Modal will be empty in this phase — just the shell with title bar and basic layout structure
- No data binding or actual UI controls beyond the shell in this phase
- Focus on getting the menu → modal → close flow working end-to-end
- Localization strings will be added in Phase 3 (Polish)
