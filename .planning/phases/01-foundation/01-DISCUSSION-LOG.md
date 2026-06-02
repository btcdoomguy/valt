# Discussion Log — Phase 01: Foundation

**Date:** 2026-05-27
**Phase:** 01 — Foundation

## Gray Areas Discussed

### 1. Modal Dimensions

**Question:** What size should the Spending Evolution modal be? Existing modals range from 600x400 (simple) to 900x600 (complex).

**Options presented:**
- Fixed size matching design dimensions (like existing 660x470 modals)
- Resizable with larger starting size (900x600) for complex analytical content

**User decision:** Resizable, starting size 900x600. Complex analytical modal needs space for category selector + chart + indicators.

### 2. Menu Placement

**Question:** Should "Evolução de gastos" go under the existing Tools submenu or as a top-level menu item?

**Options presented:**
- Under Tools submenu (grouped with ConversionCalculator, LeverageSimulator, PriceHistory)
- Top-level menu item in main dropdown

**User decision:** Under existing Tools submenu. Consistent with other analytical tools.

### 3. Context Menu Scope

**Question:** Right-click context menu: only on debit transactions or on any transaction type?

**Options presented:**
- Only debit transactions (spending-focused)
- Any transaction type (category-driven analysis)

**User decision:** Any transaction. What counts is the category selected, not the transaction type.

## Discussion Notes

- User wants the module accessible from both main menu and context menu
- Context menu should pre-select the clicked transaction's category
- Menu open should show all categories selected by default
- Modal parameter passing: string? categoryId — null for menu, category ID for context menu

## Deferred Ideas

None captured during this discussion.

## Agent Discretion Items

- Modal will be empty shell in Phase 1 — just structure, no actual controls
- Follow existing `ValtModalViewModel` + `IModalFactory` pattern exactly
- No localization in Phase 1 — strings added in Phase 3
