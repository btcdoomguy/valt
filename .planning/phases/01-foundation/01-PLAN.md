# Plan: Phase 01 — Foundation

**Phase:** 01 — Foundation
**Goal:** Wire up menu integration and modal shell
**Date:** 2026-05-27
**Mode:** YOLO (auto-approve)

## Requirements Covered

- **INT-04:** Module registers in main menu following existing menu patterns
- **INT-05:** Module registers context menu handler for debit transactions
- **UI-01:** User can open Spending Evolution modal from Main Menu > Tools > Evolução de gastos
- **UI-02:** User can open Spending Evolution modal by right-clicking on a debit transaction and selecting "Analisar evolução"

## Success Criteria

1. "Evolução de gastos" appears in Main Menu > Tools
2. Right-clicking a transaction shows "Analisar evolução" option
3. Clicking either opens an empty modal window following existing modal patterns
4. Modal has correct MinWidth/MinHeight (900x600) and custom title bar
5. Modal opens and closes correctly
6. Modal accepts parameter (category ID) when opened from context menu

## Task Breakdown

### Task 1: Create modal files

**Files to create:**
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml`
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml.cs`
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs`

**Details:**
- Follow existing modal pattern (SettingsView, PriceHistoryView)
- Window properties: 900x600, resizable, SystemDecorations=None, CustomTitleBar
- ViewModel inherits from `ValtModalViewModel`
- Implement `OnBindParameterAsync()` to receive category ID parameter
- Empty content area (placeholder Border) — Phase 2 will add actual controls
- Title: "Spending Evolution" (hardcoded for now, localization in Phase 3)

**Depends on:** None

### Task 2: Register modal in application

**Files to modify:**
- `src/Valt.UI/Views/ApplicationModalNames.cs` — add `SpendingEvolution` enum value
- `src/Valt.UI/Extensions.cs` — add factory mapping in DI registration

**Details:**
- Add `SpendingEvolution` to `ApplicationModalNames` enum
- Add case in `Extensions.cs` factory function: `ApplicationModalNames.SpendingEvolution => new SpendingEvolutionView()`

**Depends on:** Task 1

### Task 3: Add main menu integration

**Files to modify:**
- `src/Valt.UI/Views/Main/MainView.axaml` — add menu item under Tools submenu
- `src/Valt.UI/Views/Main/MainViewModel.cs` — add `[RelayCommand]` method

**Details:**
- Add MenuItem in XAML under existing Tools MenuItem (after PriceHistory)
- Bind to `OpenSpendingEvolutionCommand`
- Command opens modal with null parameter (all categories)

**Depends on:** Task 2

### Task 4: Add context menu integration

**Files to modify:**
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListView.axaml` — add context menu item
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs` — add command handler

**Details:**
- Add MenuItem in existing DataGrid.ContextMenu
- Bind to `AnalyzeSpendingEvolutionCommand`
- Pass transaction's category ID as parameter
- Command opens modal with category ID pre-selected

**Depends on:** Task 2

### Task 5: Verify end-to-end flow

**Verification steps:**
1. Build solution: `dotnet build Valt.sln`
2. Run application: `dotnet run --project src/Valt.UI/Valt.UI.csproj`
3. Test menu path: Main Menu > Tools > Evolução de gastos → modal opens
4. Test context menu: Right-click transaction → "Analisar evolução" → modal opens
5. Verify modal dimensions: 900x600, resizable, custom title bar present
6. Verify modal closes correctly (click X or close button)

**Depends on:** Tasks 1-4

## Dependencies

```
Task 1 (Create modal files)
  └── Task 2 (Register modal)
        ├── Task 3 (Menu integration)
        └── Task 4 (Context menu)
              └── Task 5 (Verify)
```

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Modal doesn't follow existing pattern | Copy exact structure from SettingsView or PriceHistoryView |
| Menu item doesn't appear | Verify XAML namespace and binding path |
| Context menu command not firing | Check DataContext binding and command parameter |
| Build errors from missing references | Ensure all using statements match project structure |

## Out of Scope

- No actual UI controls in modal (Phase 2)
- No data binding or calculations (Phase 2)
- No localization strings (Phase 3)
- No chart or category selector (Phase 2)

## Files Summary

**New files (3):**
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml`
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml.cs`
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs`

**Modified files (5):**
- `src/Valt.UI/Views/ApplicationModalNames.cs`
- `src/Valt.UI/Extensions.cs`
- `src/Valt.UI/Views/Main/MainView.axaml`
- `src/Valt.UI/Views/Main/MainViewModel.cs`
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListView.axaml`
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs`

---
*Plan created: 2026-05-27*
