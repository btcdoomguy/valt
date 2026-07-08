---
phase: 21
slug: transaction-editor-child-vms
status: approved
shadcn_initialized: false
preset: not applicable
created: 2026-07-08
reviewed_at: 2026-07-08T12:00:00Z
---

# Phase 21 — Transaction Editor Child VMs UI Design Contract

> Visual and interaction contract for the Transaction Editor refactor. The goal is to split `TransactionEditorViewModel` into per-transfer-type child VMs bound through a `ContentControl` + `DataTemplate`s, while preserving the existing visual appearance and keyboard behavior.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | Avalonia 11.3 Fluent theme (no shadcn) |
| Preset | not applicable |
| Component library | Avalonia built-in controls + Valt custom `UserControls` (`BtcInput`, `FiatInput`, `CustomTitleBar`, `ModuleHelperText`) |
| Icon library | Material Design icons via `MaterialSymbolsOutlined` font |
| Font | `GeistMono` (primary body), `Geist`, `MaterialDesign` (icons) |

---

## Scope Notes

- This is a **refactor**, not a redesign. No new user-facing features.
- Preserve the existing window size, chromeless title bar, colors, typography, and spacing as closely as possible.
- The mode selector (Debt / Credit / Transfer) remains the same three option buttons; only the form area below it becomes a `ContentControl`.

---

## Child VM Selection UI

- The parent `TransactionEditorView.axaml` keeps the existing three `Button` controls with `Classes="transaction-option {debt|credit|transfer}"`.
- The selected mode is tracked by `SelectedMode` (`Debt`, `Credit`, `Transfer`) in the parent VM.
- `Classes.selected` is bound to `DebtSelected`, `CreditSelected`, `TransferSelected` (or equivalent).
- Commands `SwitchToDebtCommand`, `SwitchToCreditCommand`, `SwitchToTransferCommand` switch the active child VM and clear any mode-incompatible state (e.g., `ToAccount` on Debt/Credit, `UseInstallments` on Transfer/Credit).
- Keyboard shortcuts remain unchanged: `Ctrl+1` / `Ctrl+2` / `Ctrl+3` for mode switching, `Ctrl+←` / `Ctrl+→` for date navigation, `Ctrl+R` for today, `Escape` to close, `Enter` to submit.

---

## Child VM Map & DataTemplate Structure

| Child VM | View | DataTemplate key | Responsibility |
|----------|------|------------------|----------------|
| `DebtTransactionEditorViewModel` | `DebtTransactionEditorView` | `DebtTransactionEditorDataTemplate` | Name, Category, From Account, Amount, Installments (add only) |
| `CreditTransactionEditorViewModel` | `CreditTransactionEditorView` | `CreditTransactionEditorDataTemplate` | Name, Category, From Account, Amount (no installments) |
| `TransferTransactionEditorViewModel` | `TransferTransactionEditorView` | `TransferTransactionEditorDataTemplate` | Name, Category, From Account, From Amount, To Account, To Amount, Transfer Rate |

- The parent hosts a `ContentControl` with `Content="{Binding ActiveChildViewModel}"`.
- DataTemplates are declared in the parent view's resources (or inline on the `ContentControl`). Each template maps a child VM type to its view:

```xml
<DataTemplate DataType="local:DebtTransactionEditorViewModel">
    <views:DebtTransactionEditorView />
</DataTemplate>
```

- All child views set `x:DataType` to their VM for compiled bindings.
- The parent view's `x:DataType` remains `TransactionEditorViewModel`.
- **No animations or transitions** on the `ContentControl`; mode switches must be immediate.

---

## Layout & Spacing

