---
status: complete
quick_id: 260710-gdb
---

# Quick Task Summary: 260710-gdb

## Description

Fix TransactionEditorView OK button caption to use 'OK' for new entries and 'Save' for editing entries in all languages.

## Changes

- Consolidated transaction editor button labels into two localization keys:
  - `TransactionEditor_Ok` = "OK" (en), "OK" (pt-BR), "OK" (es)
  - `TransactionEditor_Save` = "Save" (en), "Salvar" (pt-BR), "Guardar" (es)
- Removed the previous separate keys: `TransactionEditor_SaveTransaction`, `TransactionEditor_CreateTransaction`, `TransactionEditor_CreateTransfer`, `TransactionEditor_SaveTransfer`.
- Updated `TransactionEditorViewModel.OkButtonLabel` to return `OK` when `_transactionId` is null and `Save` otherwise, for all transaction types.
- Removed the now-unnecessary `[NotifyPropertyChangedFor(nameof(OkButtonLabel))]` on `ActiveChildViewModel`.
- Updated and expanded `TransactionEditorViewModelTests` to cover add/edit modes for both debt and transfer.

## Verification

- `dotnet build Valt.sln` succeeded.
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.UI.Screens.TransactionEditorViewModelTests"` passed (14 tests).

## Commit

c3f9296
