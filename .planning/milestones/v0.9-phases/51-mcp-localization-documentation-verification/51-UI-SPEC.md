---
phase: 51
slug: mcp-localization-documentation-verification
status: approved
shadcn_initialized: false
preset: none
created: 2026-10-06
---

# Phase 51 — UI Design Contract

> This phase ships **no new user-facing surfaces**. Scope (51-CONTEXT.md):
> MCP tool `GetGoalContributingTransactions` (GOL-08), pt-BR + es translations
> of the 12 Phase 50 localization keys (GOL-09), `.claude/docs/goals.md`
> update (GOL-10), and end-to-end verification.
>
> The only UI contract in scope is the **COPY / TRANSLATION contract** below:
> the 12 existing English strings (contract-locked by the Phase 50 UI-SPEC
> Copywriting Contract, source of truth for English text) must be translated
> to pt-BR and es preserving their semantic role and tone. All visual and
> interaction dimensions are **N/A — inherited from the Phase 50 UI-SPEC and
> unchanged by this phase.**

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Avalonia 11.3 / .NET 9 project — shadcn gate N/A) |
| Preset | not applicable |
| Component library | N/A — no new components; Phase 50 modal and its styles unchanged |
| Icon library | N/A — no new icons |
| Font | N/A — no new typography |

---

## Spacing Scale

**N/A** — no new or modified visual surfaces. The Phase 50 contract
(4 / 8 / 12 / 16 / 24px scale, modal 720×480 min) remains the governing
visual contract for the Goal Summary modal and is unchanged.

---

## Typography

**N/A** — no new or modified text styles. Phase 50 contract unchanged:
12px grid text (400 rows / 600 headers), 13px header-strip labels,
16px Semibold GeistMono final total.

---

## Color

**N/A** — no new brushes, no new semantic colors. Phase 50 contract
unchanged (window default / `Background800Brush` / `Background500Brush` /
`Text100Brush` / `Text400Brush` / `SemanticPositive200Brush` /
`SemanticNegative200Brush`; accent reserved for nothing; no destructive
color).

---

## Copywriting Contract (Translation Contract — GOL-09)

Source of truth for English text: **50-UI-SPEC.md §Copywriting Contract**
(locked copy) and `src/Valt.UI/Lang/language.resx` (verified 2026-10-06,
keys at lines 2031–2066). This phase adds the same 12 keys to
`language.pt-BR.resx` and `language.es.resx` only. No resx key renames,
no key additions, no copy edits. `language.Designer.cs` already contains
the 12 static properties (verified) — no Designer changes needed.

### Translation tone rules (prescriptive)

1. **Terminology consistency:** translate using the terminology already
   established in the same files for sibling keys — e.g. "Account",
   "Category", "Close", "Total" must match the existing translations of
   those words elsewhere in `language.pt-BR.resx` / `language.es.resx`
   (precedent: other GoalSummary/Goals keys and the LoanStateHistory modal).
2. **Length parity:** translations must stay near English length —
   column headers render in a fixed-width 12px grid header; a translation
   materially longer than the English source risks header overflow.
   Hard limit: **≤ 1.5× English character count** per string.
3. **Case and punctuation:** preserve sentence case and trailing
   punctuation exactly as in English (`GoalSummary_EmptyMessage` ends with
   a period; headers and labels have no trailing punctuation).
4. **Untranslatable tokens:** "Fiat" and "Sats" stay as-is in pt-BR and es
   (domain terms used untranslated across the existing resx files — verify
   against existing usage before translating).
5. **Register:** neutral, terse, product UI register — no marketing tone.
   Empty-state message is informational, not apologetic.

### The 12 keys with semantic roles

| # | Resx key | English (locked) | Semantic role | Translation notes |
|---|----------|------------------|---------------|-------------------|
| 1 | `Goals_ViewSummary` | View summary | Context-menu item on a goal row (verb phrase, imperative-infinitive) | Action label; keep it a short verb phrase. Length-critical: renders in a narrow context menu. |
| 2 | `GoalSummary_Title` | Goal summary | Modal title bar / window title | Noun phrase, title case per English sentence case. |
| 3 | `GoalSummary_PeriodLabel` | Period | Header-strip label next to the period value | Single-word field label; muted secondary text. Keep it one word if the language allows. |
| 4 | `GoalSummary_TotalLabel` | Total | Header-strip label for the final reconciled total | Single-word field label; pair with #3 in style. |
| 5 | `GoalSummary_ColumnAccount` | Account | DataGrid column header | Table header; match the existing translation of "Account" used in other grids (e.g. transactions view). |
| 6 | `GoalSummary_ColumnCategory` | Category | DataGrid column header | Table header; match existing "Category" translation in other grids. |
| 7 | `GoalSummary_ColumnFiat` | Fiat | DataGrid column header | Domain term — strongly prefer keeping "Fiat" untranslated (check existing resx usage). |
| 8 | `GoalSummary_ColumnSats` | Sats | DataGrid column header | Domain term — keep "Sats" untranslated. |
| 9 | `GoalSummary_ColumnRunningTotal` | Running total | DataGrid column header | Compound label; two short words. This is the widest header — most sensitive to length parity (rule 2). |
| 10 | `GoalSummary_EmptyTitle` | No transactions yet | Empty-state heading | Short declarative statement; slightly larger/emphasized visually. |
| 11 | `GoalSummary_EmptyMessage` | No transactions contribute to this goal yet. | Empty-state body sentence | Full sentence, trailing period required; informational tone. |
| 12 | `GoalSummary_Close` | Close | Modal close button (default Fluent button) | Match the existing "Close" translation used by `LoanStateHistory_Close` and other modals in the same files. |

### Out-of-scope copy

- `GoalSummary_ColumnDate` / `GoalSummary_ColumnDescription` are **not**
  among the 12 keys — the Date/Description column headers reuse existing
  resx keys from the transaction grid (per 50-UI-SPEC "reuse existing
  transaction-grid key if present"); those existing keys are already
  localized and need no action.
- Error-state copy reuses the existing `language.Error` key — no action.
- MCP tool (`GetGoalContributingTransactions`) is machine-facing: its DTO
  field names and `[Description]` attributes are English-only API surface,
  not localized UI copy — no localization action.

---

## UI Considerations

None applicable — no new UI states, no new interactions. Phase 50's
state coverage (empty / populated / zero-one-many / partial / error /
loading / gating / overflow / long-text) is unchanged and already
verified. End-to-end verification in this phase (success criterion 4)
compares rendered grid contents + final running total against goal
progress and MCP DTO rows — a data-parity check, not a new UI contract.

Applicable state considerations resolved: none applicable.

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| — | — | — | No new user-facing surfaces; Phase 50 contract governs |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| — | none | not required (Avalonia/.NET project; no component registries; shadcn N/A) |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS (N/A — inherited from Phase 50, unchanged)
- [ ] Dimension 3 Color: PASS (N/A — inherited from Phase 50, unchanged)
- [ ] Dimension 4 Typography: PASS (N/A — inherited from Phase 50, unchanged)
- [ ] Dimension 5 Spacing: PASS (N/A — inherited from Phase 50, unchanged)
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