- The active child form area in the left pane is the primary focal point; the type-selector bar at the top provides the secondary anchor.
- Window: fixed `820 x 455` (`MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight` all match).
- Left pane: `Border` `Classes="form-fields-area"` `Width="500"` `Padding="8"` `Margin="4"`.
- Right pane: `Border` `Classes="form-fields-area"` `Width="276"` `Padding="8"` `Background="{DynamicResource Background800Brush}"`.
- Shared top area (inside left pane): type selector bar, then date picker row.
- ContentControl area: below the date picker, fills the rest of the left pane, hosting the active child view.
- Inside each child view: vertical `StackPanel` `Spacing="8"`, with form rows as horizontal `StackPanel` `Spacing="8"`.
- Form field label: `TextBlock` `Margin="0,0,0,4"`.
- Form field container: `StackPanel` `Classes="form-field"` `Margin="0,4,0,4"`.
- Bottom action buttons: horizontal `StackPanel` `Spacing="8"` `Margin="0,0,0,4"` `HorizontalAlignment="Center"`, each `Width="100"`.
- Right pane notes: `TextBox` `Height="160"`.
- Right pane metadata card: `Border` `Background="{DynamicResource Background900Brush}"` `Padding="8"` `MinHeight="90"`.

### Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | label-to-input gaps, micro spacing |
| sm | 8px | compact row spacing, small padding |
| md | 16px | pane padding, section gaps |
| lg | 24px | larger section gaps |
| xl | 32px | not used in this modal |
| 2xl | 48px | not used in this modal |
| 3xl | 64px | not used in this modal |

**Exceptions:** None. All spacing values in this modal now adhere to the 4px grid (4, 8, 12, 16, 24, 32, 48, 64). Historical 5px values have been replaced to satisfy the spacing contract.

---

## Typography

| Role | Resource | Size | Weight | Line Height |
|------|----------|------|--------|-------------|
| Body / Label | `GeistMono` default / `FontSizeNormal` | 13 | Normal (400) | 1.5 |
| Option button label | `FontSizeMedium` | 14 | Normal (400) | 1.2 |
| Heading | `FontSizeLarge` | 16 | SemiBold (600) | 1.2 |
| Caption / metadata | `FontSizeXSmall` | 11 | Normal (400) | 1.4 |

- Use the existing dynamic resources (`FontSizeNormal`, `FontSizeMedium`, `FontSizeLarge`, `FontSizeXSmall`). Do not hardcode font sizes.
- The title bar text and modal helper section headers use `FontSizeLarge` + `SemiBold`.
- Material Design icons render at `FontSizeXXLarge` (20) inside the option buttons and category/account dropdown rows; this is treated as an icon size, not a typography role.

---

## Color

| Role | Brush | Default dark hex | Usage |
|------|-------|------------------|-------|
| Dominant (60%) | `Background800Brush` | `#333333` | Main window and pane backgrounds |
| Secondary (30%) | `Background900Brush` | `#1a1a1a` | Input backgrounds, metadata card, right pane fill |
| Accent (10%) | `Accent500Brush` / `Accent700Brush` | `#e98805` | Primary buttons, focus borders, checked checkbox, calendar today, hover accents |
| Destructive | `SemanticNegative600Brush` | `#7e0a0a` | Delete/confirm-delete actions only |
| Validation | `SemanticWarning200Brush` | `#ff5555` | Error icon in `DataValidationErrors` |
| Disabled | `Disabled900Brush` / `Disabled600Brush` | `#1a1a1a` / `#666666` | Disabled input backgrounds and borders |
| Body text | `Text100Brush` | `#fdfdfc` | Labels, body text |
| Muted text | `Text500Brush` | `#83807c` | Placeholders, metadata empty state |

**Accent is reserved for:**
- Primary action buttons (`Save Transaction` / `Create Transfer` / `Save Transfer`)
- Input focus borders (`TextBox`, `ComboBox`, `AutoCompleteBox`, `NumericUpDown`, `CalendarDatePicker`)
- Calendar today highlight
- Checked `CheckBox` background
- Selected / hover state of the type selector buttons
- `TextBox` caret and selection

**Destructive is reserved for:** delete or confirm-delete actions only. There are no destructive actions in this modal.

---

## Copywriting Contract

