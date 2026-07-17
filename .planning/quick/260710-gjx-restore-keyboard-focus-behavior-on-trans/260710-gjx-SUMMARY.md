---
status: complete
quick_id: 260710-gjx
---

# Quick Task Summary: 260710-gjx

## Description

Restore keyboard focus behavior on TransactionEditorView after recent changes.

## Root Cause

After the Phase 21 refactor into child views, `TransactionEditorView.OnOpened` tried to focus a child control synchronously before the `ContentControl` template had expanded and the child view was fully in the visual tree. The old implementation deferred focus with `Dispatcher.UIThread.InvokeAsync(..., DispatcherPriority.ApplicationIdle)`.

## Changes

- In `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml.cs`:
  - Added `using Avalonia.Threading;`.
  - Wrapped the initial focus logic inside `Dispatcher.UIThread.Post(..., DispatcherPriority.ApplicationIdle)` so the child view is materialized before focus is applied.

## Verification

- `dotnet build Valt.sln` succeeded.
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.UI.Screens.TransactionEditorViewModelTests"` passed (14 tests).

## Commit

60352cf
