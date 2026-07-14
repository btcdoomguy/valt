---
phase: quick
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
autonomous: true
requirements: []
must_haves:
  truths:
    - The Name column in the Sold Asset History grid shows only the asset name, with no icon.
    - The Name column text is vertically centered to match the Type and Date Sold columns.
    - The Restore Asset button is centered horizontally instead of stretching to fill the details panel.
  artifacts:
    - path: src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
      provides: Sold Asset History grid layout and Restore Asset button styling
  key_links:
    - from: src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
      to: SoldAssetItemViewModel.Name
      via: DataGridTextColumn Binding="{Binding Name}"
---

<objective>
Fix the Sold Asset History modal layout so the Name column displays plain text without the icon, aligns vertically with the other columns, and the Restore Asset button no longer stretches horizontally.

Purpose: The current grid renders the icon in the Name column using a Material font glyph, which causes misalignment and a non-plain-text appearance. The Restore Asset button currently fills the full width of the details panel, which looks wrong.
Output: Updated XAML with a plain-text Name column and a centered Restore Asset button.
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
@./AGENTS.md
@src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
@src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
</context>

<tasks>
<task type="auto">
  <name>Fix Name column and Restore Asset button layout</name>
  <files>src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml</files>
  <action>
    In the DataGrid Columns collection, replace the Name column DataGridTemplateColumn with a plain DataGridTextColumn bound to Name, preserving Width="*" and the existing header. This removes the icon entirely and lets the DataGrid apply the same default vertical alignment as the Type and Date Sold columns. Then change the Restore Asset Button HorizontalAlignment from "Stretch" to "Center" so it does not fill the details panel width.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>
    - The Name column is a DataGridTextColumn binding to Name with no icon.
    - The Restore Asset button has HorizontalAlignment="Center" and no longer stretches to fill the panel.
    - The solution builds successfully.
  </done>
</task>
</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| UI markup | Only presentation-layer changes; no untrusted input crosses a boundary |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-quick-01 | Information Disclosure | DataGrid shows asset name only | accept | No new data exposure; icon removal reduces displayed surface |
</threat_model>

<verification>
- Build passes with `dotnet build Valt.sln`.
- XAML no longer contains a `TextBlock` binding to `Icon.Unicode` in the Name column.
- The Restore Asset button no longer has `HorizontalAlignment="Stretch"`.
</verification>

<success_criteria>
- The Name column displays plain text asset names without icons.
- Text in the Name column is vertically centered to match adjacent columns.
- The Restore Asset button is centered within the details panel and does not stretch horizontally.
- The solution compiles without errors.
</success_criteria>

<output>
Create `.planning/quick/260714-hbv-it-is-still-bad-take-a-look-at-home-vmab/260714-hbv-SUMMARY.md` when done.
</output>
