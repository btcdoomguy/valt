# Phase 37: MCP Server Page Update - Research

**Researched:** 2026-07-16
**Domain:** MkDocs documentation / public MCP Server page update (bilingual pt-BR + en-US)
**Confidence:** HIGH

<user_constraints>
## User Constraints (from 37-CONTEXT.md)

### Locked Decisions
- **D-01:** Add AssetTools as a new category section `### Ativos (AssetTools)` inside the existing **"Ferramentas Disponíveis 🛠️"** section, positioned consistently with the existing category ordering (after the Budget categories, near ReportTools/CurrencyTools — exact position decided by planner to match the page's logical flow).
- **D-02:** Split the AssetTools into **grouped subsections** rather than one flat table: core asset operations (CRUD, price/quantity, visibility), asset groups, BTC loans/lending, loan-state timeline, and sold assets. Each group keeps the page's existing **2-column table style** (`| Ferramenta | Descrição |`).
- **D-03:** Document parameters for **exactly the 7 required tools** — the 4 loan-state tools and 3 sold-asset tools — using a small parameter table (`| Parâmetro | Descrição |`) under each tool's row/entry.
- **D-04:** Keep **all other tools** (the remaining AssetTools and all pre-existing categories) in the current 2-column format without parameter tables. This phase does not retrofit parameters onto already-documented categories.
- **D-05:** Parameter names, optionality, and semantics must be copied from the actual tool method signatures in `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — including the `[Description(...)]` attributes — not invented.
- **D-06:** Add a **brief IndicatorTools section** (`### Indicadores (IndicatorTools)`) documenting its Bitcoin macro-indicators tool (Mayer Multiple, Rainbow Chart, Fear & Greed Index, Bitcoin Dominance). Requirements traceability stays on MCP-01/02/03; IndicatorTools is goal-aligned completeness, not a new requirement.
- **Scope:** Public docs only (`valt-docs/docs/funcionalidades/mcp-server.md` + `mcp-server.en.md`). No Valt application code is modified in this phase.

### the agent's Discretion
- Portuguese wording of tool descriptions (must stay consistent with existing page tone and app terminology).
- Exact subsection titles and grouping boundaries inside AssetTools.
- Placement of the new AssetTools/IndicatorTools sections relative to existing categories.

### Deferred Ideas (OUT OF SCOPE)
- Retrofitting parameter tables onto the 8 pre-existing categories.
- Screenshots.
- Translation to languages beyond pt-BR/en-US.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **MCP-01** | The MCP Server page documents the `AssetTools` category and its tools. | Full 28-tool inventory verified from `AssetTools.cs` (grep count + method list); corrected grouping (incl. `GetAsset`, omitted from CONTEXT) and ready-to-use pt/en descriptions drafted in Code Examples. |
| **MCP-02** | The MCP Server page documents loan-state tools (`AddLoanStateUpdate`, `DeleteLoanStateUpdate`, `GetLoanStateTimeline`, `GetLatestLoanState`). | Exact signatures, `[Description]` attributes, and optionality extracted from `AssetTools.cs:626-725`; behavior prose sourced from `.claude/docs/assets.md` §Loan-State. |
| **MCP-03** | The MCP Server page documents sold-asset tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`). | Exact signatures + `[Description]` attributes extracted from `AssetTools.cs:727-794`; behavior prose sourced from `.claude/docs/assets.md` §Sold-State and `ativos.md`. |
</phase_requirements>

## Project Constraints (from AGENTS.md)

- **No app code changes:** This milestone updates only the public documentation site (`valt-docs`). No `src/` edits in the `valt` repo (REQUIREMENTS.md §Out of Scope; CONTEXT §Phase Boundary).
- **MCP source of truth:** `AGENTS.md` §MCP Server defines the tool structure (`[McpServerToolType]` classes, `[McpServerTool, Description]` methods). All 10 tool classes are auto-registered via `.WithToolsFromAssembly(...)` `[VERIFIED: McpServerService.cs:131]` — so AssetTools and IndicatorTools are live and must be documented.
- **MCP impact checklist (AGENTS.md):** This phase changes no tools, services, DTOs, or parameters — no `ForwardServicesFromMainApp()` or tool updates required. The checklist is satisfied by documentation accurately reflecting the code.
- **Terminology mirrors app language files:** STATE.md decision — public docs terminology mirrors `language.resx` / `language.pt-BR.resx` to avoid UI/docs drift. Verified loan/sold/indicator labels below.
- **Localization rule (docs analog):** AGENTS.md requires all language files updated together; the docs equivalent is QA-01 — every pt-BR change mirrored in the `.en.md` file in the same phase.
- **Cross-repo commits:** Doc commits happen in `../valt-docs` (prefix `docs(37-XX):`, matching `docs(36-01):` history); planning artifacts in `valt` under `.planning/` — new files require `git add -f` (`.planning/` is in `.gitignore`; 78 existing files are tracked) `[VERIFIED: .gitignore:43 + git ls-files]`.

## Summary

Phase 37 adds the two missing MCP tool categories — **AssetTools** (28 tools) and **IndicatorTools** (1 tool) — to the public MCP Server page in the sibling `valt-docs` repo, in both Portuguese and English. The page currently documents 8 categories (52 tools); after this phase it documents 10 categories (81 tools). Parameters are documented only for the 7 required tools (4 loan-state + 3 sold-asset), copied verbatim from the C# signatures per D-05.

Two factual corrections versus CONTEXT.md were found and must flow into the plan: (1) **AssetTools contains 28 tools, not 26** — CONTEXT's grouping omits `GetAsset` (single asset by ID); (2) the page's **"mais de 45 ferramentas" claim goes stale** — actual total is 90 tools (81 documented after this phase), so "mais de 80" is the accurate replacement. Code wins over stale planning docs (established STATE.md precedent).

The final gate is `mkdocs build --strict`, which **only works inside the `valt-docs/.venv`** — both the pipx `mkdocs` on PATH and the system `python3 -m mkdocs` fail on this machine (missing Material theme / missing i18n plugin respectively). Baseline build verified green today (exit 0, INFO-only output).

**Primary recommendation:** Append `### Ativos (AssetTools)` (5 grouped subsections, 28 tools, parameter tables for the 7 required tools) and `### Indicadores (IndicatorTools)` (1 tool) at the end of the existing `## Ferramentas Disponíveis 🛠️` section in both language files, using the ready-made content blocks in §Code Examples, then update REQUIREMENTS.md MCP-01/02/03 to Complete.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| User-facing MCP tool documentation | `valt-docs` repo (`docs/funcionalidades/mcp-server.md` + `.en.md`) | — | Docs site owns all public feature documentation; changes stay inline on the existing page (Phase 33 precedent). |
| Tool names, parameters, descriptions (truth) | `valt` repo `src/Valt.Infra/Mcp/Tools/AssetTools.cs`, `IndicatorTools.cs` | `.claude/docs/assets.md` (behavior prose) | D-05: signatures and `[Description]` attributes are canonical; module doc supplies loan/sold behavior context. |
| Bilingual parity | Both markdown files edited in the same phase | `mkdocs-static-i18n` suffix routing | QA-01; Phase 36 established mirror-everything (including prose tightening). |
| Requirement/bookkeeping tracking | `.planning/REQUIREMENTS.md` (checkboxes + traceability) | `.planning/ROADMAP.md` (phase checkbox) | CONTEXT §Integration points. |
| Validation gate | `mkdocs build --strict` inside `valt-docs/.venv` | grep-based content checks in plan | Build is the final verification gate (CONTEXT). |

## Standard Stack

### Core (already installed — nothing to install)

| Tool | Version | Purpose | Why Standard |
|------|---------|---------|--------------|
| MkDocs | 1.6.1 `[VERIFIED: .venv mkdocs --version]` | Static site build + strict-mode gate | Project's docs engine; used by Phases 32–36 |
| mkdocs-material | 9.7.1 `[VERIFIED: .venv pip list]` | Theme (admonitions, tables, tabbed content) | Configured in `mkdocs.yml` |
| mkdocs-static-i18n | 1.3.0 `[VERIFIED: .venv pip list]` | pt/en routing via `.en.md` suffix | Configured in `mkdocs.yml` (`docs_structure: suffix`) |
| Python | 3.12.3 `[VERIFIED: python3 --version]` | Runtime for the venv | — |

**Activation command (mandatory — see Pitfall 1):**
```bash
cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict
```

### Supporting

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `grep`/`rg` | Verify tool names/params appear in both pages | Post-edit content checks (see Validation Architecture) |
| `git` (valt-docs, branch `master`, clean) `[VERIFIED: git status]` | Doc commits with `docs(37-XX):` prefix | After each wave/plan per Phase 36 pattern |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `valt-docs/.venv` mkdocs | pipx `mkdocs` on PATH | **Broken** — pipx env lacks mkdocs-material (`Unrecognised theme name: 'material'`) `[VERIFIED by running]` |
| `valt-docs/.venv` mkdocs | system `python3 -m mkdocs` (1.5.3) | **Broken** — system env lacks `mkdocs-static-i18n` (`The "i18n" plugin is not installed`) `[VERIFIED by running]` |

## Package Legitimacy Audit

**Not applicable — this phase installs zero external packages.** All tooling already exists in the `valt-docs/.venv` (verified above). No `npm install`, `pip install`, or `cargo add` steps may appear in the plan. If any plan task proposes installing a package, it is out of scope and should be rejected at plan-check.

## Architecture Patterns

### System Architecture Diagram

```
valt repo (source of truth)                valt-docs repo (public site)
─────────────────────────                ─────────────────────────────
src/Valt.Infra/Mcp/Tools/
  AssetTools.cs (28 tools) ──┐
  IndicatorTools.cs (1 tool) ┤  read-only reference
  [Description] attributes   ▼
                        ┌─────────────────────────────┐
                        │  New page sections (inline) │
                        │  ### Ativos (AssetTools)    │
                        │    ├─ 4 group tables (2col) │
                        │    ├─ loan-state group      │
                        │    │    + 4 param tables    │
                        │    └─ sold-assets group     │
                        │         + 3 param tables    │
                        │  ### Indicadores            │
                        │    └─ 1-row table           │
                        └──────┬──────────┬───────────┘
                               │          │
                     mcp-server.md   mcp-server.en.md
                          (pt-BR)        (en-US)
                               │          │
                               └────┬─────┘
                                    ▼
                    .venv: mkdocs build --strict  →  exit 0 (gate)
                                    ▼
              .planning/REQUIREMENTS.md MCP-01/02/03 → Complete
```

### Recommended Doc Structure (insertion map)

Existing `## Ferramentas Disponíveis 🛠️` order (both files) `[VERIFIED: mcp-server.md:131-225, mcp-server.en.md:131-225]`:
1. Contas / Accounts (AccountTools)
2. Transações / Transactions (TransactionTools)
3. Categorias / Categories (CategoryTools)
4. Despesas Fixas / Fixed Expenses (FixedExpenseTools)
5. Metas / Goals (GoalTools)
6. Preço Médio / Average Price (AvgPriceTools)
7. Relatórios / Reports (ReportTools)
8. Moedas / Currencies (CurrencyTools)
9. **→ NEW: Ativos / Assets (AssetTools)** — 1-line purpose intro + 5 grouped subsections
10. **→ NEW: Indicadores / Indicators (IndicatorTools)** — 1-line intro + single-row table

**Placement recommendation (the agent's discretion):** append both new H3 sections after `### Moedas (CurrencyTools)`. Rationale: zero disruption to the 8 existing sections, "newest categories last" matches how the page evolved, and both new sections stay adjacent. Alternative (also valid): AssetTools immediately after Fixed Expenses to mirror the `AGENTS.md` tool-folder ordering; IndicatorTools still last.

### Pattern 1: Grouped category section with 2-column tables (D-02)

**What:** The AssetTools H3 contains a short purpose paragraph, then five H4 subsections, each with the page's standard 2-column table.
**When to use:** For the 28-tool AssetTools category — a flat table would bury the 7 requirement-critical tools.
**Corrected grouping** (CONTEXT omitted `GetAsset`; code wins):

| Subsection (pt / en) | Tools | Count |
|----------------------|-------|-------|
| Operações de Ativos / Asset Operations | `GetAssets`, `GetVisibleAssets`, `GetAsset`, `GetAssetsSummary`, `CreateBasicAsset`, `CreateRealEstateAsset`, `CreateLeveragedPosition`, `UpdateAssetPrice`, `UpdateAssetQuantity`, `ToggleAssetVisibility`, `ToggleAssetNetWorthInclusion`, `DeleteAsset` | 12 |
| Empréstimos BTC / BTC Loans | `CreateBtcLoan`, `CreateBtcLending`, `RepayLoan` | 3 |
| Grupos de Ativos / Asset Groups | `GetAssetGroups`, `CreateAssetGroup`, `UpdateAssetGroup`, `DeleteAssetGroup`, `MoveAssetToGroup`, `RemoveAssetFromGroup` | 6 |
| Linha do Tempo do Estado do Empréstimo / Loan State Timeline | 4 loan-state tools **+ parameter tables** | 4 |
| Ativos Vendidos / Sold Assets | 3 sold-asset tools **+ parameter tables** | 3 |
| **Total** | | **28** |

### Pattern 2: Parameter tables under tool entries (D-03)

**What:** Small `| Parâmetro | Descrição |` tables for exactly the 7 required tools.
**Constraint:** Markdown tables cannot nest inside table rows — parameter tables must appear **below** the group's 2-column table, one per tool, labeled with the tool name in bold. (Alternative: drop the 2-column table for these two groups and give each tool a bold entry + description line + its parameter table. Primary recommendation is the labeled-tables-below approach because it keeps D-02's "each group keeps the 2-column style" literally true.)
**Edge case:** `ListSoldAssets` takes **no parameters** — document this with a one-line note ("Sem parâmetros." / "No parameters.") instead of an empty table; this still satisfies MCP-03 ("tools and their parameters").

### Pattern 3: Source-evidence HTML comments

**What:** `<!-- Source: <file> -->` comments above factual claims derived from code.
**Established by:** Phases 32–34 (CONTEXT); `ativos.md` carries 12, `mcp-server.md` currently has 0 `[VERIFIED: grep]`.
**Apply to:** One `<!-- Source: src/Valt.Infra/Mcp/Tools/AssetTools.cs -->` above the AssetTools section content and one `<!-- Source: src/Valt.Infra/Mcp/Tools/IndicatorTools.cs -->` above the IndicatorTools table, in both language files.

### Pattern 4: Bilingual mirror discipline (QA-01)

**What:** Identical heading structure, table row counts, and content in both files; only prose is translated. Tool names and parameter names stay in English inside backticks in both languages (code identifiers, per Phase 35/36 "exact app/code names as labels").
**Established by:** Phase 36 decision — mirroring must include *all* prose changes, not just structural additions.

### Pattern 5: Bookkeeping tail

**What:** After docs verified: REQUIREMENTS.md `- [ ]`→`- [x]` for MCP-01/02/03 + traceability `Pending`→`Complete` `[VERIFIED: REQUIREMENTS.md:42-44, 96-98]`; ROADMAP.md phase checkbox → `- [x] ... (completed 2026-07-16)` matching Phases 32–36 `[VERIFIED: ROADMAP.md:21-26]`.

### Anti-Patterns to Avoid

- **Inventing parameter descriptions:** D-05 requires copying from `[Description(...)]` attributes; paraphrase only for translation, never for semantics.
- **Retrofitting pre-existing categories:** GoalTools/AvgPriceTools/ReportTools/AccountTools/TransactionTools docs have name drift vs code (e.g., docs list `CreateDCAGoal`, code has `CreateDcaGoal`; docs list `AddBitcoinToBitcoinTransfer`, code has `DeleteTransaction`) — **explicitly out of scope** (D-04). Do not "fix while nearby"; log for Phase 38 QA instead.
- **Flat 28-row table:** violates D-02 grouping.
- **Editing only the Portuguese page:** violates QA-01; Phase 36 had a dedicated mirror plan (36-02) for this reason.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tool name list | Re-typing 29 names from memory | The verified inventory in §Code Examples (grep-extracted) | A single typo breaks MCP-01/02/03 accuracy and can't be caught by the build |
| Parameter truth | Paraphrasing behavior | `[Description]` attributes copied from `AssetTools.cs` (D-05) | Invented semantics = docs lying to AI agents consuming the page |
| Build validation | Eyeballing markdown | `mkdocs build --strict` in `.venv` | Catches broken tables/admonitions/links mechanically |
| Bilingual content | Writing English independently | Mirror the finalized PT structure, translate prose only | QA-01 parity; structure drift is invisible to the build |

**Key insight:** For docs-accuracy phases, the code *is* the spec. Every tool name, parameter, and behavior claim below was grep-verified against source in this session; the plan should treat §Code Examples as copy-paste-grade material, not inspiration.

## Common Pitfalls

### Pitfall 1: Wrong mkdocs environment (build gate silently unrunnable)
**What goes wrong:** `mkdocs build --strict` fails with `Unrecognised theme name: 'material'` (pipx mkdocs 1.6.1 on PATH) or `The "i18n" plugin is not installed` (system python 3.12, mkdocs 1.5.3).
**Why it happens:** Three mkdocs installs coexist; only `valt-docs/.venv` has material 9.7.1 + static-i18n 1.3.0.
**How to avoid:** Always `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict`. `[VERIFIED by running all three in this session; baseline .venv build exits 0, INFO-only]`
**Warning signs:** Config-value ERROR lines before any markdown processing.

### Pitfall 2: CONTEXT's tool count is wrong (26 vs 28)
**What goes wrong:** Plan built around 26 tools omits `GetAsset` (and miscounts subsections).
**Why it happens:** CONTEXT's grouping list skipped `GetAsset` (single asset by ID) and the "26" figure doesn't match its own list (27).
**How to avoid:** Use the 28-tool table in this research — verified by `grep -c 'McpServerTool, Description'` (= 28) and full method-name extraction.
**Warning signs:** Any plan task saying "26 tools".

### Pitfall 3: Parameter tables nested into table rows
**What goes wrong:** Attempting to embed a parameter table inside a `| Ferramenta | Descrição |` row breaks the table (markdown can't nest block tables in rows), producing garbled rendering that `--strict` may not even flag.
**How to avoid:** Pattern 2 — labeled parameter tables *below* the group table.
**Warning signs:** Raw `|` characters rendered as text on the built page.

### Pitfall 4: Stale "mais de 45 ferramentas" claim
**What goes wrong:** Page intro (line 3) and tools-section intro (line 133) claim "over 45 tools" while the page lists 81 — an internal contradiction against the phase goal ("lists the complete current toolset").
**Why it happens:** Claim predates AssetTools/IndicatorTools.
**How to avoid:** Update both occurrences (and both EN mirrors) to "mais de 80 ferramentas" / "over 80 tools". True either way: 81 documented, 90 actual `[VERIFIED: per-category grep counts — 7+8+4+6+13+12+7+4+1+28=90; documented 52+28+1=81]`.
**Warning signs:** A reader counting table rows gets a number 80% larger than the intro claim.

### Pitfall 5: Bilingual drift via "small" unmirrored edits
**What goes wrong:** PT page gets a tweak the EN page doesn't (or vice versa) — Phase 36 explicitly hit this and needed a mirror wave.
**How to avoid:** One plan per language (Phase 36 pattern: 36-01 PT, 36-02 EN), with the EN plan checklist including a heading/row-count parity grep.
**Warning signs:** `grep -c '^###' ` differs between the two files after the edit.

### Pitfall 6: New `.planning/` files silently untracked
**What goes wrong:** Research/plan/summary files don't get committed — `.planning/` is gitignored; only the 78 already-tracked files update normally.
**How to avoid:** `git add -f` for new planning files (CONTEXT §Integration points; verified `.gitignore:43`).
**Warning signs:** `git status` shows no new planning files after writing them.

### Pitfall 7: Wrong repo / wrong working directory
**What goes wrong:** Edits or build commands run in `valt` instead of `valt-docs` (the sibling repo), or doc commits land in the wrong repository.
**How to avoid:** Plan tasks must spell out absolute/sibling-relative paths (`../valt-docs/docs/funcionalidades/mcp-server.md`); doc commits use `docs(37-XX):` prefix in valt-docs; planning commits use `docs(37):` in valt.

## Code Examples

All tool names, parameter names, optionality, and English semantics below were extracted in this session from `AssetTools.cs` / `IndicatorTools.cs` `[VERIFIED]`. Portuguese wording follows the agent's-discretion allowance and app terminology (`Ativos`, `Empréstimo BTC`, `snapshot`, `Histórico de Ativos Vendidos` — verified against `ativos.md` and `language.pt-BR.resx`).

### Example 1: AssetTools section skeleton (Portuguese)

```markdown
### Ativos (AssetTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/AssetTools.cs -->
Ferramentas para gerenciar **ativos** — investimentos externos rastreados separadamente das contas (ações, ETFs, criptomoedas, imóveis, posições alavancadas, empréstimos BTC e mais).

#### Operações de Ativos

| Ferramenta | Descrição |
|------------|-----------|
| `GetAssets` | Lista todos os ativos rastreados |
| `GetVisibleAssets` | Lista apenas os ativos visíveis |
| `GetAsset` | Obtém um ativo pelo seu ID |
| `GetAssetsSummary` | Resumo dos ativos com totais na moeda principal e em sats, incluindo ativos vs passivos |
| `CreateBasicAsset` | Cria um ativo básico (ação, ETF, cripto, commodity ou personalizado) |
| `CreateRealEstateAsset` | Cria um ativo imobiliário |
| `CreateLeveragedPosition` | Cria uma posição alavancada (futuros, perpétuos, margem) |
| `UpdateAssetPrice` | Atualiza o preço atual de um ativo |
| `UpdateAssetQuantity` | Atualiza a quantidade de um ativo básico |
| `ToggleAssetVisibility` | Alterna a visibilidade de um ativo |
| `ToggleAssetNetWorthInclusion` | Alterna a inclusão do ativo no cálculo de patrimônio líquido |
| `DeleteAsset` | Remove um ativo |

#### Empréstimos BTC

| Ferramenta | Descrição |
|------------|-----------|
| `CreateBtcLoan` | Cria um empréstimo com garantia em BTC, com LTV, saúde e juros; suporta APR diário ou dívida total fixa (estilo HodlHodl) |
| `CreateBtcLending` | Cria uma posição de empréstimo BTC/fiat (credor), com acompanhamento de juros |
| `RepayLoan` | Marca um empréstimo BTC ou posição de credor como quitado |

#### Grupos de Ativos

| Ferramenta | Descrição |
|------------|-----------|
| `GetAssetGroups` | Lista todos os grupos de ativos |
| `CreateAssetGroup` | Cria um grupo de ativos |
| `UpdateAssetGroup` | Atualiza o nome e a descrição de um grupo |
| `DeleteAssetGroup` | Remove um grupo; os ativos do grupo ficam sem grupo |
| `MoveAssetToGroup` | Move um ativo para um grupo |
| `RemoveAssetFromGroup` | Remove um ativo do seu grupo |
```

### Example 2: Loan-state subsection WITH parameter tables (Portuguese)

```markdown
#### Linha do Tempo do Estado do Empréstimo

Cada empréstimo com garantia em BTC mantém uma linha do tempo de **snapshots** de estado; os cálculos atuais sempre usam o snapshot mais recente.

| Ferramenta | Descrição |
|------------|-----------|
| `AddLoanStateUpdate` | Adiciona um novo snapshot de estado a um empréstimo com garantia em BTC |
| `DeleteLoanStateUpdate` | Remove um snapshot de estado pela data efetiva |
| `GetLoanStateTimeline` | Retorna a linha do tempo cronológica completa de snapshots do empréstimo |
| `GetLatestLoanState` | Retorna o estado mais recente registrado do empréstimo |

**Parâmetros de `AddLoanStateUpdate`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo |
| `effectiveDate` | Data efetiva (yyyy-MM-dd) |
| `totalBorrowed` | Principal tomado ainda devido na data do snapshot |
| `interestAccruedUntilDate` | Juros acumulados até a data do snapshot |
| `collateralSats` | Colateral em BTC, em satoshis |
| `apr` | APR como decimal (ex.: 0,12 para 12%) |
| `fees` | Taxas pagas |
| `note` | Observação (opcional) |

**Parâmetros de `DeleteLoanStateUpdate`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo |
| `effectiveDate` | Data efetiva do snapshot a remover (yyyy-MM-dd) |

**Parâmetros de `GetLoanStateTimeline` e `GetLatestLoanState`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo |
```

*(Parameter source: `AssetTools.cs:626-725` — all params required except `note` = optional.)*

### Example 3: Sold-assets subsection WITH parameter tables (Portuguese)

```markdown
#### Ativos Vendidos

Marcar um ativo como vendido oculta o ativo da lista ativa e o move para o histórico, preservando o estado de visibilidade anterior para eventual restauração.

| Ferramenta | Descrição |
|------------|-----------|
| `MarkAssetAsSold` | Marca um ativo como vendido na data informada (padrão: hoje) |
| `UndoAssetSale` | Reverte uma venda e restaura o ativo à lista ativa |
| `ListSoldAssets` | Lista todos os ativos marcados como vendidos |

**Parâmetros de `MarkAssetAsSold`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo a marcar como vendido |
| `dateSold` | Data da venda em yyyy-MM-dd (opcional, padrão: hoje) |

**Parâmetros de `UndoAssetSale`:**

| Parâmetro | Descrição |
|-----------|-----------|
| `assetId` | O ID do ativo vendido a restaurar |

`ListSoldAssets` não recebe parâmetros.
```

*(Parameter source: `AssetTools.cs:727-794` — `dateSold` optional with `""` default → today; note the tool validates yyyy-MM-dd format.)*

### Example 4: IndicatorTools section (Portuguese + English)

```markdown
### Indicadores (IndicatorTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/IndicatorTools.cs -->
Indicadores macro do Bitcoin, atualizados em segundo plano e servidos a partir de cache.

| Ferramenta | Descrição |
|------------|-----------|
| `GetBitcoinIndicators` | Retorna os indicadores macro atuais do Bitcoin: Mayer Multiple, Rainbow Chart, Fear & Greed Index e dominância do BTC |
```

```markdown
### Indicators (IndicatorTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/IndicatorTools.cs -->
Bitcoin macro indicators, refreshed in the background and served from cache.

| Tool | Description |
|------|-------------|
| `GetBitcoinIndicators` | Gets current Bitcoin macro indicators: Mayer Multiple, Rainbow Chart, Fear & Greed Index, and Bitcoin Dominance |
```

*(Source: `IndicatorTools.cs:10-11`; cache populated by `IndicatorsUpdaterJob` `[VERIFIED: Crawlers/Indicators/IndicatorsUpdaterJob.cs]`; same four indicators shown in the Reports UI panel — EN labels "Mayer Multiple", "Rainbow Chart", "Fear & Greed", "BTC Dominance" `[VERIFIED: language.resx Reports.Indicators.*]`.)*

### Example 5: English mirror conventions

- Section headings: `### Assets (AssetTools)`, H4s: `#### Asset Operations` / `#### BTC Loans` / `#### Asset Groups` / `#### Loan State Timeline` / `#### Sold Assets`.
- Table headers: `| Tool | Description |` and `| Parameter | Description |`.
- Tool/parameter names stay in English backticked identifiers in **both** languages.
- EN descriptions mirror the code `[Description]` text closely, e.g. `CreateBtcLoan` → "Create a BTC-collateralized loan (borrowing fiat against BTC collateral); tracks LTV, health status, and accrued interest; supports daily APR or a fixed total debt (HodlHodl-style)."

### Example 6: Intro count update (both pages, 2 spots each)

```markdown
# PT line 3:   "...Com mais de 80 ferramentas disponíveis, você pode..."
# PT line 133: "O servidor MCP do Valt expõe mais de 80 ferramentas organizadas por categoria."
# EN line 3:   "...With over 80 tools available, you can..."
# EN line 133: "Valt's MCP server exposes over 80 tools organized by category."
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Inline factual claims without provenance | `<!-- Source: ... -->` evidence comments above code-derived claims | Phases 32–34 | New MCP sections should adopt the convention (page currently has none) |
| Single-language docs edits, mirror later | PT + EN mirrored within the same phase (dedicated mirror plan) | Phase 36 | QA-01 compliance without follow-up debt |

**Deprecated/outdated:**
- Bare `mkdocs` invocation on this machine — only `.venv`-activated mkdocs works (see Pitfall 1).
- The "45+ tools" claim — superseded by 90 actual / 81 documented tools (see Pitfall 4).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | *(none)* | — | All factual claims in this research were verified in-session against local authoritative sources (code, language files, docs files, build runs). No external/web sources were needed or used. |

**No user confirmation needed for factual content.** The only decision points are the agent's-discretion items already delegated by CONTEXT (placement, subsection titles, PT wording) plus the two recommendations in Open Questions.

## Open Questions

1. **Update the "mais de 45 ferramentas" claim to "mais de 80"?**
   - What we know: Claim appears 2× per page (intro line 3, tools-section line 133) in both languages; post-phase documented count is 81, actual code count is 90 `[VERIFIED]`.
   - What's unclear: Whether the planner considers this in-scope. It is a 4-line factual fix aligned with the phase goal ("lists the complete current toolset") and the milestone's content-accuracy theme.
   - Recommendation: Include it — leaving it makes the page self-contradictory (success criterion 1 fails in spirit if the intro undercounts by 45%).

2. **Add an `Ativos` link to "Próximos Passos / Next Steps"?**
   - What we know: The section currently links Transações, Importar e Exportar, Relatórios, Preço Médio; `ativos.md` exists and documents the same features AssetTools manipulates.
   - What's unclear: CONTEXT doesn't mention it; it's a 1-line-per-language addition.
   - Recommendation: Optional, low-risk, improves cross-navigation. Planner may include or defer without affecting MCP-01/02/03.

3. **Pre-existing category drift (out of scope — log for Phase 38)**
   - What we know: Documented names drifted from code in GoalTools (8 documented vs 13 actual, e.g. `CreateDCAGoal`→`CreateDcaGoal`), AvgPriceTools (11 vs 12, e.g. `GetAvgPriceProfiles`→`GetProfile`), ReportTools (6 vs 7, e.g. `GetWealthHistory`→`GetAllTimeHigh`/`GetMaxBtcStack`/`GetStatistics`), AccountTools (`CreateAccount` vs `CreateFiatAccount`/`CreateBtcAccount`), TransactionTools (docs list `AddBitcoinToBitcoinTransfer` — not in code; code has `DeleteTransaction`) `[VERIFIED: grep extraction]`.
   - What's unclear: Nothing for this phase — D-04 forbids touching these.
   - Recommendation: Do NOT fix in Phase 37. Note in the plan's deferred/out-of-scope section so Phase 38 (QA) can decide.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `valt-docs/.venv` (mkdocs + material + i18n) | Final build gate | ✓ | mkdocs 1.6.1, material 9.7.1, static-i18n 1.3.0 | — (only working env) |
| Python 3 | venv runtime | ✓ | 3.12.3 | — |
| git (valt-docs, `master`, clean tree) | Doc commits | ✓ | — | — |
| git (valt, `.planning/` partially ignored) | Planning commits | ✓ | — | `git add -f` for new files |
| `dotnet` SDK | — | not needed | — | No app build required (docs-only phase) |
| ~~pipx `mkdocs`~~ | — | ✗ broken (no material theme) | 1.6.1 | Use `.venv` |
| ~~system `python3 -m mkdocs`~~ | — | ✗ broken (no i18n plugin) | 1.5.3 | Use `.venv` |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none (the two broken mkdocs installs have the `.venv` fallback, which is the primary).

## Validation Architecture

*(Included: `workflow.nyquist_validation` is `true` in `.planning/config.json`.)*

### Test Framework

| Property | Value |
|----------|-------|
| Framework | MkDocs strict build + grep-based content checks (docs phase — no unit test framework applies) |
| Config file | `../valt-docs/mkdocs.yml` (existing; no changes expected — NAV-01 belongs to Phase 38) |
| Quick run command | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` |
| Full suite command | Same command (the build IS the full suite) + content greps below |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| MCP-01 | AssetTools category + 28 tools visible on page | build + content grep | `grep -c 'AssetTools' mcp-server.md mcp-server.en.md` (≥1 each); loop over 28 tool names: `for t in GetAssets GetVisibleAssets GetAsset GetAssetsSummary CreateBasicAsset CreateRealEstateAsset CreateLeveragedPosition UpdateAssetPrice UpdateAssetQuantity ToggleAssetVisibility ToggleAssetNetWorthInclusion DeleteAsset CreateBtcLoan CreateBtcLending RepayLoan GetAssetGroups CreateAssetGroup UpdateAssetGroup DeleteAssetGroup MoveAssetToGroup RemoveAssetFromGroup AddLoanStateUpdate DeleteLoanStateUpdate GetLoanStateTimeline GetLatestLoanState MarkAssetAsSold UndoAssetSale ListSoldAssets; do grep -l "\`$t\`" mcp-server.md mcp-server.en.md; done | ✅ (pages exist; content added by phase) |
| MCP-02 | 4 loan-state tools + parameters documented | content grep | `grep -E 'AddLoanStateUpdate|DeleteLoanStateUpdate|GetLoanStateTimeline|GetLatestLoanState' mcp-server.md`; `grep -E 'totalBorrowed|interestAccruedUntilDate|collateralSats|effectiveDate' mcp-server.md` (params present, both languages) | ✅ |
| MCP-03 | 3 sold-asset tools + parameters documented | content grep | `grep -E 'MarkAssetAsSold|UndoAssetSale|ListSoldAssets' mcp-server.md`; `grep 'dateSold' mcp-server.md` (both languages) | ✅ |
| QA-01 | Bilingual parity | structure diff | `diff <(grep -c '^#' mcp-server.md) <(grep -c '^#' mcp-server.en.md)`; compare `grep -c '^|' ` counts per file | ✅ |
| QA-02 | Site builds strict | build gate | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` → exit 0 | ✅ |
| Goal-aligned | IndicatorTools documented | content grep | `grep -E 'IndicatorTools|GetBitcoinIndicators' mcp-server.md mcp-server.en.md` | ✅ |

### Sampling Rate
- **Per task commit:** content greps for the section just edited
- **Per wave merge:** `mkdocs build --strict` + parity checks
- **Phase gate:** Build green + all requirement greps pass + REQUIREMENTS.md updated, before `/gsd-verify-work`

### Wave 0 Gaps
- None — documentation phase validated via the mkdocs build gate and content greps; no test framework, fixtures, or config to create. Baseline build verified green in this session.

## Security Domain

*(Included: `security_enforcement` not explicitly `false` in config.)*

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — (docs-only phase; no app surface changes) |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | no | — (parameters are *documented*, not parsed; validation already lives in the tools, e.g. `AddLoanStateUpdate` rejects non-`yyyy-MM-dd` dates — docs simply mirror that contract) |
| V6 Cryptography | no | — |

### Known Threat Patterns for this phase

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Docs leaking sensitive paths/secrets | Information Disclosure | No secrets exist in tool signatures; all documented material is already public in the open-source repo |
| Docs encouraging unsafe MCP exposure | — | Preserve the existing `!!! warning "Segurança"` localhost-only admonition untouched (page lines 41-42 / EN 41-42); do not add tunneling instructions |
| Docs/code drift misleading AI agents | Tampering (integrity of guidance) | D-05 verbatim-from-code rule + source-evidence comments + build gate |

## Sources

### Primary (HIGH confidence)
- `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — full file read (795 lines); 28 tools confirmed via attribute count + method extraction; 7 required tools' signatures and `[Description]` attributes copied verbatim (lines 626-794)
- `src/Valt.Infra/Mcp/Tools/IndicatorTools.cs` — full file read; `GetBitcoinIndicators` description + cached-data behavior
- `src/Valt.Infra/Mcp/Server/McpServerService.cs:131` — `.WithToolsFromAssembly(...)` confirms all tool classes live
- `valt-docs/docs/funcionalidades/mcp-server.md` + `.en.md` — full reads; current 8-category structure, 2-column tables, stale "45+" claims at lines 3/133
- `valt-docs/mkdocs.yml` — i18n suffix config; no nav changes needed
- Live environment probes — `.venv` build green; pipx/system mkdocs failures reproduced
- `.claude/docs/assets.md` — loan-state timeline and sold-state behavior prose (lines 156-295)
- `language.resx` / `language.pt-BR.resx` — UI terminology (Update Loan State / Atualizar Estado do Empréstimo; Sold Asset History / Histórico de Ativos Vendidos; Indicators panel labels)
- `valt-docs/docs/funcionalidades/ativos.md` — established pt-BR terminology and `<!-- Source: -->` convention (12 comments)
- Per-category tool counts via `grep -c 'McpServerTool, Description'` on all 10 tool files (Account 7, Transaction 8, Category 4, FixedExpense 6, Goal 13, AvgPrice 12, Report 7, Currency 4, Indicator 1, Asset 28 → 90 total)

### Secondary (MEDIUM confidence)
- Phase 36 RESEARCH/PATTERNS/VERIFICATION/SUMMARY files — established plan-split pattern (PT then EN mirror), exact build command with `.venv` activation, commit message conventions

### Tertiary (LOW confidence)
- None — no web sources used; no external research providers configured in this environment (`exa_search: false`, `brave_search: false`, `firecrawl: false`), and none were needed since every source of truth is local and authoritative.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified by executing the tools in-session; baseline strict build passed
- Architecture: HIGH — page structure, insertion points, and grouping verified by full file reads; tool inventory verified by two independent greps
- Pitfalls: HIGH — every pitfall reproduced (mkdocs env failures), counted (tool inventory, "45+" claim), or traced to a logged project decision (`.gitignore`, D-04 scope)

**Research date:** 2026-07-16
**Valid until:** 2026-08-15 (30 days — stable docs domain; invalidate sooner if `AssetTools.cs` gains/loses tools)
