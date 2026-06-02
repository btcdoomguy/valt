# Quick Task: Rewrite Reset Button from Scratch

## Description
The Reset button for BTC price simulation has been buggy through multiple attempts. Rewriting the feature from scratch with the simplest possible approach.

## Problem
Previous attempts using `[RelayCommand(CanExecute)]` with CommunityToolkit.Mvvm caused the button to never become active or to not respond on the first click.

## Solution
Simplify the implementation:
1. ViewModel: Use plain `[RelayCommand]` without CanExecute
2. XAML: Use `IsEnabled="{Binding IsCustomPriceActive}"` binding directly on the Button

This removes all command-level CanExecute complexity and relies on simple property binding which works reliably.

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`