| Element | Copy | Resource key | Notes |
|---------|------|--------------|-------|
| Primary CTA (Debt/Credit) | **Save Transaction** | `TransactionEditor_SaveTransaction` | New key; add to `language.resx`, `language.pt-BR.resx`, `language.es.resx`, and `language.Designer.cs` |
| Primary CTA (Transfer, add) | **Create Transfer** | `TransactionEditor_CreateTransfer` | New key; localize as above |
| Primary CTA (Transfer, edit) | **Save Transfer** | `TransactionEditor_SaveTransfer` | New key; localize as above |
| Cancel | **Cancel** | `CancelButton` | Existing key |
| Mode labels | **Debt / Credit / Transfer** | `ManageTransactions_Debt`, `ManageTransactions_Credit`, `ManageTransactions_Transfer` | Existing keys |
| Empty metadata state | **No additional properties** | `ManageTransactions.Properties.None` | Existing key |
| Error loading transaction | **Transaction not found** | `Error.TransactionNotFound` | Existing key; close dialog after message box |
| Required validation | "Value is required" / "Origin value is required" / "Destination account is required" | Existing validation attributes | Keep existing messages |

**Destructive confirmation:** None in this modal. The cancel button discards changes without a confirmation.

---

## Accessibility

- Keep all existing keyboard shortcuts in `TransactionEditorView.axaml.cs` (`Ctrl+1`/`2`/`3`, `Ctrl+←`/`→`, `Ctrl+R`, `Escape`, `Enter`).
- Preserve `TabIndex` order across shared and child controls:
  - `Name` (0)
  - `Category` (1)
  - `From Account` (2)
  - `From Amount` (3 / 4)
  - `Use Installments` checkbox (5)
  - `Installment Count` (6)
  - `To Account` (7)
  - `To Amount` (8 / 9)
  - `Notes` (10)
  - `Date` (12)
  - `OK` (13)
  - `Cancel` (14)
- Set `AutomationProperties.Name` on the three transaction-option buttons to the localized mode labels (`Debt`, `Credit`, `Transfer`).
- When mode changes, ensure focus is not lost; if it is, move focus to the first focusable input of the new child view.
- Retain Fluent default focus visuals for all inputs and buttons.
- Use the existing `DataValidationErrors` error template for every validated field.
- Every input must have a visible `TextBlock` label.

---

## DataTemplate / ContentControl Details

- `ContentControl.Content` binds to the active child VM.
- DataTemplates can be inline in the parent view or in a dedicated `ResourceDictionary`.
- If a child view is a separate `UserControl`, declare it in the parent view's namespace and use it as the root of the template.
- Compiled bindings must be enabled on child views with `x:DataType="local:ChildViewModel"`.
- The parent VM is responsible for:
  - Switching the active child VM on mode change.
  - Exposing shared properties (`Date`, `Name`, `Category`, `Notes`, available lists, metadata, `OkCommand`, `CancelCommand`).
  - Reading the active child's values to build the `TransactionDetailsDto` via `ITransactionDetailsBuilder`.
- Child VMs are responsible for:
  - Their own mode-specific properties (`FromAccount`, `ToAccount`, amounts, `UseInstallments`, etc.).
  - Their own validation attributes (or exposing validation state to the parent).
- The `ContentControl` must not use a transition/animation so the form appears instantly.

---

## Design-Time Support

- Keep the existing parameterless constructor in `TransactionEditorViewModel` so the Avalonia designer can instantiate it.
- Provide a design-time `Request` or default state so the designer shows the Debt mode by default.
- If child views are separate `UserControls`, provide parameterless design-time constructors or design-time data contexts so they render in the parent designer.

---

## Implementation Notes

- `TransactionEditorViewModel` remains the shell; child VMs handle the per-mode form state.
- The mode switch commands must update `ActiveChildViewModel` and reset mode-incompatible state.
- Focus helpers in `TransactionEditorView.axaml.cs` (e.g., focusing the amount input after the `AutoCompleteBox` dropdown closes) must be updated to locate the active child view or call a child-view method. Avoid naming controls inside a `DataTemplate` from the code-behind.
- The `ProcessEnterCommand` logic that checks which input is focused should be updated to query the active child VM (e.g., via an `IsAnyAmountFocused` property or interface) rather than inspecting the parent VM directly.
- Transfer-rate display belongs to the Transfer child VM; it still runs on a background task but updates the UI through the child VM's properties.

---

## Registry Safety

Not applicable — this is an Avalonia desktop project with no shadcn registry or third-party UI component registries.

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| n/a | n/a | n/a |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
