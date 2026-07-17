---
mode: quick
description: Fix TransactionEditorView OK button caption to use 'OK' for new entries and 'Save' for editing entries in all languages
---

# Quick Task 260710-gdb: Fix TransactionEditorView OK Button Caption

## Goal

Make the OK button label on `TransactionEditorView` context-aware:
- Show **OK** when inserting a new entry (any transaction type).
- Show **Save** when editing an existing entry (any transaction type).
- Apply the change to all supported languages (en, pt-BR, es).

## Tasks

1. **Simplify localization resources**
   - Rename `TransactionEditor_SaveTransaction` to `TransactionEditor_Save` and change its value to `Save` / `Salvar` / `Guardar`.
   - Rename `TransactionEditor_CreateTransaction` to `TransactionEditor_Ok` and change its value to `OK` in all languages.
   - Rename `TransactionEditor_CreateTransfer` to `TransactionEditor_Ok`.
   - Rename `TransactionEditor_SaveTransfer` to `TransactionEditor_Save`.
   - Update `language.Designer.cs` accordingly.

2. **Update ViewModel logic**
   - In `TransactionEditorViewModel.cs`, change `OkButtonLabel` to return `language.TransactionEditor_Ok` when `_transactionId` is null and `language.TransactionEditor_Save` otherwise.

3. **Update tests**
   - Update `TransactionEditorViewModelTests.cs` assertions to expect the new `OK` and `Save` labels for add and edit modes, including transfer mode.

4. **Verify**
   - Run `dotnet build Valt.sln`.
   - Run the relevant test class.
