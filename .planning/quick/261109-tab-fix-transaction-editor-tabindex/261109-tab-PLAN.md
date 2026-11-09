---
phase: quick-261109-tab
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml
autonomous: true
requirements:
  - QUICK-261109-tab

must_haves:
  truths:
    - Tabbing through the Transaction Editor window visits the "Exclude from goals and reports" checkbox immediately after the Notes textbox
    - The rest of the tab order (form fields 0-9, Notes 10, Date 12, OK 13, Cancel 14) is unchanged
  artifacts:
    - src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml with the exclusion CheckBox carrying TabIndex="11"
  key_links:
    - Exclusion CheckBox at lines 200-207 of TransactionEditorView.axaml had no TabIndex (default 0), hijacking the start of the tab order
---

<objective>
Fix the TabIndex of the Transaction Editor window. After adding the "Exclude from goals and reports" checkbox, the tab order became messy — the checkbox must come after the Notes textbox.

Root cause: the CheckBox (TransactionEditorView.axaml, right pane) has no TabIndex, so it defaults to 0 in Avalonia and is visited at the very start of the tab order instead of after Notes (TabIndex 10).

Purpose: restore sequential keyboard navigation.
Output: One-line XAML change — add TabIndex="11" to the exclusion CheckBox, so the order is form fields (0-9) → Notes (10) → Exclude checkbox (11) → Date (12) → OK (13) → Cancel (14).
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
