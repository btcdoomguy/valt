# Roadmap: Valt — Spending Evolution Module

**Milestone:** v1.0 Spending Evolution
**Created:** 2026-05-27
**Granularity:** Coarse
**Mode:** YOLO

## Overview

**3 phases** | **22 requirements mapped** | All v1 requirements covered ✓

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 1 | Foundation | Wire up menu integration and modal shell | INT-04, INT-05, UI-01, UI-02 | 4 |
| 2 | Core Implementation | Build query, chart, and data layer | UI-03, UI-04, UI-05, UI-06, UI-07, UI-08, UI-09, DATA-01, DATA-02, DATA-03, DATA-04, DATA-05, DATA-06, DATA-07, INT-01, INT-02, INT-03 | 17 |
| 3 | Polish | Localization, performance, and testing | INT-06 | 1 |

---

## Phase Details

### Phase 1: Foundation

**Goal:** Set up menu entries, context menu handler, and modal window shell.

**Requirements:** INT-04, INT-05, UI-01, UI-02

**Success Criteria:**
1. "Evolução de gastos" appears in Main Menu > Tools
2. Right-clicking a debit transaction shows "Analisar evolução" option
3. Clicking either opens an empty modal window following existing modal patterns
4. Modal has correct MinWidth/MinHeight and custom title bar

**Depends on:** None

---

### Phase 2: Core Implementation

**Goal:** Build the query layer, chart visualization, category selector, and data calculations.

**Requirements:** UI-03, UI-04, UI-05, UI-06, UI-07, UI-08, UI-09, DATA-01, DATA-02, DATA-03, DATA-04, DATA-05, DATA-06, DATA-07, INT-01, INT-02, INT-03

**Success Criteria:**
1. Category selector displays all categories in a tree with checkboxes
2. Dual-axis line chart shows fiat total (left Y) and sats total (right Y)
3. Time range dropdown offers 12/24/36/48/60 month options
4. Cost of living indicators show percentage increase for both fiat and BTC
5. Chart updates automatically when categories are selected/deselected
6. Right-click opens modal with only that transaction's category pre-selected
7. Menu opens modal with all categories pre-selected
8. Query completes in <500ms for 5 years of data
9. Data calculations correctly aggregate by month and currency
10. Missing PriceInSats data is handled gracefully (warning shown)

**Depends on:** Phase 1

**Plans:**
- [x] 02-01-PLAN.md — Query Layer & DTOs (CQRS pipeline, LiteDB aggregation)
- [x] 02-02-PLAN.md — Chart Data & ViewModel (LiveCharts dual-axis, category loading)
- [x] 02-03-PLAN.md — View Layout & Integration (TreeView, indicators, pre-selection)

---

### Phase 3: Polish

**Goal:** Add localization, verify performance, and ensure code quality.

**Requirements:** INT-06

**Success Criteria:**
1. All UI strings localized in en-US, pt-BR, and es
2. Architecture tests pass (new module follows layer dependency rules)
3. Manual testing confirms all requirements from Phase 2
4. Code review passes (follows existing patterns)

**Depends on:** Phase 2

---

## Requirements Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| UI-01 | Phase 1 | Pending |
| UI-02 | Phase 1 | Pending |
| UI-03 | Phase 2 | Complete |
| UI-04 | Phase 2 | Complete |
| UI-05 | Phase 2 | Complete |
| UI-06 | Phase 2 | Complete |
| UI-07 | Phase 2 | Complete |
| UI-08 | Phase 2 | Complete |
| UI-09 | Phase 2 | Complete |
| DATA-01 | Phase 2 | Complete |
| DATA-02 | Phase 2 | Complete |
| DATA-03 | Phase 2 | Complete |
| DATA-04 | Phase 2 | Complete |
| DATA-05 | Phase 2 | Complete |
| DATA-06 | Phase 2 | Complete |
| DATA-07 | Phase 2 | Complete |
| INT-01 | Phase 2 | Complete |
| INT-02 | Phase 2 | Complete |
| INT-03 | Phase 2 | Complete |
| INT-04 | Phase 1 | Pending |
| INT-05 | Phase 1 | Pending |
| INT-06 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 22 total
- Mapped to phases: 22
- Unmapped: 0 ✓

---
*Roadmap created: 2026-05-27*
*Last updated: 2026-05-27 after 02-03 plan execution*
