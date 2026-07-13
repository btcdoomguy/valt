# Phase 31: MCP, Localization, Documentation, and Verification - Context

**Gathered:** 2026-07-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Finalize the Asset Sold History feature for the v0.5 milestone. This phase exposes the sold-asset operations through MCP, localizes all new user-facing strings added in Phase 30, updates the Asset module documentation, and runs end-to-end verification.

</domain>

<decisions>
## Implementation Decisions

### Localization strings
- **D-01:** Use the `SoldAssetHistory_*` key prefix for all new sold-asset strings. This mirrors the existing `LoanStateHistory_Title` pattern and keeps the feature namespace consistent.
- **D-02:** Localize every new hardcoded Phase 30 string. The required new keys are:
  - Modal title: `SoldAssetHistory_Title` ("Sold Asset History")
  - DataGrid column headers: `SoldAssetHistory_Column_Name`, `SoldAssetHistory_Column_Type`, `SoldAssetHistory_Column_DateSold`
  - Empty state: `SoldAssetHistory_NoSoldAssets`, `SoldAssetHistory_NoSoldAssets_Detail`
  - Error state: `SoldAssetHistory_LoadError`, `SoldAssetHistory_LoadError_Detail` (or reuse a generic error-detail key if one exists)
  - Action labels: `SoldAssetHistory_RestoreAsset`, `SoldAssetHistory_Close`
  - Main Assets tab actions: `SoldAssetHistory_MarkAsSold`, `SoldAssetHistory_History`
  - Date Sold prompt: `SoldAssetHistory_DateSold_Title`, `SoldAssetHistory_DateSold_Prompt`
  - Confirmation prompts: `SoldAssetHistory_MarkAsSoldConfirmation_Title`, `SoldAssetHistory_MarkAsSoldConfirmation_Message`, `SoldAssetHistory_RestoreConfirmation_Title`, `SoldAssetHistory_RestoreConfirmation_Message`, `SoldAssetHistory_RecordProceeds_Title`, `SoldAssetHistory_RecordProceeds_Message`
- **D-03:** Provide English, Portuguese (pt-BR), and Spanish (es) translations in the same PR. Do not leave non-English placeholders.
- **D-04:** Existing card labels reused by `SoldAssetDetailsCard.axaml` are already localized under `Assets_Card_*`. Only the new hardcoded strings listed in D-02 need new keys.

### Agent's Discretion
- **MCP tool design:** Not discussed. The planner should follow the existing `AssetTools` pattern and requirements MCP-01 through MCP-03.
- **Documentation scope:** Not discussed beyond the requirement to update `.claude/docs/assets.md` with sold-history behavior and MCP impact (DOCS-02).
- **Verification approach:** Not discussed beyond the requirement to run end-to-end verification (TEST-03). The planner should define the concrete verification steps.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap and requirements
- `.planning/ROADMAP.md` §Phase 31 — goal, success criteria, and requirements (MCP-01, MCP-02, MCP-03, DOCS-01, DOCS-02, TEST-03).
- `.planning/REQUIREMENTS.md` — v0.5 Asset Sold History requirements; note ASSET-07+ and HIST-05+ are deferred to v2.
- `.planning/PROJECT.md` — project constraints: .NET / Avalonia / LiteDB / CommunityToolkit.Mvvm, backward compatibility, and vertical-slice UI work.
- `.planning/STATE.md` — prior locked decisions: sold-state query filtering, `AssetViewModel`/`AssetDTO` reuse, independence of `IsSold` and `Visible`.
- `.planning/phases/29-domain-persistence-and-active-view-filtering/29-CONTEXT.md` — Phase 29 decisions: `IsSold`/`DateSold`/`PreviousVisibility`, active/sold query split, command surface.
- `.planning/phases/30-history-ui-and-details-reuse/30-CONTEXT.md` — Phase 30 decisions: History modal layout, DataGrid columns, details card reuse, Restore Asset flow, Mark as Sold integration.

### Asset module documentation
- `.claude/docs/assets.md` — existing Assets module documentation; must be updated with sold-history behavior and MCP impact (DOCS-02).

