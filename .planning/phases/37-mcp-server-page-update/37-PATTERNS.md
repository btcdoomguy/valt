# Phase 37: MCP Server Page Update - Pattern Map

**Mapped:** 2026-07-16
**Files analyzed:** 4 (2 doc pages + 2 bookkeeping files)
**Analogs found:** 4 / 4

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `../valt-docs/docs/funcionalidades/mcp-server.md` | documentation page (pt-BR) | static content (append-style section addition) | itself — existing 8 category sections (lines 131-225) | exact |
| `../valt-docs/docs/funcionalidades/mcp-server.en.md` | documentation page (en-US mirror) | static content (translation mirror) | `mcp-server.md` + Phase 36 mirror discipline | exact |
| `.planning/REQUIREMENTS.md` | bookkeeping (requirements tracker) | checkbox + traceability table edit | itself — Phase 36 completions (lines 36-38, 93-95) | exact |
| `.planning/ROADMAP.md` | bookkeeping (phase checkbox) | single-line checkbox edit | Phases 32-36 completed checkboxes | exact |

**Source-of-truth references (read-only, not modified):**
- `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — canonical tool names, parameters, `[Description]` attributes (D-05)
- `src/Valt.Infra/Mcp/Tools/IndicatorTools.cs` — canonical indicator tool description

## Pattern Assignments

### `../valt-docs/docs/funcionalidades/mcp-server.md` (documentation page, append-style)

**Analog:** the file itself — the 8 existing category sections in `## Ferramentas Disponíveis 🛠️` (lines 131-225).

**Category section pattern** (lines 131-145 — H3 heading with parenthesized tool class name + intro line + 2-column table):

```markdown
## Ferramentas Disponíveis 🛠️

O servidor MCP do Valt expõe mais de 45 ferramentas organizadas por categoria.

### Contas (AccountTools)

| Ferramenta | Descrição |
|------------|-----------|
| `GetAccounts` | Lista todas as contas |
| `CreateAccount` | Cria uma nova conta |
| `EditAccount` | Edita uma conta existente |
| `DeleteAccount` | Remove uma conta |
| `GetAccountBalance` | Obtém o saldo de uma conta |
| `GetAccountHistory` | Histórico de saldo de uma conta |
```

Conventions to copy:
- H3 heading format: `### <Nome Traduzido> (<ToolClassName>)` — e.g. `### Ativos (AssetTools)`, `### Indicadores (IndicatorTools)`.
- Table header exactly `| Ferramenta | Descrição |` with `|------------|-----------|` separator.
- Tool names are **backticked English code identifiers** in both languages (Phase 35/36 "exact app/code names as labels" precedent).
- One blank line between the H3 heading and the table; sections separated by single blank lines.
- Insertion point: after the last existing category `### Moedas (CurrencyTools)` (ends line 225), before `## Casos de Uso 💡` (line 227).

