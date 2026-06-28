---
phase: quick
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/UserControls/FiatInput.axaml.cs
autonomous: true
requirements:
  - QUICK-FIX
must_haves:
  truths:
    - Editing or copying a Bitcoin-only transaction no longer crashes in FiatInput.set_FiatValue.
    - FiatInput tolerates null bindings for FiatValue by treating them as FiatValue.Empty.
  artifacts:
    - path: src/Valt.UI/UserControls/FiatInput.axaml.cs
      provides: Null-safe FiatValue setter and display update
      contains:
        - value ?? FiatValue.Empty
        - _fiatValue ?? FiatValue.Empty
  key_links:
    - from: TransactionEditorViewModel.LoadTransactionDetailsFromDto
      to: FiatInput.FiatValue
      via: Avalonia TwoWay binding pushes null when destination side has no fiat value
      pattern: ToAccountFiatValue = values.ToAccountFiatValue
---

<objective>
Fix the NullReferenceException that occurs when editing or copying a transaction by making `FiatInput` resilient to `null` `FiatValue` bindings.

Purpose: A null `FiatValue` flows from `TransactionDetailsBuilder.LoadFromDto` for Bitcoin-only transactions and for transfers without fiat on the destination side. When the binding pushes that null into `FiatInput.FiatValue`, the setter dereferences it at line 92. The control should coerce `null` to `FiatValue.Empty` instead of crashing.
Output: Updated `FiatInput.axaml.cs` with null-coalescing guards and a green build.
</objective>

<execution_context>
@.claude/docs/budget.md
</execution_context>

<context>
@src/Valt.UI/UserControls/FiatInput.axaml.cs
@src/Valt.UI/Services/TransactionDetailsBuilder.cs
@src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: Guard FiatInput against null FiatValue bindings</name>
  <files>src/Valt.UI/UserControls/FiatInput.axaml.cs</files>
  <action>
    Update the `FiatInput` control so it never dereferences a null `FiatValue`.

    1. In the `FiatValue` property setter, coalesce the incoming `value` to `FiatValue.Empty` before calling `SetAndRaise` and updating `_rawValue`/`UpdateDisplayValue`. Preserve the two-way binding behavior; only change the null handling.
    2. In `UpdateDisplayValue()`, add defense-in-depth by coalescing `_fiatValue` to `FiatValue.Empty` before any formatting logic that touches its value.
    3. In `UpdateFiatValue()`, add defense-in-depth by coalescing `_fiatValue` to `FiatValue.Empty` before comparing or creating a new `FiatValue`.
    4. Do not change `DecimalPlaces`, `CurrencySymbol`, `SymbolOnRight`, `DisplayValue`, calculator properties, or other logic. Do not modify `TransactionDetailsBuilder` or `TransactionEditorViewModel`; the fix belongs in the control because multiple callers may legitimately bind null.
    5. Ensure `FiatValue.Empty` is already imported via the `Valt.Core.Common` namespace.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>
    - `FiatInput.FiatValue` setter coalesces `null` to `FiatValue.Empty`.
    - `UpdateDisplayValue` and `UpdateFiatValue` coalesce `_fiatValue` before use.
    - `dotnet build Valt.sln` completes with no errors.
  </done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| UI control → domain value | Untrusted/null binding data crosses into `FiatInput`; control must sanitize. |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-quick-01 | Denial of Service | `FiatInput.set_FiatValue` | mitigate | Coerce null `FiatValue` to `FiatValue.Empty` at setter and display-update boundaries. |
</threat_model>

<verification>
- Build succeeds: `dotnet build Valt.sln`
- No regressions in existing behavior: binding a real `FiatValue` still formats correctly with the configured currency symbol and decimal places.
</verification>

<success_criteria>
- The app no longer crashes when a user edits or copies a Bitcoin-only transaction or a transfer without destination fiat value.
- `FiatInput` remains valid for both null and non-null `FiatValue` bindings.
</success_criteria>

<output>
Create `.planning/quick/260628-pol-fix-nullreferenceexception-in-fiatinput-/260628-pol-SUMMARY.md` when done.
</output>
