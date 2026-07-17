---
status: complete
quick_id: 260710-g7d
---

# Quick Task Summary: 260710-g7d

## Description

Fix TransactionEditorView OK button caption to show 'Add Transaction' for new transactions and 'Save Transaction' for editing existing transactions.

## Changes

- Added `TransactionEditor_CreateTransaction` localization key to `language.resx`, `language.pt-BR.resx`, and `language.es.resx`.
- Added the generated `TransactionEditor_CreateTransaction` property to `language.Designer.cs`.
- Updated `TransactionEditorViewModel.OkButtonLabel` to return the new create label for non-transfer transactions when `_transactionId` is null.
- Renamed existing test to assert the add label for new debt transactions.
- Added a new test asserting the save label for debt transactions when editing.

## Verification

- `dotnet build Valt.sln` succeeded.
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.UI.Screens.TransactionEditorViewModelTests"` passed (13 tests).

## Commit

3a8e2a5
