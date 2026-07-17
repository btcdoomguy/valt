# Phase 38: Navigation, New Pages, and Quality Assurance - Research

**Researched:** 2026-07-16
**Domain:** MkDocs documentation / site navigation, new Settings page, MCP drift remediation, cross-page QA (bilingual pt-BR + en-US)
**Confidence:** HIGH

**Note:** No `38-CONTEXT.md` exists (discuss-phase not run). Research is therefore unconstrained by locked decisions; recommendations below are prescriptive for the planner and should be confirmed with the user in discuss-phase where flagged.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **NAV-01** | `mkdocs.yml` navigation updated to include new top-level pages or sections. | Full nav audit complete: all 16 existing pages are in nav (no orphans). Adding the Settings page requires +1 nav entry (`mkdocs.yml:88-91` Guide block). **Verified empirically: `mkdocs build --strict` does NOT fail on orphan pages** (INFO-level log, exit 0) — NAV-01 needs an explicit file-vs-nav audit step, not just the build gate. |
| **NAV-02** | New/renamed pages have consistent titles in both PT and EN nav translations. | Dual `nav_translations` pattern decoded (`mkdocs.yml:19-37` pt block, `42-60` en block). Rule: add `EN Title: PT Title` to the pt block and `PT Title: EN Title` to the en block. Build log confirms mechanism ("Translated 18 navigation elements to 'pt'"). |
| **NAV-03** | Settings & Configuration page added, or explicitly deferred. | **Recommendation: CREATE it.** Complete settings inventory extracted from `SettingsView.axaml` + resx files (3 tabs, 10+ settings, all labels verified verbatim). Full page outline + file names in §Architecture Patterns. Asset Groups page: **recommend explicit deferral** — already documented inline at `ativos.md:219-228`. |
| **QA-01** | Every PT content change mirrored in the EN `.en.md` file. | Baseline parity sweep run this session: all 16 page pairs have identical heading counts and table-row counts. Parity grep scripts provided. **Verified: missing `.en.md` does NOT fail the build** (silent fallback) — parity needs scripted checks. |
| **QA-02** | Site builds with `mkdocs build` without errors or broken internal links. | Baseline `mkdocs build --strict` green (exit 0, INFO-only). **Verified: broken internal links DO abort the strict build** ("Aborted with 1 warnings in strict mode!"). Only working env is `valt-docs/.venv`. |
| **QA-03** | Content review checklist applied to all updated pages (accuracy, completeness, tone). | Concrete 12-check checklist defined (A1–A5 accuracy, C1–C4 completeness, T1–T3 tone) with per-page artifact format (`38-QA-CHECKLIST.md`). Page scope: 8 v0.6-updated pages ×2 languages + new Settings page ×2 = 18 files. |
</phase_requirements>

## Project Constraints (from AGENTS.md)

- **No app code changes:** v0.6 updates only the public docs site (`valt-docs`); no `src/` edits in the `valt` repo (REQUIREMENTS.md §Out of Scope).
- **Terminology mirrors app language files:** STATE.md decision — public docs terminology mirrors `language.resx` / `language.pt-BR.resx`. All Settings labels in this research were extracted verbatim from those files.
- **Localization rule (docs analog):** AGENTS.md requires all language files updated together; the docs equivalent is QA-01 — every pt-BR change mirrored in the `.en.md` file in the same phase.
- **Cross-repo commits:** Doc commits in `../valt-docs` (prefix `docs(38-XX):`, matching `docs(37-XX):` history); planning artifacts in `valt` under `.planning/` — new files require `git add -f` (`.planning/` is gitignored) `[VERIFIED: phase 37 research + .gitignore]`.
- **Languages:** Only pt-BR and en-US in docs (REQUIREMENTS §Out of Scope) — even though the app ships 3 resx files (en, pt-BR, es `[VERIFIED: src/Valt.UI/Lang/]`).

## Summary

Phase 38 closes milestone v0.6 with four workstreams: (1) create a **Settings & Configuration page** — the app exposes a rich, currently-undocumented settings surface (3 tabs, 10+ settings, all verified from code/resx this session), and three existing pages already reference settings screens without a canonical link target; (2) **update `mkdocs.yml` nav + dual nav_translations** for the new page (no orphans exist today — 16/16 pages are in nav); (3) **fix the carried-forward MCP tool-name drift** — enumerated exhaustively below: **7 of the 8 pre-existing categories contain drift** (only CategoryTools is clean), with 28 phantom documented names and 32 real tools missing from the docs; and (4) **run the milestone QA sweep** — parity greps, strict build, and a recorded content-review checklist artifact.

Two scope-expanding findings versus the Phase 37 hand-off: the drift list Phase 37 logged (GoalTools, AvgPriceTools, ReportTools, AccountTools, TransactionTools) is incomplete — **FixedExpenseTools and CurrencyTools also drifted** (`CreateFixedExpense`, `GetFixedExpenseHistory`, `GetSupportedCurrencies`, `GetExchangeRate` are all phantom). After the drift fix, the documented tool count becomes exactly 90 = the code truth, so the page's "mais de 80 ferramentas" claim stays true.

Build-gate behavior was verified empirically this session (critical for plan verification steps): **broken internal links abort `--strict`; orphan pages and missing `.en.md` files do NOT** — the latter two need scripted audits, which this research provides.

**Primary recommendation:** Three plans — Wave 1: (P01) Settings page PT+EN + nav/nav_translations update; (P02) MCP drift fix across 7 category tables PT+EN (disjoint files from P01, parallelizable); Wave 2: (P03) full QA sweep + `38-QA-CHECKLIST.md` artifact + REQUIREMENTS.md/ROADMAP.md bookkeeping + final strict build gate.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Settings page content | `valt-docs` (`docs/guia/configuracoes.md` + `.en.md`) | — | New page; Guide section is the onboarding flow that ends in app configuration |
| Nav + translations config | `valt-docs/mkdocs.yml` (nav `86-105`, pt block `19-37`, en block `42-60`) | — | Single config file owns all nav state |
| Settings truth (labels, defaults, behavior) | `valt` repo `SettingsView.axaml`, `SettingsViewModel.cs`, `DisplaySettings.cs`, `CurrencySettings.cs`, `LocalStorageService.cs`, `ThemeService.cs` | `language.resx` / `language.pt-BR.resx` (verbatim labels) | Code is the spec; resx is the label canon |
| MCP drift truth | `valt` repo `src/Valt.Infra/Mcp/Tools/*.cs` (method names + `[Description]` attributes) | — | D-05-style verbatim-from-code rule (phase 37 precedent) |
| MCP page edits | `valt-docs` `docs/funcionalidades/mcp-server.md` + `.en.md` (identical line numbers 135-225) | — | Drift tables live at the same lines in both languages |
| QA evidence | `.planning/phases/38-*/38-QA-CHECKLIST.md` (new artifact) | Phase `38-VERIFICATION.md` | Criterion 6 requires "applied" to mean something recorded |
| Bookkeeping | `.planning/REQUIREMENTS.md` (NAV-01/02/03 → Complete) + `.planning/ROADMAP.md` (phase checkbox) | — | Established phases 32–37 tail pattern |

