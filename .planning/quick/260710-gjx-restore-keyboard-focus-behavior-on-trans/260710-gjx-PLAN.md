---
mode: quick
description: Restore keyboard focus behavior on TransactionEditorView after recent changes
---

# Quick Task 260710-gjx: Restore Keyboard Focus on TransactionEditorView

## Goal

Restore the previous keyboard focus behavior on `TransactionEditorView` so that the correct input receives focus when the modal opens.

## Background

The old implementation deferred focus with `Dispatcher.UIThread.InvokeAsync(..., DispatcherPriority.ApplicationIdle)` inside `OnOpened`. After the Phase 21 refactor into child views, focus is applied synchronously in `OnOpened`, which runs before the child content is fully materialized in the visual tree. As a result the focus is lost.

## Tasks

1. **Defer initial focus**
   - In `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml.cs`, wrap the focus logic in `OnOpened` so it runs on the UI thread at `DispatcherPriority.ApplicationIdle`.

2. **Verify build and tests**
   - Run `dotnet build Valt.sln`.
   - Run the TransactionEditor view model tests.
