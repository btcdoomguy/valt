---
phase: quick
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/UserControls/BtcInput.axaml.cs
autonomous: true
requirements:
  - QUICK-FIX
must_haves:
  truths:
    - Copying a fiat transaction no longer crashes in BtcInput.UpdateDisplayValue.
    - BtcInput tolerates null bindings for BtcValue by treating them as BtcValue.Empty.
  artifacts:
    - path: src/Valt.UI/UserControls/BtcInput.axaml.cs
      provides: Null-safe BtcValue setter and display update
      contains:
        - value ?? BtcValue.Empty
        - _btcValue ?? BtcValue.Empty
  key_links:
    - from: TransactionEditorViewModel.LoadTransactionDetailsFromDto
      to: BtcInput.BtcValue
      via: Avalonia TwoWay binding pushes null for fiat transactions
      pattern: FromAccountBtcValue = values.FromAccountBtcValue
---

<objective>
Fix the NullReferenceException that occurs when copying a fiat transaction by making `BtcInput` resilient to `null` `BtcValue` bindings.

Purpose: A null `BtcValue` flows from `TransactionDetailsBuilder.LoadFromDto` for fiat transactions and crashes inside `BtcInput.UpdateDisplayValue` when the binding pushes it into the control. The control should coerce `null` to `BtcValue.Empty` instead of dereferencing it.
Output: Updated `BtcInput.axaml.cs` with null-coalescing guards and a green build.
</objective>

<execution_context>
@.claude/docs/budget.md
</execution_context>

<context>
@src/Valt.UI/UserControls/BtcInput.axaml.cs
@src/Valt.UI/Services/TransactionDetailsBuilder.cs
@src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: Guard BtcInput against null BtcValue bindings</name>
  <files>src/Valt.UI/UserControls/BtcInput.axaml.cs</files>
  <action>
    Update the `BtcInput` control so it never dereferences a null `BtcValue`.

    1. In the `BtcValue` property setter, coalesce the incoming `value` to `BtcValue.Empty` before calling `SetAndRaise` and `UpdateDisplayValue`. Preserve the two-way binding behavior; only change the null handling.
    2. In `UpdateDisplayValue()`, add defense-in-depth by coalescing `_btcValue` to `BtcValue.Empty` before calling `ToBitcoinString()` or `ToString()`.
    3. Do not change `IsBitcoin`, `DisplayValue`, or other logic. Do not modify `TransactionDetailsBuilder` or `TransactionEditorViewModel`; the fix belongs in the control because multiple callers may legitimately bind null.
    4. Ensure `BtcValue.Empty` is already imported via `Valt.Core.Common` namespace.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>
    - `BtcInput.BtcValue` setter coalesces `null` to `BtcValue.Empty`.
    - `UpdateDisplayValue` coalesces `_btcValue` before formatting.
    - `dotnet build Valt.sln` completes with no errors.
  </done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| UI control → domain value | Untrusted/null binding data crosses into `BtcInput`; control must sanitize. |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-quick-01 | Denial of Service | `BtcInput.UpdateDisplayValue` | mitigate | Coerce null `BtcValue` to `BtcValue.Empty` at setter and display-update boundaries. |
</threat_model>

<verification>
- Build succeeds: `dotnet build Valt.sln`
- No regressions in existing behavior: binding a real `BtcValue` still formats correctly in sats/BTC modes.
</verification>

<success_criteria>
- The app no longer crashes when a user copies a fiat transaction.
- `BtcInput` remains valid for both null and non-null `BtcValue` bindings.
</success_criteria>

<output>
Create `.planning/quick/260628-pgi-fix-nullreferenceexception-in-btcinput-w/260628-pgi-SUMMARY.md` when done.
</output>