## Standard Stack

### Core (already installed — nothing to install)

| Tool | Version | Purpose | Why Standard |
|------|---------|---------|--------------|
| MkDocs | 1.6.1 `[VERIFIED: phase 37 + build rerun this session]` | Static site build + strict-mode gate | Project's docs engine |
| mkdocs-material | 9.7.1 `[VERIFIED: phase 37 .venv pip list]` | Theme | Configured in `mkdocs.yml` |
| mkdocs-static-i18n | 1.3.0 `[VERIFIED: phase 37 .venv pip list]` | pt/en routing via `.en.md` suffix | `docs_structure: suffix` in `mkdocs.yml` |
| Python | 3.12.3 | venv runtime | — |

**Activation command (mandatory — only working env):**
```bash
cd /home/vmabellini/RiderProjects/valt-docs && . .venv/bin/activate && mkdocs build --strict
```
Baseline verified green this session (exit 0, INFO-only, "Documentation built in 0.73 seconds").

### Supporting

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `grep` / `diff` / `wc` | Parity checks, drift verification, nav audit | Every plan's verification steps |
| `git` (valt-docs, clean tree at `0bfe8ec`) `[VERIFIED: git status]` | Doc commits `docs(38-XX):` | After each plan |

## Package Legitimacy Audit

**Not applicable — this phase installs zero external packages.** All tooling already exists in `valt-docs/.venv`. No `pip install`, `npm install`, or `cargo add` steps may appear in any plan. If a plan proposes installing a package, it is out of scope and should be rejected at plan-check.

## Architecture Patterns

### System Architecture Diagram

```
valt repo (source of truth)                 valt-docs repo (public site)
─────────────────────────                   ────────────────────────────
SettingsView.axaml (3 tabs) ──┐
resx labels (EN/PT)           ├─► NEW guia/configuracoes.md + .en.md ──┐
ThemeService (13 themes)      │                                        │
                              │     mkdocs.yml:                        │
                              │       nav Guide block + Settings ──────┤
Mcp/Tools/*.cs (90 tools) ────┤       nav_translations ±1 each block   │
  7 drifted categories        ├─► mcp-server.md/.en.md tables rewritten│
                              │     (lines 135-225, both files)        │
                              └────────────────────┬───────────────────┘
                                                   ▼
                              .venv: mkdocs build --strict → exit 0
                                                   ▼
                    QA sweep: parity greps (16→18 pairs), nav audit,
                    38-QA-CHECKLIST.md (18 files × 12 checks)
                                                   ▼
              REQUIREMENTS.md NAV-01/02/03 → Complete; ROADMAP.md ✓
```

### Current nav state (verified this session)

