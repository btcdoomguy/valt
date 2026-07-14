---
phase: quick-260714-i1p
plan: 01
type: execute
status: complete
subsystem: Valt.UI
requires: []
provides:
  - day-accurate-asset-sold-date
affects:
  - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptViewModel.cs
  - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptView.axaml
  - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptView.axaml.cs
tech-stack:
  added: []
  patterns:
    - CalendarDatePicker with PreviousDay/NextDay/SelectToday pattern
    - RelayCommand
key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptViewModel.cs
    - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptView.axaml
    - src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptView.axaml.cs
decisions: []
metrics:
  duration: 12 min
  completed_date: 2026-07-14
  tasks: 3
  files: 3
---

# Phase quick-260714-i1p Plan 01: DateSold CalendarDatePicker Summary

One-liner: Replaced the month/year-only DateCalendarSelector in the Mark as Sold prompt with a day-accurate CalendarDatePicker, matching the transaction editor pattern with PreviousDay/NextDay buttons, SelectToday context menu, and keyboard shortcuts.

## What Was Done

1. **Updated `DateSoldPromptViewModel.cs`** — `DateSold` is now `DateTime?` defaulting to today; added `PreviousDay`, `NextDay`, `SelectToday` RelayCommands; `Ok()` passes the nullable date through to the `Response` record.
2. **Updated `DateSoldPromptView.axaml`** — Replaced `DateCalendarSelector` with a horizontal `StackPanel` containing a MaterialDesign left button, `CalendarDatePicker` (Width="140", SelectedDateFormat="Short", IsTodayHighlighted="True"), and a right button, plus a `SelectToday` context menu item.
3. **Updated `DateSoldPromptView.axaml.cs`** — Added a bubbling `KeyDown` handler for `Ctrl+Left` (PreviousDay), `Ctrl+Right` (NextDay), `Ctrl+R` (SelectToday), and `Enter` (Ok).

## Verification

- `dotnet build Valt.sln` succeeds with no new errors or warnings.
- All three changed files compile and the Mark as Sold flow continues to pass the selected `DateSold` to `MarkAssetAsSoldCommand`.

## Deviations from Plan

None — plan executed exactly as written.

## Auth Gates

None.

## Known Stubs

None.

## Threat Flags

None.

## Self-Check: PASSED

- [x] Modified files exist
- [x] Commits exist: `f39e848`, `c4d4b2e`, `d79acfa`
- [x] `dotnet build Valt.sln` passes