### Backend commands and queries
- `src/Valt.App/Modules/Assets/Commands/MarkAssetAsSold/MarkAssetAsSoldCommand.cs` and `MarkAssetAsSoldHandler.cs` — mark an asset sold with an optional `DateSold`.
- `src/Valt.App/Modules/Assets/Commands/UndoAssetSale/UndoAssetSaleCommand.cs` and `UndoAssetSaleHandler.cs` — restore a sold asset.
- `src/Valt.App/Modules/Assets/Queries/GetSoldAssets/GetSoldAssetsQuery.cs` and `GetSoldAssetsHandler.cs` — returns sold assets ordered by `DateSold` descending.
- `src/Valt.App/Modules/Assets/DTOs/AssetDTO.cs` — includes `IsSold`, `DateSold`, and `PreviousVisibility`.

### MCP tools
- `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — existing asset MCP tools; new sold-asset tools should follow this pattern.
- `src/Valt.Infra/Mcp/Server/McpServerService.cs` — service forwarding for MCP tools.

### UI strings to localize
- `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml` — History modal title, DataGrid columns, empty/error states, Restore Asset button, Close button.
- `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs` — confirmation dialog strings for Restore Asset, error message detail.
- `src/Valt.UI/Views/Main/Modals/DateSoldPrompt/DateSoldPromptView.axaml` and `DateSoldPromptViewModel.cs` — Date Sold prompt title and body.
- `src/Valt.UI/Views/Main/Tabs/Assets/AssetsView.axaml` — Mark as Sold context menu and History toolbar button.
- `src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` — Mark as Sold confirmation, Record proceeds confirmation, and related notification strings.
- `src/Valt.UI/Lang/language.resx`, `src/Valt.UI/Lang/language.pt-BR.resx`, `src/Valt.UI/Lang/language.es.resx` — all new keys must be added to all three files and `language.Designer.cs` updated.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `language.Designer.cs` — auto-generated static properties for resx keys; update it after adding new keys (standard Visual Studio / `resgen` behavior).
- `AssetsViewModel` / `SoldAssetHistoryViewModel` — all hardcoded strings are currently inline; Phase 31 replaces them with `language.*` references.
- `SoldAssetDetailsCard.axaml` — already binds to `Assets_Card_*` keys, so no new localization work is needed for the per-type details card.
- `AssetTools` / command handlers — the backend already supports the operations; Phase 31 only needs to wire them to MCP and update docs.

### Established Patterns
- Localization keys for modal features use an underscore prefix matching the feature name: `LoanStateHistory_Title`, `UpdateLoanState_*`. New keys should follow `SoldAssetHistory_*`.
- Assets tab action keys use `Assets_*` for existing actions. The user chose to use `SoldAssetHistory_*` for the new Mark as Sold / History strings to keep the feature namespace consistent.
- Resx files are kept in sync across `language.resx`, `language.pt-BR.resx`, and `language.es.resx`; the designer file is regenerated from the primary `.resx`.
- MCP tools return string results, publish `McpDataChangedNotification` on success, and follow the command/validator/handler pattern in `Valt.App`.

### Integration Points
- Add new `SoldAssetHistory_*` keys to the three resx files and the designer file.
- Replace hardcoded strings in `SoldAssetHistoryView.axaml`, `SoldAssetHistoryViewModel.cs`, `DateSoldPromptView.axaml`, `DateSoldPromptViewModel.cs`, `AssetsView.axaml`, and `AssetsViewModel.cs` with `{x:Static lang:language.SoldAssetHistory_*}` or `language.SoldAssetHistory_*` references.
- Add MCP tools to `AssetTools.cs` (or a new tool class) and forward any required services in `McpServerService.ForwardServicesFromMainApp()` if necessary.
- Update `.claude/docs/assets.md` with the new sold-state domain behavior, the History UI flow, and the MCP tool additions.
- Verification should include at minimum: `dotnet build`, `dotnet test`, and a manual UI smoke test of Mark as Sold → History → Restore Asset.

</code_context>

<specifics>
## Specific Ideas

- Use `SoldAssetHistory_*` as the single prefix for all new keys, including the Mark as Sold context menu and History toolbar button, to keep the feature cohesive.
- Provide human-quality Portuguese and Spanish translations in the same PR; do not leave them as English placeholders.
- The `SoldAssetDetailsCard` reuses the existing `Assets_Card_*` keys, so no additional localization is needed for the per-type details content.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within the Phase 31 scope. (v2 history enhancements and sale-price/capital-gains tracking remain in `.planning/REQUIREMENTS.md` v2.)

</deferred>

---

*Phase: 31-MCP, Localization, Documentation, and Verification*
*Context gathered: 2026-07-13*
