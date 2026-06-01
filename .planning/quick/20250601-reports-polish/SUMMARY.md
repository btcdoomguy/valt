---
status: complete
---

# Summary: Reports Polish Fixes

## Completed

### 1. Removed BTC Price Simulator Dashboard
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Removed the entire SimulatedPrices dashboard Grid (MinHeight="150" with DashboardGridPanel.RowSpan binding) from the Reports > Summary view. The feature was redundant since the fixed price bar already shows the simulation status.

### 2. Reduced FixedPriceConfig Modal Height
**File**: `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml`

Reduced window dimensions from 400 to 280:
- `d:DesignHeight`: 400 → 280
- `MinHeight`: 400 → 280
- `MaxHeight`: 400 → 280

This removes the excessive empty space below the form fields.

### 3. Fixed Reset Button First-Click Issue
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Changed the Reset button from `IsVisible="{Binding IsCustomPriceActive}"` to `IsEnabled="{Binding IsCustomPriceActive}"`. This fixes an Avalonia bug where buttons with `IsVisible` bindings don't respond to the first click after becoming visible.

## Verification
- Build succeeded: 0 errors
- Tests passed: 49/49 (Reports filter)