**Grouped subsection pattern (D-02 — new for this phase, grounded in the page's existing heading hierarchy):**

The page already uses H4 for sub-groups elsewhere (lines 86, 94: `#### Via Open WebUI`, `#### Via CLI`). The AssetTools section reuses that level for its 5 groups:

```markdown
### Ativos (AssetTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/AssetTools.cs -->
<1-line purpose paragraph>

#### Operações de Ativos

| Ferramenta | Descrição |
|------------|-----------|
| `GetAssets` | Lista todos os ativos rastreados |
...
```

**Parameter table pattern (D-03 — labeled tables BELOW the group table, never nested in rows):**

Markdown cannot nest block tables inside table rows (RESEARCH Pitfall 3). Copy this structure:

```markdown
**Parâmetros de `AddLoanStateUpdate`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo |
| `effectiveDate` | Data efetiva (yyyy-MM-dd) |
| `note` | Observação (opcional) |
```

- Bold label `**Parâmetros de \`ToolName\`:**` above each parameter table.
- Parameter names backticked, English, exactly as in the C# signature.
- Optionality marked inline in the description: `(opcional, padrão: hoje)` — matching how `dateSold = ""` and `note = null` appear in code.
- Edge case: `ListSoldAssets` takes no parameters → one-line note `` `ListSoldAssets` não recebe parâmetros. `` instead of an empty table (RESEARCH Pattern 2).

**Source-evidence comment pattern (from `ativos.md:7` and `despesas-fixas.md:76`):**

```markdown
<!-- Source: src/Valt.Core/Modules/Assets/AssetTypes.cs + language.pt-BR.resx -->
O módulo de Ativos foi criado para quem deseja enxergar seu patrimônio completo...
```

Apply as:
- `<!-- Source: src/Valt.Infra/Mcp/Tools/AssetTools.cs -->` directly under the `### Ativos (AssetTools)` heading.
- `<!-- Source: src/Valt.Infra/Mcp/Tools/IndicatorTools.cs -->` directly under the `### Indicadores (IndicatorTools)` heading.
- The mcp-server.md page currently has **zero** such comments; this phase introduces the convention there (Phases 32-34 established it).

**Intro count fix pattern (RESEARCH Pitfall 4 — 2 spots per file):**

```markdown
# line 3:   "...Com mais de 45 ferramentas disponíveis..." → "...Com mais de 80 ferramentas disponíveis..."
# line 133: "O servidor MCP do Valt expõe mais de 45 ferramentas..." → "...expõe mais de 80 ferramentas..."
```

---

### `../valt-docs/docs/funcionalidades/mcp-server.en.md` (documentation page, translation mirror)

**Analog:** `mcp-server.md` (structural twin — verified identical heading/table skeleton at lines 131-225) + Phase 36 mirror discipline.

**Mirror conventions** (verified from the EN file's existing sections, lines 131-225):

```markdown
### Accounts (AccountTools)

| Tool | Description |
|------|-------------|
| `GetAccounts` | Lists all accounts |
```

- Heading parity: same H3/H4 hierarchy, translated labels — `### Assets (AssetTools)`, H4s `#### Asset Operations` / `#### BTC Loans` / `#### Asset Groups` / `#### Loan State Timeline` / `#### Sold Assets`; `### Indicators (IndicatorTools)`.
- Table headers: `| Tool | Description |` / `| Parameter | Description |` with `|------|-------------|` separators.
- Tool/parameter names **stay English backticked identifiers** — never translated (both languages).
- Bold parameter labels: `**Parameters of \`AddLoanStateUpdate\`:**`.
- No-parameter note: `` `ListSoldAssets` takes no parameters. ``
- Identical `<!-- Source: ... -->` comments at identical positions (comments are not translated).
- EN descriptions track the code `[Description]` text closely (see excerpts below).
- Same intro count fix: `"over 45 tools"` → `"over 80 tools"` at lines 3 and 133.
- **Parity gate:** after edits, `grep -c '^#'` and `grep -c '^|'` must match between the two files (RESEARCH Validation Architecture, QA-01).

---

### `.planning/REQUIREMENTS.md` (bookkeeping)

**Analog:** itself — the Phase 36 completion pattern.

**Checkbox flip pattern** (current lines 42-44 → match completed style of lines 36-38):

```markdown
# Before:
- [ ] **MCP-01**: The MCP Server page documents the `AssetTools` category and its tools.
# After (copy style from FXE-01..03 at lines 36-38):
- [x] **MCP-01**: The MCP Server page documents the `AssetTools` category and its tools.
```

**Traceability table pattern** (lines 96-98 → match lines 93-95):

```markdown
# Before:
| MCP-01 | Phase 37 | Pending |
# After (copy style from FXE rows):
| MCP-01 | Phase 37 | Complete |
```

Applies to MCP-01, MCP-02, MCP-03 only. NAV-01/02/03 stay `Pending` (Phase 38).

---

### `.planning/ROADMAP.md` (bookkeeping)

**Analog:** Phases 32-36 completed phase checkboxes (RESEARCH Pattern 5, verified `ROADMAP.md:21-26`).

```markdown
- [x] Phase 37: MCP Server Page Update (completed 2026-07-16)
```

---

## Shared Patterns

### D-05: Descriptions copied verbatim from `[Description]` attributes

**Source:** `src/Valt.Infra/Mcp/Tools/AssetTools.cs` (grep-verified, all 28 tools)
**Apply to:** Both doc pages — every tool description row derives from these; PT wording is the agent's discretion, EN tracks the code text closely.

Verified canonical English descriptions (the spec — do not paraphrase semantics):

```
Line 52:  GetAssets          — "Get all tracked assets (investments like stocks, ETFs, crypto, real estate, etc.)"
Line 62:  GetVisibleAssets   — "Get only visible assets"
Line 72:  GetAsset           — "Get a single asset by its ID"
Line 83:  GetAssetsSummary   — "Get asset summary with totals in main currency and sats, including assets vs liabilities breakdown"
Line 97:  CreateBasicAsset   — "Create a basic asset (stock, ETF, crypto, commodity, or custom)"
Line 138: CreateRealEstateAsset — "Create a real estate asset"
Line 175: CreateLeveragedPosition — "Create a leveraged trading position (futures, perpetuals, margin)"
Line 232: UpdateAssetPrice   — "Update the current price of an asset"
Line 257: UpdateAssetQuantity — "Update the quantity of a basic asset"
Line 282: ToggleAssetVisibility — "Toggle the visibility of an asset"
Line 313: ToggleAssetNetWorthInclusion — "Toggle whether an asset is included in net worth calculation"
Line 344: CreateBtcLoan      — "Create a BTC-collateralized loan (borrowing fiat against BTC collateral). Tracks LTV, health status, and accrued interest. Supports either daily APR accrual or a predefined fixed total debt (HodlHodl-style)."
Line 405: CreateBtcLending   — "Create a lending position (lending fiat/BTC to a borrower or platform). Tracks earned interest."
Line 446: RepayLoan          — "Mark a BTC loan or lending position as repaid"
Line 469: DeleteAsset        — "Delete an asset"
Line 492: GetAssetGroups     — "Get all asset groups"
Line 502: CreateAssetGroup   — "Create an asset group"
Line 527: UpdateAssetGroup   — "Update an asset group name and description"
Line 554: DeleteAssetGroup   — "Delete an asset group. Assets in the group will become ungrouped."
Line 577: MoveAssetToGroup   — "Move an asset to an asset group"
Line 602: RemoveAssetFromGroup — "Remove an asset from its group"
Line 626: AddLoanStateUpdate — "Add a new state snapshot to a BTC-backed loan"
Line 673: DeleteLoanStateUpdate — "Delete a state snapshot from a BTC-backed loan by its effective date"
Line 708: GetLoanStateTimeline — "Get the full chronological snapshot timeline of a BTC-backed loan"
Line 719: GetLatestLoanState — "Get the latest recorded state of a BTC-backed loan"
Line 730: MarkAssetAsSold    — "Marks an asset as sold on the given date (defaults to today)."
Line 766: UndoAssetSale      — "Reverts a previous asset sale and restores the asset to the active list."
Line 789: ListSoldAssets     — "Lists all assets that have been marked as sold."
```

**Corrected grouping (28 tools — RESEARCH Pitfall 2: CONTEXT said 26 and omitted `GetAsset`; code wins):**

| Subsection (pt / en) | Tools | Count |
|----------------------|-------|-------|
| Operações de Ativos / Asset Operations | GetAssets, GetVisibleAssets, GetAsset, GetAssetsSummary, CreateBasicAsset, CreateRealEstateAsset, CreateLeveragedPosition, UpdateAssetPrice, UpdateAssetQuantity, ToggleAssetVisibility, ToggleAssetNetWorthInclusion, DeleteAsset | 12 |
| Empréstimos BTC / BTC Loans | CreateBtcLoan, CreateBtcLending, RepayLoan | 3 |
| Grupos de Ativos / Asset Groups | GetAssetGroups, CreateAssetGroup, UpdateAssetGroup, DeleteAssetGroup, MoveAssetToGroup, RemoveAssetFromGroup | 6 |
| Linha do Tempo do Estado do Empréstimo / Loan State Timeline | 4 loan-state tools + param tables | 4 |
| Ativos Vendidos / Sold Assets | 3 sold-asset tools + param tables | 3 |

### Parameter truth: loan-state tools (AssetTools.cs:626-725)

**Apply to:** Both doc pages, the 4 parameter tables. Copy names/optionality exactly:

```csharp
// AddLoanStateUpdate (lines 627-637) — all required except note
[Description("The asset ID")] string assetId,
[Description("Effective date (yyyy-MM-dd)")] string effectiveDate,
[Description("Borrowed principal still owed at the snapshot date")] decimal totalBorrowed,
[Description("Interest accrued up to the snapshot date")] decimal interestAccruedUntilDate,
[Description("BTC collateral in satoshis")] long collateralSats,
[Description("APR as decimal (e.g., 0.12 for 12%)")] decimal apr,
[Description("Fees paid")] decimal fees,
[Description("Optional note")] string? note = null

// DeleteLoanStateUpdate (lines 677-678)
[Description("The asset ID")] string assetId,
[Description("Effective date (yyyy-MM-dd)")] string effectiveDate

// GetLoanStateTimeline (line 711) / GetLatestLoanState (line 722) — identical single param
[Description("The asset ID")] string assetId
```

Note: the tool validates `yyyy-MM-dd` and returns `"Error: Effective date must be in yyyy-MM-dd format"` on bad input (lines 642-647) — safe to mention in the description.

### Parameter truth: sold-asset tools (AssetTools.cs:730-794)

```csharp
// MarkAssetAsSold (lines 734-735)
[Description("The ID of the asset to mark as sold")] string assetId,
[Description("Sale date in yyyy-MM-dd format (optional, defaults to today)")] string dateSold = ""

// UndoAssetSale (line 770)
[Description("The ID of the sold asset to restore")] string assetId

// ListSoldAssets (lines 790-791) — no parameters
public static async Task<IReadOnlyList<AssetDTO>> ListSoldAssets(IQueryDispatcher queryDispatcher)
```

### Parameter truth: IndicatorTools (IndicatorTools.cs:10)

```csharp
[McpServerTool, Description("Get current Bitcoin macro indicators including Mayer Multiple, Rainbow Chart, Fear & Greed Index, and Bitcoin Dominance. Returns cached data if available.")]
public static IndicatorResultDto GetBitcoinIndicators(IIndicatorCache indicatorCache)
```

Single tool, no user-facing parameters. Cached data behavior (background job `IndicatorsUpdaterJob`) is fair to mention — the tool returns a "No indicator data available yet..." message until the cache is populated (lines 14-20).

### Bilingual mirror discipline (QA-01)

**Source:** Phase 36 established pattern (PT plan then EN mirror plan); both target files verified structurally identical today.
**Apply to:** Every edit — identical heading structure, table row counts, `<!-- Source: -->` comments, and code identifiers in both files; only prose is translated. Terminology mirrors app language files (`Ativos`, `Empréstimo BTC`, `snapshot`, `Histórico de Ativos Vendidos` — verified against `ativos.md` and `language.pt-BR.resx`).

### Build gate

**Apply to:** Final verification of both doc pages.

```bash
cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict
```

Only the `.venv` mkdocs works on this machine (pipx lacks Material theme; system python lacks i18n plugin — RESEARCH Pitfall 1). Baseline build verified green 2026-07-16.

## No Analog Found

None — all 4 files have exact analogs (the target page's own existing sections are the primary pattern source).

## Metadata

**Analog search scope:** `../valt-docs/docs/funcionalidades/` (mcp-server.md, mcp-server.en.md, ativos.md, despesas-fixas.md), `src/Valt.Infra/Mcp/Tools/` (AssetTools.cs, IndicatorTools.cs + 8 sibling tool files via grep), `.planning/REQUIREMENTS.md`
**Files scanned:** 8 (4 full reads, 2 targeted code reads, 1 grep across 10 tool files, 1 bookkeeping read)
**Pattern extraction date:** 2026-07-16
