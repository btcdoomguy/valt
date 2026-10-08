---
phase: 51-mcp-localization-documentation-verification
plan: 02
subsystem: localization
tags: [localization, resx, pt-BR, es, GOL-09]
requires:
  - Phase 50 GoalSummary English keys (language.resx lines 2030-2066, untouched)
  - language.Designer.cs static properties (pre-existing, untouched)
provides:
  - Fully localized Goal Summary modal in pt-BR and es (12 keys each)
affects:
  - src/Valt.UI/Lang/language.pt-BR.resx
  - src/Valt.UI/Lang/language.es.resx
tech-stack:
  added: []
  patterns:
    - 4-line resx data blocks with xml:space="preserve", mirroring en block order
    - Established sibling terminology reuse per 51-PATTERNS.md tables
key-files:
  created: []
  modified:
    - src/Valt.UI/Lang/language.pt-BR.resx (+37 lines, 12 keys)
    - src/Valt.UI/Lang/language.es.resx (+37 lines, 12 keys)
decisions:
  - "es Period label uses Período, not the Frecuencia precedent (Frecuencia means Frequency; Período already appears 3x in the es file and is semantically correct per UI-SPEC rule 1)"
  - "View summary rendered as infinitive verb phrase following the existing Ver Histórico / Ver historial context-menu pattern (pt-BR Ver resumo, es Ver resumen)"
metrics:
  duration: ~10 minutes
  completed: 2026-10-07
  tasks: 2
  commits: 2
status: complete
actuals:
  tokens: 1700
  tasks: 2
  commits: 2
---

# Phase 51 Plan 02: Goal Summary pt-BR + es Translations Summary

All 12 Phase 50 GoalSummary keys translated into pt-BR and es with established
sibling terminology, 1.5x length parity, and exact punctuation preservation —
key sets now mirror the en file at 1048 keys in all three locales (GOL-09).

## Task Results

| # | Task | Commit | Result |
|---|------|--------|--------|
| 1 | pt-BR translations — 12 GoalSummary keys | cde0ea7 | 12 keys inserted after `Goals_YearlyIndicator`, en block order; 1048 total keys; XML well-formed |
| 2 | es translations — 12 GoalSummary keys + tri-locale parity gate | 788d29f | 12 keys inserted; 1048 total keys; parity gate PASS; Designer.cs untouched |

## Translations Delivered

| Key | en | pt-BR (chars / limit) | es (chars / limit) |
|-----|----|----------------------|--------------------|
| Goals_ViewSummary | View summary (13) | Ver resumo (10/19) | Ver resumen (11/19) |
| GoalSummary_Title | Goal summary (12) | Resumo da meta (14/18) | Resumen de meta (15/18) |
| GoalSummary_PeriodLabel | Period (6) | Período (7/9) | Período (7/9) |
| GoalSummary_TotalLabel | Total (5) | Total (5/7) | Total (5/7) |
| GoalSummary_ColumnAccount | Account (7) | Conta (5/10) | Cuenta (6/10) |
| GoalSummary_ColumnCategory | Category (8) | Categoria (9/12) | Categoría (9/12) |
| GoalSummary_ColumnFiat | Fiat | Fiat (untranslated) | Fiat (untranslated) |
| GoalSummary_ColumnSats | Sats | Sats (untranslated) | Sats (untranslated) |
| GoalSummary_ColumnRunningTotal | Running total (13) | Total acumulado (15/19) | Total acumulado (15/19) |
| GoalSummary_EmptyTitle | No transactions yet (20) | Ainda sem transações (20/30) | Aún sin transacciones (21/30) |
| GoalSummary_EmptyMessage | …this goal yet. (46) | Nenhuma transação contribui para esta meta ainda. (49/69) | Ninguna transacción contribuye a esta meta aún. (47/69) |
| GoalSummary_Close | Close (5) | Fechar (6/7) | Cerrar (6/7) |

Only `GoalSummary_EmptyMessage` ends with a period in both locales; no other
value carries trailing punctuation.

## Verification Evidence

- `grep -c "GoalSummary_\|Goals_ViewSummary"` → exactly 12 in both pt-BR and es
- `grep -c "<data name="` → 1048 / 1048 / 1048 (en / pt-BR / es)
- Tri-locale parity: sorted key-name extraction byte-identical across all three files
- XML well-formedness: `xml.dom.minidom.parse` exits 0 for both modified files
- `language.Designer.cs`: 11 `GoalSummary_*` + 1 `Goals_ViewSummary` static properties pre-exist; `git diff --stat` empty (untouched)
- `language.resx` (en source): untouched
- "Fiat" kept untranslated in both locales: 10 existing value usages per file confirm the convention (UI-SPEC rule 4)

## Decisions Made

1. **es Period → Período (not Frecuencia):** the `FixedExpenses.Columns.Period`
   es precedent reads "Frecuencia" (Frequency), which is semantically wrong for
   a date-period label. "Período" already appears in the es file and matches the
   English semantic role — chosen per UI-SPEC rule 1 (terminology consistency)
   with this documented deviation from the Frecuencia precedent.
2. **View summary → infinitive verb phrase:** both files already render view
   actions as infinitives in context menus (`Ver Histórico` pt-BR, `Ver historial`
   es), consistent with `Editar`/`Recalcular` — so `Ver resumo` / `Ver resumen`.

## Deviations from Plan

None blocking. The two decisions above are within the plan's own instructions
(the es read_first note explicitly sanctions Período; the pt-BR action
instructs choosing a verb phrase consistent with existing context-menu labels).

## Known Stubs

None — all 12 keys carry real translations wired to the Phase 50 XAML via the
pre-existing Designer properties.

## Self-Check: PASSED

- src/Valt.UI/Lang/language.pt-BR.resx: FOUND (12 keys, 1048 total)
- src/Valt.UI/Lang/language.es.resx: FOUND (12 keys, 1048 total)
- Commit cde0ea7: FOUND
- Commit 788d29f: FOUND
