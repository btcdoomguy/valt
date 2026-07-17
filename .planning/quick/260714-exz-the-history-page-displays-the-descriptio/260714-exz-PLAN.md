---
phase: quick
plan: 260714-exz
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
autonomous: false
requirements:
  - HISTORY-GRID-ICON
must_haves:
  truths:
    - The Sold Asset History grid shows the asset name with a Material Design glyph, not a raw serialized icon string
    - The icon property is parsed from the stored Icon ID instead of displayed as text
  artifacts:
    - path: src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
      provides: SoldAssetItemViewModel.Icon as Core.Common.Icon parsed from dto.Icon
    - path: src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
      provides: DataGrid name column binds Text="{Binding Icon.Unicode}"
  key_links:
    - from: SoldAssetHistoryViewModel.cs
      to: SoldAssetHistoryView.axaml
      via: SoldAssetItemViewModel.Icon.Unicode is bound to the icon TextBlock
      pattern: Icon.Unicode
---

<objective>
Fix the Sold Asset History grid so the icon column renders the Material Design glyph instead of the raw serialized Icon ID string.
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
</execution_context>

<context>
@./AGENTS.md
@src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
@src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
@src/Valt.Core/Common/Icon.cs
Screenshot: /home/vmabellini/Pictures/valt3/Screenshot from 2026-07-14 10-41-19.png
</context>

<tasks>

<task type="auto">
  <name>Parse the Icon ID in SoldAssetItemViewModel</name>
  <files>src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs</files>
  <action>
    In the SoldAssetItemViewModel nested record, change the Icon property from string to Valt.Core.Common.Icon. Initialize it with Icon.RestoreFromId(dto.Icon) in the constructor. Update the design-time CreateDesignTimeItem helper to accept a Core.Common.Icon parameter and pass Icon.Empty for the sample rows so the design rows remain valid. Add a using for Valt.Core.Common if needed.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>
    SoldAssetItemViewModel.Icon is a Core.Common.Icon parsed from the DTO Icon string and the design-time constructor still compiles.
  </done>
</task>

<task type="auto">
  <name>Bind the grid icon glyph to Icon.Unicode</name>
  <files>src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml</files>
  <action>
    In the DataGridTemplateColumn for the Name column, change the icon TextBlock binding from Text="{Binding Icon}" to Text="{Binding Icon.Unicode}" while keeping the MaterialDesign FontFamily. This causes the TextBlock to render the unicode character instead of the serialized Icon ID.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>
    The DataGrid name column binds the icon TextBlock to the parsed unicode glyph.
  </done>
</task>

<task type="checkpoint:human-verify" gate="blocking">
  <what-built>
    The SoldAssetHistoryViewModel now parses the stored Icon ID string into a Core.Common.Icon, and the grid's icon TextBlock binds to that icon's Unicode property. The build passes and the UI should render the actual glyph.
  </what-built>
  <how-to-verify>
    1. Run the application: dotnet run --project src/Valt.UI/Valt.UI.csproj
    2. Open the Assets tab and either mark an asset as sold or open an existing sold asset.
    3. Open the Sold Asset History modal.
    4. Confirm the Name column shows the asset name preceded by the correct Material Design icon glyph, and no semicolon-separated icon string appears.
  </how-to-verify>
  <resume-signal>Type "approved" or describe any remaining display issue.</resume-signal>
</task>

</tasks>

<threat_model>
## Trust Boundaries
No external trust boundaries crossed.

## STRIDE Threat Register
| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-260714-01 | Information Disclosure | UI grid | accept | Only a serialized icon ID was being displayed; no sensitive data exposed |
</threat_model>

<verification>
1. dotnet build Valt.sln passes.
2. The Sold Asset History modal shows the asset name with the Material Design glyph.
3. The raw serialized icon string no longer appears in the Name column.
</verification>

<success_criteria>
The Name column in the Sold Asset History grid renders the icon glyph and the asset name, not the raw Icon ID string.
</success_criteria>

<output>
Create .planning/quick/260714-exz-the-history-page-displays-the-descriptio/260714-exz-SUMMARY.md when done.
</output>
