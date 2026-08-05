---
quick_id: 260804-u3u
slug: add-pt-br-and-es-translations-for-the-ne
mode: quick
status: complete
---

# Quick Task Summary: Add pt-BR and es translations for Phase 39 Reports strings

## What Changed

Added Portuguese (pt-BR) and Spanish (es) translations for the 18 new Phase 39 Reports strings:

- `Reports_BurnRate_*` (9 strings)
- `Reports_SavingsRate_*` (3 strings)
- `Reports_FixedVariable_*` (6 strings)

## Files Changed

- `src/Valt.UI/Lang/language.pt-BR.resx`
- `src/Valt.UI/Lang/language.es.resx`

## Verification

- `dotnet build Valt.sln` → 0 errors
- `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` → 9/9 passed

## Commit

`fix(quick): add pt-BR and es translations for Phase 39 Reports strings`
