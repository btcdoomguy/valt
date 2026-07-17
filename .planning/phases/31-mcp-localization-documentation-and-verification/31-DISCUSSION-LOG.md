# Phase 31: MCP, Localization, Documentation, and Verification - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-13
**Phase:** 31-MCP, Localization, Documentation, and Verification
**Areas discussed:** Localization strings

---

## Localization strings

| Option | Description | Selected |
|--------|-------------|----------|
| SoldAssetHistory_* prefix | Matches the existing LoanStateHistory_Title pattern and keeps all modal strings under one prefix. | ✓ |
| Mixed Assets_* + SoldAssetHistory_* | Uses Assets_* for action strings in the main Assets tab and SoldAssetHistory_* for modal-only strings. | |

**User's choice:** SoldAssetHistory_* prefix
**Notes:** The user wants a single consistent prefix for all new sold-asset strings, including the Mark as Sold context menu and History toolbar button.

| Option | Description | Selected |
|--------|-------------|----------|
| All new strings | Localize every hardcoded Phase 30 string: History title, columns, empty/error states, Restore Asset button, Close button, Mark as Sold menu, History toolbar button, Date Sold prompt, and all confirmation/notification messages. | ✓ |
| Visible labels only | Localize visible labels and buttons only; keep English for confirmation dialog bodies and error messages. | |

**User's choice:** All new strings
**Notes:** Every new hardcoded user-facing string added in Phase 30 should get a resx key.

| Option | Description | Selected |
|--------|-------------|----------|
| Provide all 3 languages now | Add English, Portuguese, and Spanish values in the same PR so the UI is fully localized. | ✓ |
| English only, placeholders later | Add English strings and leave pt-BR/es as placeholders for future translation. | |

**User's choice:** Provide all 3 languages now
**Notes:** Do not leave non-English placeholders; provide translations in the same PR.

| Option | Description | Selected |
|--------|-------------|----------|
| Use SoldAssetHistory_* everywhere | Use SoldAssetHistory_MarkAsSold and SoldAssetHistory_History for the main-tab actions too. | ✓ |
| Use Assets_* for main-tab actions | Use Assets_MarkAsSold and Assets_History since they live in the main Assets tab. | |

**User's choice:** Use SoldAssetHistory_* everywhere
**Notes:** Consistent prefix across the feature, even for strings that appear in the main Assets tab.

---

## the agent's Discretion

The following areas were not selected for discussion and are left to the planner/executor's discretion within the bounds of the requirements:
- MCP tool design (names, parameters, return shapes) for MarkAssetAsSold, UndoAssetSale, and GetSoldAssets.
- Documentation depth and exact section structure for `.claude/docs/assets.md`.
- Concrete end-to-end verification steps beyond the requirement to cover mark sold, history browse, details panel, undo, and totals refresh.

## Deferred Ideas

None — discussion stayed within Phase 31 scope. (v2 history enhancements and sale-price/capital-gains tracking remain in `.planning/REQUIREMENTS.md` v2.)