**No orphan pages.** All 16 doc pages are in `mkdocs.yml` nav `[VERIFIED: file listing vs nav, lines 86-105]`. The nav uses **English keys** pointing to PT-named files; `nav_translations` under the `pt` locale maps EN→PT display titles; the `en` locale block mirrors PT→EN (inert today since nav keys are already English, but it's the established dual-block convention — follow it for NAV-02). `FAQ` has no translation entries (identical in both languages — acceptable precedent).

### Pattern 1: New-page onboarding (NAV-01 + NAV-02 + NAV-03)

**Recommendation: CREATE the Settings page** (do not defer). Rationale:
1. Milestone scope explicitly considered it ("Consider new pages: Settings & Configuration, Asset Groups").
2. 10+ user-configurable settings across 3 tabs are completely undocumented — the biggest remaining docs gap.
3. Three existing pages reference settings screens with no link target: `mcp-server.md:24-27` ("Acesse **Configurações** > **Avançado**"), `faq.md:88` ("Na tela de configurações, avançado > Limpar o cache do saldo em conta"), `primeiros-passos.md:48` ("Acesse o menu de configurações") `[VERIFIED: grep, EN mirrors exist at equivalent lines]`.
4. Content is 100% locally verifiable (labels extracted verbatim from resx this session — below).
5. Cheap: 2 files + 1 nav entry + 2 translation lines.

**File names (follow PT-named convention):** `docs/guia/configuracoes.md` + `docs/guia/configuracoes.en.md`.
**Nav placement:** Guide section, after Basic Concepts (`mkdocs.yml:91`), key `Settings` → pt translation `Configurações` (matches app menu item `Main_Menu_Settings` = "Settings"/"Configurações" — app-aligned terminology per STATE.md decision).
**Nav translations to add:** pt block → `Settings: Configurações`; en block → `Configurações: Settings`.

**Verified settings inventory (source: `SettingsView.axaml`, `SettingsViewModel.cs`, `DisplaySettings.cs`, `CurrencySettings.cs`, `LocalStorageService.cs`, `ThemeService.cs:22-34`, resx files):**

| Tab (PT / EN) | Setting | PT label (verbatim) | EN label (verbatim) | Behavior |
|---------------|---------|--------------------|--------------------|----------|
| Geral / General | Main fiat currency | Moeda fiat principal | Main fiat currency | ComboBox of available currencies; default USD (`CurrencySettings.cs`) |
| Geral / General | Language & format | Idioma e formato | Language and display format | ComboBox lists ALL system cultures with pt-BR, es, en-US pinned top (`SettingsViewModel.cs:86-114`); app ships translations in 3 languages only; "(requer reinicialização)" / "(requires restart)" |
| Geral / General | Show hidden accounts | Exibir contas ocultas | Show hidden accounts | CheckBox; `DisplaySettings.ShowHiddenAccounts`, default false |
| Geral / General | Theme | Tema | Theme | 13 dark themes: Default, Ocean, Midnight Galaxy, Golden Hour, Arctic Frost, Forest Canopy, Crimson Ember, Monochrome, Rose Quartz, Sunset Blaze, Mocha Brew, Copper Forge, Pepe (`ThemeService.cs:22-34`) |
| Geral / General | Font size | Tamanho da Fonte | Font Size | Pequeno/Médio/Grande — Small/Medium/Large |
| Moedas / Currencies | Available fiat currencies | Moedas fiduciárias | Available currencies | Checkbox list; info: "USD está inclusa por padrão. Moedas em uso não podem ser removidas." / "USD is always included. Currencies in use cannot be removed."; adding triggers historical price download with confirmation dialog |
| Avançado / Advanced | MCP Server | Servidor MCP (Assistente IA) | MCP Server (AI Assistant) | CheckBox; persisted via `LocalStorageService`; off by default |
| Avançado / Advanced | MCP port | Porta do Servidor MCP | MCP Server Port | NumericUpDown 1024–65535, default 5200, clamped in `DisplaySettings` |
| Avançado / Advanced | Maintenance buttons | Limpar o cache de saldo de conta / Reprocessar o cache de nomes de transações / Alterar senha do banco de dados | Clear account totals cache / Clear transaction term cache / Change database password | 3 link buttons |

**Page outline (prescriptive):** Visão Geral (open via ☰ hamburger menu → Configurações/Settings; modal with OK/Cancel) → Geral (5 settings) → Moedas (currency list + download note) → Avançado (MCP enable/port + cross-link to `mcp-server.md`; 3 maintenance buttons incl. when to clear the account cache — cross-link from `faq.md:88`) → Arquivo de Dados (brief: `.valt` file is created at a user-chosen location — `CreateDatabaseViewModel.cs:132` "MyDatabase.valt"; password-protected LiteDB; backup = copy the file; cross-link `instalacao.md:85-87` which already covers this — do not duplicate) → Próximos Passos (links: primeiros-passos, faq, mcp-server).

**Asset Groups page: recommend explicit DEFERRAL.** Already documented inline: `ativos.md:219-228` §Grupos de Ativos (manage-groups modal, move/remove via right-click, visual-only semantics) with full EN mirror; the 6 group MCP tools are documented on the MCP page. A standalone page would duplicate content. Record the deferral in `38-VERIFICATION.md` and as a note in REQUIREMENTS.md traceability (satisfies "explicitly documented").

### Pattern 2: MCP drift remediation (7 of 8 categories)

**Full drift enumeration** — PT `mcp-server.md` and EN `mcp-server.en.md` have **identical line numbers** in these sections `[VERIFIED: grep both files]`. Code truth = method names extracted from `[McpServerTool]` attributes this session; every phantom name grep-confirmed absent from `src/Valt.Infra/Mcp/Tools/` (0 hits).

| Section (lines) | Line | Documented name | Verdict | Code truth |
|-----------------|------|-----------------|---------|------------|
| **Contas/Accounts** (135-144) | 139 | `GetAccounts` | ✅ | `GetAccounts` |
| | 140 | `CreateAccount` | ❌ phantom | Split: `CreateFiatAccount` + `CreateBtcAccount` |
| | 141 | `EditAccount` | ✅ | `EditAccount` |
| | 142 | `DeleteAccount` | ✅ | `DeleteAccount` |
| | 143 | `GetAccountBalance` | ❌ phantom | Doesn't exist (balances come via `GetAccounts`) |
| | 144 | `GetAccountHistory` | ❌ phantom | Doesn't exist |
| | — | *(missing)* | | `GetAccount`, `GetAccountGroups` |
| **Transações/Transactions** (146-157) | 150-155, 157 | 7 tools | ✅ | all correct |
| | 156 | `AddBitcoinToBitcoinTransfer` | ❌ phantom | Doesn't exist |
| | — | *(missing)* | | `DeleteTransaction` |
| **Categorias/Categories** (159-166) | 163-166 | 4 tools | ✅ | **NO DRIFT — only clean category** |
| **Despesas Fixas/Fixed Expenses** (168-176) | 172 | `GetFixedExpenses` | ✅ | `GetFixedExpenses` |
| | 173 | `CreateFixedExpense` | ❌ phantom | Split: `CreateMonthlyFixedExpense` + `CreateMonthlyVariableExpense` |
| | 174 | `EditFixedExpense` | ✅ | `EditFixedExpense` |
| | 175 | `DeleteFixedExpense` | ✅ | `DeleteFixedExpense` |
| | 176 | `GetFixedExpenseHistory` | ❌ phantom | Doesn't exist |
| | — | *(missing)* | | `GetFixedExpense` |
| **Metas/Goals** (178-189) | 182 | `GetGoals` | ✅ | `GetGoals` |
| | 183 | `CreateStackBitcoinGoal` | ✅ | `CreateStackBitcoinGoal` |
| | 184 | `CreateDCAGoal` | ❌ wrong case | `CreateDcaGoal` |
| | 185 | `CreateFiatIncomeGoal` | ❌ renamed | `CreateIncomeFiatGoal` |
| | 186 | `CreateBitcoinIncomeGoal` | ❌ renamed | `CreateIncomeBtcGoal` |
| | 187 | `CreateSpendingLimitGoal` | ✅ | `CreateSpendingLimitGoal` |
| | 188 | `CreateReduceCategoryGoal` | ❌ renamed | `CreateReduceExpenseCategoryGoal` |
| | 189 | `CreateHodlBitcoinGoal` | ❌ renamed | `CreateBitcoinHodlGoal` |
| | — | *(missing ×5)* | | `GetGoal`, `CreateSaveFiatGoal`, `CreateSavingsRateGoal`, `CreateNetWorthBtcGoal`, `DeleteGoal` |
| **Preço Médio/Average Price** (191-205) | 195 | `GetAvgPriceProfiles` | ❌ renamed | `GetProfiles` |
| | 196 | `CreateAvgPriceProfile` | ❌ phantom | Split: `CreateBrazilianRuleProfile` + `CreateFifoProfile` |
| | 197 | `EditAvgPriceProfile` | ❌ renamed | `EditProfile` |
| | 198 | `DeleteAvgPriceProfile` | ❌ renamed | `DeleteProfile` |
| | 199 | `GetAvgPriceLines` | ❌ renamed | `GetProfileLines` |
| | 200 | `AddAvgPriceBuyLine` | ❌ renamed | `AddBuyLine` |
| | 201 | `AddAvgPriceSellLine` | ❌ renamed | `AddSellLine` |
| | 202 | `AddAvgPriceTransferInLine` | ❌ phantom | Doesn't exist (closest real: `AddSetupLine`) |
| | 203 | `AddAvgPriceTransferOutLine` | ❌ phantom | Doesn't exist |
| | 204 | `EditAvgPriceLine` | ❌ renamed | `EditLine` |
| | 205 | `DeleteAvgPriceLine` | ❌ renamed | `DeleteLine` |
| | — | *(missing)* | | `GetProfile`, `AddSetupLine` — **0/11 documented names correct: worst category** |
| **Relatórios/Reports** (207-216) | 211 | `GetMonthlyTotals` | ✅ | `GetMonthlyTotals` |
| | 212 | `GetWealthOverview` | ✅ | `GetWealthOverview` |
| | 213 | `GetWealthHistory` | ❌ phantom | Doesn't exist |
| | 214 | `GetCategoryStatistics` | ❌ phantom | Doesn't exist (closest: `GetExpensesByCategory`, `GetStatistics`) |
| | 215 | `GetIncomeVsExpenses` | ❌ phantom | Doesn't exist |
| | 216 | `GetTopExpenseCategories` | ❌ phantom | Doesn't exist |
| | — | *(missing ×5)* | | `GetExpensesByCategory`, `GetIncomeByCategory`, `GetAllTimeHigh`, `GetMaxBtcStack`, `GetStatistics` |
| **Moedas/Currencies** (218-225) | 222 | `GetSupportedCurrencies` | ❌ renamed | `GetAvailableCurrencies` |
| | 223 | `GetExchangeRate` | ❌ phantom | Doesn't exist |
| | 224 | `GetHistoricalPrice` | ✅ | `GetHistoricalPrice` |
| | 225 | `ConvertCurrency` | ✅ | `ConvertCurrency` |
| | — | *(missing)* | | `GetMainCurrency` |

**Totals:** 28 phantom documented names; 32 real tools undocumented; 24 correct names. After the fix, documented tools = 61 (pre-existing) + 28 (AssetTools) + 1 (IndicatorTools) = **90 = exact code truth** — the "mais de 80 ferramentas" intro claim stays TRUE, no count edit needed.

**Fix pattern:** rewrite each drifted category's table in place (same section, same 2-column style, both files), using the copy-paste-grade corrected tables in §Code Examples. Descriptions for renamed/phantom rows are also stale (e.g., "Adds an incoming transfer" describes a nonexistent tool) — rewrite descriptions from the code `[Description]` attributes (all extracted in §Sources, EN verbatim; PT translated in page tone). Add `<!-- Source: src/Valt.Infra/Mcp/Tools/<File>.cs -->` above each rewritten table per the phase 32–34 evidence-comment convention.

### Pattern 3: QA-03 "applied" — the concrete checklist

**"Applied" means:** the artifact `.planning/phases/38-navigation-new-pages-and-quality-assurance/38-QA-CHECKLIST.md` exists, contains one row per file (18 files) × 12 checks with pass/fail + evidence (grep output or "read"), and is committed. Scope: 8 v0.6-updated pages × 2 languages (`instalacao`, `faq`, `primeiros-passos`, `ativos`, `relatorios`, `metas`, `despesas-fixas`, `mcp-server`) + new `configuracoes` × 2.

| # | Dimension | Check | Method |
|---|-----------|-------|--------|
| A1 | Accuracy | Code identifiers on page exist in `src/` | grep each identifier (tools, commands, enums) |
| A2 | Accuracy | UI labels match `language.resx` (EN) / `language.pt-BR.resx` (PT) verbatim | spot-check labels added/changed in v0.6 |
| A3 | Accuracy | Counts/limits/defaults match code (90 tools, port 5200, day 1–28, 13 themes) | targeted greps |
| A4 | Accuracy | `<!-- Source: -->` claims still true against cited file | re-grep the cited fact |
| A5 | Accuracy | No stale claims remain from phase fix lists (no "SQLite", no phantom tools, no "45 ferramentas") | negative greps |
| C1 | Completeness | Requirement-mandated items present | re-run each phase's requirement greps (ACC/AST/RPT/GOAL/FXE/MCP) |
| C2 | Completeness | Heading parity PT/EN | `diff <(grep '^#' pt) <(grep '^#' en)` (structure, not text) |
| C3 | Completeness | Table-row parity | `grep -c '^|'` equal in both files |
| C4 | Completeness | All pages in nav; new nav key translated in both blocks | file-vs-nav audit script (below) |
| T1 | Tone | Second-person instructional voice; no dev notes/TODOs | read + `grep -in 'todo\|fixme\|xxx'` |
| T2 | Tone | Heading-emoji convention consistent with sibling pages | visual/grep heading lines |
| T3 | Tone | Admonition style (`!!! tip/warning/info` + quoted title) consistent | grep admonitions, compare siblings |

### Anti-Patterns to Avoid

- **Trusting the build gate for orphans/parity:** `--strict` catches broken links but NOT orphan pages (INFO log, exit 0 — verified this session) and NOT missing `.en.md` (silent PT fallback — verified). All three need scripted checks.
- **Fixing only the PT MCP tables:** 7 category tables exist at identical line numbers in both files; QA-01 requires both.
- **Renaming tools without rewriting descriptions:** stale descriptions ("Adds an incoming transfer") lie about nonexistent tools.
- **Claiming the culture combo has 3 options:** it lists ALL system cultures with pt-BR/es/en-US pinned top (`SettingsViewModel.cs:86-114`); the page must say the app is *translated* into 3 languages, not that the combo has 3 entries.
- **Duplicating backup/password content:** `instalacao.md:52-87` already covers `.valt` file + password + backup — cross-link, don't duplicate.
- **A standalone Asset Groups page:** duplicates `ativos.md:219-228`; defer explicitly instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Corrected MCP tables | Re-typing names/descriptions from memory | §Code Examples tables (grep-extracted this session) | One typo re-creates the drift being fixed |
| Nav coverage proof | Eyeballing mkdocs.yml | The file-vs-nav audit one-liner (§Code Examples 4) | Build doesn't catch orphans |
| Bilingual parity proof | Reading both files side by side | Heading/table-row count greps (§Code Examples 5) | Structure drift is invisible to the build |
| Settings labels | Paraphrasing the UI | Verbatim resx strings in the inventory table above | STATE.md: docs terminology mirrors app language files |

**Key insight:** same as phase 37 — for docs-accuracy phases, the code *is* the spec. Everything in §Code Examples is copy-paste-grade.

## Common Pitfalls

### Pitfall 1: Assuming `--strict` validates nav coverage
**What goes wrong:** New page created, forgotten in nav → build still green → NAV-01 silently unmet.
**Why it happens:** mkdocs logs "pages exist... but are not included in the nav configuration" at INFO level; strict only aborts on WARNING+ `[VERIFIED empirically this session: orphan test page, exit 0]`.
**How to avoid:** Plan must include the file-vs-nav audit script as a verification step.
**Warning signs:** Page reachable via search but absent from the nav tabs.

### Pitfall 2: Assuming the build enforces bilingual parity
**What goes wrong:** `.en.md` forgotten or partially mirrored → EN site silently serves PT fallback content; QA-01 violated with a green build `[VERIFIED empirically: PT-only page in nav, exit 0]`.
**How to avoid:** Heading-count diff + table-row count grep per pair, in the wave-2 gate.
**Warning signs:** `grep -c '^#'` differs between a pair.

### Pitfall 3: nav_translations edited in only one block
**What goes wrong:** New nav key shows translated title in one language, raw key in the other → NAV-02 fails.
**Why it happens:** Two separate blocks (`mkdocs.yml:19-37` pt, `42-60` en); easy to edit one.
**How to avoid:** One edit adds both lines: pt block `Settings: Configurações`, en block `Configurações: Settings`.
**Warning signs:** Build log "Translated 18 navigation elements to 'pt'" still says 18 after adding a page (should become 19).

### Pitfall 4: Wrong mkdocs environment (carried from phase 37)
**What goes wrong:** `mkdocs build --strict` fails — pipx mkdocs lacks material theme; system python lacks i18n plugin.
**How to avoid:** Always `cd /home/vmabellini/RiderProjects/valt-docs && . .venv/bin/activate && mkdocs build --strict` `[VERIFIED: phase 37; baseline rerun green this session]`.

### Pitfall 5: Settings page contradicting the app's actual behavior
**What goes wrong:** Docs say "choose from 3 languages" (false — combo lists all cultures), or "light theme" (false — all 13 themes are dark-base, `ThemeService.cs:22-34`), or imply the database lives at a fixed path (false — user picks the `.valt` location at creation).
**How to avoid:** Use the verified inventory table in Pattern 1; every row cites its source.
**Warning signs:** Any Settings claim without a code/resx citation.

### Pitfall 6: New `.planning/` files silently untracked
**What goes wrong:** Research/plan/checklist files not committed — `.planning/` is gitignored.
**How to avoid:** `git add -f` for new planning files (phase 37 precedent).

### Pitfall 7: Wrong repo / wrong working directory
**What goes wrong:** Edits or builds run in `valt` instead of `valt-docs`.
**How to avoid:** Plan tasks spell absolute paths; doc commits use `docs(38-XX):` prefix in valt-docs; planning commits in valt.

## Code Examples

### Example 1: Corrected MCP category tables (Portuguese — copy-paste-grade)

Tool names verified via `[McpServerTool]` attribute extraction; PT descriptions follow page tone from code `[Description]` semantics (all EN source descriptions in §Sources).

```markdown
### Contas (AccountTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/Budget/AccountTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetAccounts` | Lista todas as contas com seus saldos atuais |
| `GetAccount` | Obtém uma conta pelo seu ID |
| `GetAccountGroups` | Lista todos os grupos de contas |
| `CreateFiatAccount` | Cria uma conta em moeda fiat (ex.: conta bancária, cartão de crédito) |
| `CreateBtcAccount` | Cria uma conta Bitcoin (ex.: cold storage, wallet em exchange) |
| `EditAccount` | Edita as propriedades de uma conta existente |
| `DeleteAccount` | Remove uma conta (falha se ela tiver transações) |

### Transações (TransactionTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/Budget/TransactionTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetTransactions` | Lista transações com filtros opcionais por data, conta, categoria ou termo de busca |
| `AddFiatExpense` | Adiciona despesa em fiat |
| `AddFiatIncome` | Adiciona receita em fiat |
| `AddFiatToFiatTransfer` | Transferência entre contas fiat |
| `AddBitcoinExpense` | Adiciona despesa em Bitcoin |
| `AddBitcoinIncome` | Adiciona receita em Bitcoin |
| `AddBitcoinPurchase` | Compra de Bitcoin (fiat sai, sats entram) |
| `DeleteTransaction` | Remove uma transação |

### Despesas Fixas (FixedExpenseTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/Budget/FixedExpenseTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetFixedExpenses` | Lista todas as despesas fixas/recorrentes |
| `GetFixedExpense` | Obtém uma despesa fixa pelo seu ID |
| `CreateMonthlyFixedExpense` | Cria uma despesa fixa mensal com valor constante |
| `CreateMonthlyVariableExpense` | Cria uma despesa fixa mensal com faixa de valor variável (mínimo–máximo) |
| `EditFixedExpense` | Edita nome, categoria e status de uma despesa fixa |
| `DeleteFixedExpense` | Remove uma despesa fixa (as transações perdem a associação) |

### Metas (GoalTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/GoalTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetGoals` | Lista todas as metas, com filtro opcional por data |
| `GetGoal` | Obtém uma meta pelo seu ID |
| `CreateStackBitcoinGoal` | Meta de acumular um valor alvo em Bitcoin (sats) |
| `CreateSpendingLimitGoal` | Meta de limitar os gastos em fiat a um valor máximo |
| `CreateDcaGoal` | Meta de DCA: realizar um número alvo de compras de Bitcoin |
| `CreateIncomeFiatGoal` | Meta de alcançar uma renda alvo em fiat |
| `CreateIncomeBtcGoal` | Meta de alcançar uma renda alvo em Bitcoin (sats) |
| `CreateReduceExpenseCategoryGoal` | Meta de limitar gastos em uma categoria específica |
| `CreateBitcoinHodlGoal` | Meta de limitar vendas de Bitcoin (HODL) |
| `CreateSaveFiatGoal` | Meta de poupar um valor alvo em fiat (receitas menos despesas) |
| `CreateSavingsRateGoal` | Meta de poupar um percentual alvo da renda |
| `CreateNetWorthBtcGoal` | Meta de alcançar um patrimônio líquido alvo em bitcoin (sats) |
| `DeleteGoal` | Remove uma meta |

### Preço Médio (AvgPriceTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/AvgPriceTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetProfiles` | Lista todos os perfis de preço médio/custo de aquisição |
| `GetProfile` | Obtém um perfil pelo seu ID |
| `GetProfileLines` | Lista as linhas de compra/venda/setup de um perfil |
| `CreateBrazilianRuleProfile` | Cria um perfil com cálculo pela Regra Brasileira (média ponderada) |
| `CreateFifoProfile` | Cria um perfil com cálculo FIFO (First-In-First-Out) |
| `EditProfile` | Edita um perfil de preço médio |
| `DeleteProfile` | Remove um perfil e todas as suas linhas |
| `AddBuyLine` | Adiciona uma linha de compra (aquisição) a um perfil |
| `AddSellLine` | Adiciona uma linha de venda (alienação) a um perfil |
| `AddSetupLine` | Adiciona uma linha de setup (posição inicial) a um perfil |
| `EditLine` | Edita uma linha existente de um perfil |
| `DeleteLine` | Remove uma linha de um perfil |

### Relatórios (ReportTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/ReportTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetMonthlyTotals` | Totais mensais de receitas, despesas e transações em bitcoin em um período |
| `GetWealthOverview` | Visão geral do patrimônio em fiat e BTC por período (diário, semanal, mensal, anual) |
| `GetExpensesByCategory` | Despesas por categoria em um período |
| `GetIncomeByCategory` | Receitas por categoria em um período |
| `GetAllTimeHigh` | Máxima histórica do patrimônio, com data e declínio atual em percentual |
| `GetMaxBtcStack` | Maior stack de BTC já acumulado, com data e declínio desde o pico |
| `GetStatistics` | Estatísticas financeiras: mediana de despesas mensais e cobertura do patrimônio em meses |

### Moedas (CurrencyTools)

<!-- Source: src/Valt.Infra/Mcp/Tools/CurrencyTools.cs -->
| Ferramenta | Descrição |
|------------|-----------|
| `GetAvailableCurrencies` | Lista as moedas fiat disponíveis e as atualmente em uso |
| `GetMainCurrency` | Retorna a moeda fiat principal configurada no aplicativo |
| `ConvertCurrency` | Converte valores entre moedas (USD, BRL, BTC, SATS etc.), usando cotações ao vivo quando disponíveis |
| `GetHistoricalPrice` | Preço histórico do BTC (em USD) ou de moedas fiat (relativo ao USD) em uma data |
```

**EN mirrors:** same names; descriptions track the code `[Description]` text verbatim-ish (full extraction in §Sources — e.g., `CreateBrazilianRuleProfile` → "Create a new average price profile using Brazilian Rule calculation (weighted average)"). Table headers `| Tool | Description |`.

### Example 2: mkdocs.yml nav diff (NAV-01 + NAV-02)

```yaml
nav:
  - Home: index.md
  - Guide:
    - Installation: guia/instalacao.md
    - Getting Started: guia/primeiros-passos.md
    - Basic Concepts: guia/conceitos-basicos.md
    - Settings: guia/configuracoes.md        # NEW (mkdocs.yml:91 insertion point)
  # ...rest unchanged

# pt locale nav_translations — add (after line 24 area, keeping Guide-grouped order):
            Settings: Configurações
# en locale nav_translations — add:
            Configurações: Settings
```

### Example 3: Settings page skeleton (Portuguese)

```markdown
# Configurações ⚙️

O Valt é configurado pela janela **Configurações**, acessível pelo menu ☰ no canto
superior esquerdo da tela principal. As opções ficam organizadas em três abas:
**Geral**, **Moedas** e **Avançado**. Clique em **OK** para salvar.

## Geral

<!-- Source: src/Valt.UI/Views/Main/Modals/Settings/SettingsView.axaml + language.pt-BR.resx -->
| Opção | Descrição |
|-------|-----------|
| **Moeda fiat principal** | Moeda usada nos totais e relatórios (padrão: USD) |
| **Idioma e formato** | Idioma da interface e formato de datas/números; o Valt é traduzido em Português, English e Español *(requer reinicialização)* |
| **Exibir contas ocultas** | Mostra contas marcadas como ocultas nas listas |
| **Tema** | 13 temas escuros: Default, Ocean, Midnight Galaxy, Golden Hour, Arctic Frost, Forest Canopy, Crimson Ember, Monochrome, Rose Quartz, Sunset Blaze, Mocha Brew, Copper Forge e Pepe |
| **Tamanho da Fonte** | Pequeno, Médio ou Grande |

## Moedas

<!-- Source: SettingsView.axaml (Currencies tab) + Settings.FiatCurrencies.* resx -->
Marque as moedas fiat que você usa. **USD está inclusa por padrão** e moedas em uso
não podem ser removidas. Ao adicionar novas moedas, o Valt baixa o histórico de
preços delas (com confirmação).

## Avançado

### Servidor MCP (Assistente IA)

Ative o **Servidor MCP** e configure a **Porta** (1024–65535, padrão: 5200) para
conectar assistentes de IA. Detalhes completos em [Servidor MCP](../funcionalidades/mcp-server.md).

### Manutenção

| Botão | Quando usar |
|-------|-------------|
| **Limpar o cache de saldo de conta** | Se os saldos exibidos parecerem errados (veja o [FAQ](../referencia/faq.md)) |
| **Reprocessar o cache de nomes de transações** | Se a busca por transações não encontrar itens existentes |
| **Alterar senha do banco de dados** | Para trocar a senha do seu arquivo `.valt` |

## Seu Arquivo de Dados

Seus dados ficam em um arquivo `.valt` criptografado, cujo local você escolhe ao
criá-lo — veja [Instalação](../guia/instalacao.md). Faça backups copiando o arquivo
para um local seguro.

## Próximos Passos

- [Primeiros Passos](primeiros-passos.md) - Configure contas e categorias
- [FAQ](../referencia/faq.md) - Perguntas frequentes
- [Servidor MCP](../funcionalidades/mcp-server.md) - Conecte sua IA
```

*(EN mirror: `# Settings ⚙️`, same structure, labels from `language.resx` verbatim.)*

### Example 4: File-vs-nav audit (NAV-01 proof — build does NOT catch orphans)

```bash
cd /home/vmabellini/RiderProjects/valt-docs
comm -23 \
  <(find docs -name '*.md' ! -name '*.en.md' | sed 's|^docs/||' | sort) \
  <(grep -oE '[a-z-]+/[a-z-]+\.md|index\.md' mkdocs.yml | sort -u)
# Expected output: empty (every non-.en.md page appears in nav)
```

### Example 5: Bilingual parity sweep (QA-01 proof)

```bash
cd /home/vmabellini/RiderProjects/valt-docs/docs
for pt in $(find . -name '*.md' ! -name '*.en.md'); do
  en="${pt%.md}.en.md"
  [ -f "$en" ] || echo "MISSING MIRROR: $en"
  h1=$(grep -c '^#' "$pt"); h2=$(grep -c '^#' "$en")
  r1=$(grep -c '^|' "$pt"); r2=$(grep -c '^|' "$en")
  [ "$h1" = "$h2" ] && [ "$r1" = "$r2" ] || echo "PARITY FAIL: $pt (h:$h1/$h2 rows:$r1/$r2)"
done
# Baseline this session: all 16 pairs OK (only moedas.md differs in raw line
# count 137/134 — prose wrapping, not structure; use heading/row counts, not wc -l)
```

### Example 6: MCP drift regression check (post-fix)

```bash
cd /home/vmabellini/RiderProjects/valt-docs/docs/funcionalidades
# All 28 phantom names must be GONE from both pages:
for t in CreateAccount GetAccountBalance GetAccountHistory AddBitcoinToBitcoinTransfer \
         CreateFixedExpense GetFixedExpenseHistory CreateDCAGoal CreateFiatIncomeGoal \
         CreateBitcoinIncomeGoal CreateReduceCategoryGoal CreateHodlBitcoinGoal \
         GetAvgPriceProfiles CreateAvgPriceProfile EditAvgPriceProfile DeleteAvgPriceProfile \
         GetAvgPriceLines AddAvgPriceBuyLine AddAvgPriceSellLine AddAvgPriceTransferInLine \
         AddAvgPriceTransferOutLine EditAvgPriceLine DeleteAvgPriceLine GetWealthHistory \
         GetCategoryStatistics GetIncomeVsExpenses GetTopExpenseCategories \
         GetSupportedCurrencies GetExchangeRate; do
  grep -l "\`$t\`" mcp-server.md mcp-server.en.md 2>/dev/null && echo "STILL PRESENT: $t"
done
# All 61 real names of the 8 pre-existing categories must be PRESENT in both pages
# (full list in Pattern 2 tables above)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Assume `mkdocs build --strict` validates everything | Empirically split gates: build for links/syntax, scripts for orphans/parity/drift | This phase | Plan verification steps must include scripted audits, not just the build |
| Per-phase content fixes | Cross-page QA sweep with a recorded checklist artifact | This phase (QA-03) | `38-QA-CHECKLIST.md` becomes the milestone's quality evidence |
| Docs describe tools aspirationally | Verbatim-from-code names/descriptions + `<!-- Source: -->` comments | Phases 32–37 | Drift fix completes this transition for the MCP page |

**Deprecated/outdated:**
- The 28 phantom tool names (Pattern 2) — removed by this phase.
- "Docs will catch it in review" without a checklist — replaced by the A/C/T checklist (Pattern 3).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Settings page placement in Guide section (vs Reference) — recommendation, not locked | Pattern 1 | Low: cosmetic nav position; user can override in discuss-phase |
| A2 | Asset Groups page deferral is acceptable to the user (milestone said "consider") | Pattern 1 | Low: content already exists inline; deferral is explicitly documented per criterion 3's allowance |
| A3 | PT description wording in Example 1 (translated from code `[Description]` text) | Example 1 | Low: names are code-verified; wording is agent's-discretion per established pattern |

**Everything else** in this research was verified in-session against local authoritative sources (code, resx, docs files, live build runs). No external/web sources were needed.

## Open Questions

1. **Settings page: create or defer?** (Research recommends CREATE with the outline above.)
   - What we know: Full settings inventory verified; 3 existing pages would gain link targets; NAV-03 accepts either outcome if documented.
   - What's unclear: User's appetite for the extra page this milestone.
   - Recommendation: Discuss-phase confirm; default to create.

2. **Should the 3 existing settings references become cross-links?** (`mcp-server.md:24-27`, `faq.md:88`, `primeiros-passos.md:48`)
   - What we know: All three mention settings screens; linking is a 1-line-per-file change.
   - Recommendation: Include as optional task in the Settings-page plan (improves nav coherence, zero risk).

3. **MCP drift fix: full rewrite of the 7 tables vs. minimal renames?**
   - What we know: Minimal renames would keep stale descriptions and missing tools (32 real tools undocumented); full rewrite lands everything at 90/90.
   - Recommendation: Full table replacement per Example 1 — same effort class, complete accuracy, and it finishes the milestone's "complete current toolset" theme.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `valt-docs/.venv` (mkdocs + material + i18n) | Build gate | ✓ | mkdocs 1.6.1, material 9.7.1, static-i18n 1.3.0 | — (only working env; re-verified green this session) |
| Python 3 | venv runtime | ✓ | 3.12.3 | — |
| git (valt-docs, `master`, clean at `0bfe8ec`) | Doc commits | ✓ `[VERIFIED: git status]` | — | — |
| git (valt, `.planning/` partially ignored) | Planning commits | ✓ | — | `git add -f` for new files |
| `dotnet` SDK | — | not needed | — | No app build required (docs-only phase) |
| ~~pipx/system mkdocs~~ | — | ✗ broken | — | Use `.venv` (Pitfall 4) |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

## Validation Architecture

*(Included: `workflow.nyquist_validation` is `true` in `.planning/config.json`.)*

### Test Framework

| Property | Value |
|----------|-------|
| Framework | MkDocs strict build + grep/comm-based content checks (docs phase — no unit test framework applies) |
| Config file | `../valt-docs/mkdocs.yml` (edited by this phase: nav + nav_translations) |
| Quick run command | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` |
| Full suite command | Build + nav audit (Ex. 4) + parity sweep (Ex. 5) + drift regression (Ex. 6) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| NAV-01 | All pages in nav (incl. new Settings page) | script | Example 4 audit → empty output | ❌ created by phase (`guia/configuracoes.md`) |
| NAV-02 | New nav key translated in both blocks | grep | `grep -n 'Settings: Configurações' mkdocs.yml && grep -n 'Configurações: Settings' mkdocs.yml`; build log "Translated 19 navigation elements to 'pt'" | ✅ (mkdocs.yml) |
| NAV-03 | Settings page exists + reachable, or deferral recorded | grep + build | `test -f docs/guia/configuracoes.md && test -f docs/guia/configuracoes.en.md`; page in nav (Ex. 4) | ❌ created by phase |
| QA-01 | Bilingual parity across all pairs | script | Example 5 sweep → no FAIL/MISSING lines | ✅ |
| QA-02 | Strict build green, no broken links | build gate | `mkdocs build --strict` → exit 0 (broken links abort — verified) | ✅ |
| QA-03 | Checklist applied + recorded | artifact check | `test -f .planning/phases/38-*/38-QA-CHECKLIST.md`; all 18 file rows filled | ❌ created by phase |
| Drift (goal) | 28 phantom names gone, 61 real names present | script | Example 6 regression greps | ✅ (mcp-server pages) |

### Sampling Rate
- **Per task commit:** content greps for the section just edited
- **Per wave merge:** `mkdocs build --strict` + Example 4/5 scripts
- **Phase gate:** Build green + all requirement checks pass + `38-QA-CHECKLIST.md` complete + REQUIREMENTS.md NAV-01/02/03 → Complete, before `/gsd-verify-work`

### Wave 0 Gaps
- None — documentation phase validated via build gate + scripts; no test framework to create. Baseline build verified green this session.

## Security Domain

*(Included: `security_enforcement` not explicitly `false` in config.)*

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — (docs-only; the Settings page *describes* the database password feature but changes no auth surface) |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | no | — (port range 1024–65535 is documented, not parsed) |
| V6 Cryptography | no | — (docs mention LiteDB encryption; no crypto implemented or configured by docs) |

### Known Threat Patterns for this phase

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Docs weakening MCP security posture | Tampering (integrity of guidance) | Settings page must repeat the localhost-only caution and cross-link the MCP page's existing `!!! warning "Segurança"` admonition (`mcp-server.md:41-42`); no tunneling instructions |
| Docs leaking user paths/secrets | Information Disclosure | Describe the `.valt` file location as user-chosen; never print real paths or passwords; keep the "passwords are unrecoverable" note consistent with `instalacao.md:56-57` |
| Docs/code drift misleading AI agents | Tampering | Verbatim-from-code rule + source-evidence comments + drift regression greps (Ex. 6) |

## Sources

### Primary (HIGH confidence)
- `valt-docs/mkdocs.yml` — full read: nav (86-105), dual nav_translations blocks (19-37 pt, 42-60 en), suffix i18n config
- `valt-docs/docs/` — full file listing (16 PT + 16 EN); nav coverage audit: no orphans
- `valt-docs/docs/funcionalidades/mcp-server.md` + `.en.md` — full reads; drift tables at identical lines 135-225 in both
- `src/Valt.Infra/Mcp/Tools/*.cs` (all 10 files) — method names extracted via `[McpServerTool]` attribute lookahead; all 28 phantom names grep-confirmed absent; all `[Description]` attributes extracted for the 7 drifted categories
- `src/Valt.UI/Views/Main/Modals/Settings/SettingsView.axaml` — full read: 3 tabs, 10+ settings, exact bindings
- `src/Valt.UI/Views/Main/Modals/Settings/SettingsViewModel.cs` — cultures list behavior (86-114), MCP port default 5200, persistence paths
- `src/Valt.Infra/Settings/DisplaySettings.cs` + `CurrencySettings.cs` — persisted settings, defaults, port clamp
- `src/Valt.UI/Services/Theming/ThemeService.cs:22-34` — 13 dark themes, verbatim names
- `src/Valt.UI/Services/LocalStorage/LocalStorageService.cs` — culture/theme/fontscale/MCP-enabled persistence
- `src/Valt.UI/Views/Main/Modals/CreateDatabase/CreateDatabaseViewModel.cs:128-135` — user-chosen `.valt` file location ("MyDatabase.valt")
- `src/Valt.UI/Lang/language.resx` + `language.pt-BR.resx` — all Settings labels extracted verbatim via XML parse; `language.es.resx` exists (3 app languages)
- `valt-docs/docs/funcionalidades/ativos.md:219-228` — Asset Groups inline documentation (deferral rationale)
- `valt-docs/docs/guia/instalacao.md:52-87` — existing `.valt`/password/backup coverage (cross-link target)
- Live environment probes this session: baseline strict build green; orphan page → exit 0 (INFO); broken internal link → "Aborted with 1 warnings in strict mode!"; PT-only page in nav → exit 0 (silent fallback); build log "Translated 18 navigation elements to 'pt'"
- Parity baseline sweep: all 16 pairs heading/table-row identical (`moedas.md` line-count 137/134 is prose wrapping only)

### Secondary (MEDIUM confidence)
- `.planning/phases/37-mcp-server-page-update/37-RESEARCH.md` + `37-01-SUMMARY.md` — carried-forward drift hand-off (D-04), build-env pitfalls, commit conventions
- `.planning/phases/36-*/36-VERIFICATION.md` — verification/truths-table pattern for the phase gate

### Tertiary (LOW confidence)
- None — no web sources used; no external research providers configured (`exa_search: false`, `brave_search: false`, `firecrawl: false`), and none were needed since every source of truth is local and authoritative.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — build re-executed green this session; no installs
- Architecture: HIGH — nav structure, settings inventory, drift enumeration, and gate behavior all verified by direct file reads, greps, and four live build experiments
- Pitfalls: HIGH — Pitfalls 1–3 reproduced empirically (orphan/broken-link/missing-mirror builds); Pitfall 5 traced to specific code lines

**Research date:** 2026-07-16
**Valid until:** 2026-08-15 (30 days — stable docs domain; invalidate sooner if `src/Valt.Infra/Mcp/Tools/` or the Settings modal change)
