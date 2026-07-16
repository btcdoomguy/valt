# Phase 37: MCP Server Page Update - Context

**Gathered:** 2026-07-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Update the public Valt **MCP Server** page (`valt-docs/docs/funcionalidades/mcp-server.md`) so it lists the complete current toolset: add the missing **AssetTools** category (26 tools), documenting the loan-state tools (`AddLoanStateUpdate`, `DeleteLoanStateUpdate`, `GetLoanStateTimeline`, `GetLatestLoanState`) and sold-asset tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`) **with their parameters**. All changes must be mirrored in the English `mcp-server.en.md` translation. No Valt application code is modified in this phase.

</domain>

<decisions>
## Implementation Decisions

### AssetTools section structure
- **D-01:** Add AssetTools as a new category section `### Ativos (AssetTools)` inside the existing **"Ferramentas Disponíveis 🛠️"** section, positioned consistently with the existing category ordering (after the Budget categories, near ReportTools/CurrencyTools — exact position decided by planner to match the page's logical flow).
- **D-02:** Split the 26 AssetTools into **grouped subsections** rather than one flat table: core asset operations (CRUD, price/quantity, visibility), asset groups, BTC loans/lending, loan-state timeline, and sold assets. Each group keeps the page's existing **2-column table style** (`| Ferramenta | Descrição |`).

### Parameter documentation depth
- **D-03:** Document parameters for **exactly the 7 required tools** — the 4 loan-state tools and 3 sold-asset tools — using a small parameter table (`| Parâmetro | Descrição |`) under each tool's row/entry.
- **D-04:** Keep **all other tools** (the remaining AssetTools and all pre-existing categories) in the current 2-column format without parameter tables. This phase does not retrofit parameters onto already-documented categories.
- **D-05:** Parameter names, optionality, and semantics must be copied from the actual tool method signatures in `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — including the `[Description(...)]` attributes — not invented.

### IndicatorTools coverage
- **D-06:** Add a **brief IndicatorTools section** (`### Indicadores (IndicatorTools)`) documenting its Bitcoin macro-indicators tool (Mayer Multiple, Rainbow Chart, Fear & Greed Index, Bitcoin Dominance), because the phase goal requires the *complete current toolset*. Requirements traceability stays on MCP-01/02/03; IndicatorTools is goal-aligned completeness, not a new requirement.

### the agent's Discretion
- Portuguese wording of tool descriptions (must stay consistent with existing page tone and app terminology).
- Exact subsection titles and grouping boundaries inside AssetTools.
- Placement of the new AssetTools/IndicatorTools sections relative to existing categories.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap and requirements
- `.planning/ROADMAP.md` §Phase 37 — goal, success criteria, and requirements (MCP-01, MCP-02, MCP-03).
- `.planning/REQUIREMENTS.md` — v0.6 documentation requirements; MCP-01 through MCP-03 are the scope for this phase.
- `.planning/PROJECT.md` — v0.6 milestone context; documentation site only, no app code changes.
- `.planning/STATE.md` — current milestone state and accumulated context.

### MCP tool source of truth (code)
- `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — all 26 asset tools with exact method names, parameters, and `[Description]` attributes (loan-state tools at lines ~700-730; sold-asset tools; asset groups; CRUD).
- `src/Valt.Infra/Mcp/Tools/IndicatorTools.cs` — Bitcoin macro-indicators tool description.
- `AGENTS.md` §MCP Server — tool structure, conventions, and MCP impact checklist.

### Internal module docs (behavior descriptions)
- `.claude/docs/assets.md` — accurate source for asset types, loans, loan-state timeline, and sold-history behavior.

### Public docs source and site configuration
- `../valt-docs/docs/funcionalidades/mcp-server.md` — Portuguese MCP Server page to update (currently documents 8 categories; AssetTools and IndicatorTools missing).
- `../valt-docs/docs/funcionalidades/mcp-server.en.md` — English MCP Server page to mirror.
- `../valt-docs/mkdocs.yml` — site configuration (no new pages expected in this phase).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable assets
- `src/Valt.Infra/Mcp/Tools/AssetTools.cs` contains the exact tool names, parameter lists, and `[Description]` attributes — the canonical source for the new tables.
- The existing MCP page already has 8 category sections in a consistent 2-column table format to reuse.
- `.claude/docs/assets.md` provides accurate prose for loan-state timeline and sold-history behavior to adapt for tool descriptions.

### Established patterns
- The public docs site is Material for MkDocs with Portuguese source files and `.en.md` English translations; pages are kept bilingually in sync in the same phase.
- The `valt-docs` repository is separate from the main `valt` repository; this phase works across both repositories (doc commits in valt-docs, planning artifacts in valt — `.planning/` files may need `git add -f`).
- Phases 32-34 established that source-evidence HTML comments (`<!-- Source: ... -->`) should be placed above factual claims derived from the code.
- Phase 33 established that new feature documentation stays inline on the existing feature page unless a standalone page is explicitly required.
- Phase 35/36 established that exact app/code names should be used as labels in docs tables.
- `mkdocs build --strict` in `../valt-docs` is the final verification gate.

### Integration points
- The plan must open/edit files in `../valt-docs` (sibling to the current workspace), not inside the `valt` repository.
- No new navigation entries are expected — all new content stays inline on the existing MCP Server page.
- The plan should update `.planning/REQUIREMENTS.md` MCP-01/MCP-02/MCP-03 status to `Complete` after the docs are written and verified.

</code_context>

<specifics>
## Specific Ideas

### Required tools with parameters (from AssetTools.cs)
- **Loan-state:** `AddLoanStateUpdate`, `DeleteLoanStateUpdate`, `GetLoanStateTimeline`, `GetLatestLoanState` (at AssetTools.cs:720).
- **Sold-asset:** `MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`.

### Existing page structure to preserve
Sections: O Que é MCP, Habilitando o Servidor, Conectando com LLMs (Claude Desktop, Ollama, ChatGPT, Outros LLMs), Ferramentas Disponíveis (8 categories), Casos de Uso (Consultando Dados, Importação em Lote). The phase adds AssetTools and IndicatorTools categories without removing or restructuring existing sections.

### Grouping suggestion for AssetTools (26 tools)
- Core assets: `GetAssets`, `GetVisibleAssets`, `GetAssetsSummary`, `CreateBasicAsset`, `CreateRealEstateAsset`, `CreateLeveragedPosition`, `UpdateAssetPrice`, `UpdateAssetQuantity`, `ToggleAssetVisibility`, `ToggleAssetNetWorthInclusion`, `DeleteAsset`.
- Loans/lending: `CreateBtcLoan`, `CreateBtcLending`, `RepayLoan`.
- Asset groups: `GetAssetGroups`, `CreateAssetGroup`, `UpdateAssetGroup`, `DeleteAssetGroup`, `MoveAssetToGroup`, `RemoveAssetFromGroup`.
- Loan-state timeline (with parameters): the 4 loan-state tools.
- Sold assets (with parameters): the 3 sold-asset tools.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within the Phase 37 scope. (Retrofitting parameter tables onto the 8 pre-existing categories, screenshots, and translation to languages beyond pt-BR/en-US remain deferred to future phases.)

</deferred>

---

*Phase: 37-MCP Server Page Update*
*Context gathered: 2026-07-16*
