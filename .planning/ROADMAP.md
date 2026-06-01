# Roadmap: Valt — Bitcoin Price Simulation

**Milestone:** v1.1 Bitcoin Price Simulation
**Created:** 2026-06-01
**Granularity:** Coarse
**Mode:** YOLO

## Overview

**3 phases** | **14 requirements mapped** | All v1 requirements covered ✓

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 4 | UI Foundation | Fixed price bar, buttons, and price input modal | UI-01, UI-02, UI-03 | 4 |
| 5 | Core Implementation | Service refactoring, custom price logic, visual states | UI-04, UI-05, UI-06, DATA-01, DATA-02, DATA-03, DATA-04, DATA-05 | 7 |
| 6 | Polish | Architecture tests, regression testing, localization | INT-01, INT-02, INT-03 | 4 |

---

## Phase Details

### Phase 4: UI Foundation

**Goal:** Create fixed price bar in Resumo panel with BTC price display, "Simular" button, and price input modal.

**Requirements:** UI-01, UI-02, UI-03

**Success Criteria:**
1. Fixed price bar is visible in Resumo panel showing current BTC price in main fiat currency
2. "Simular" button in the price bar opens a modal window
3. Modal allows input of custom BTC price with validation (non-negative, numeric)
4. Modal follows existing modal patterns (MinWidth/MinHeight, custom title bar, SystemDecorations=None)

**Depends on:** None

---

### Phase 5: Core Implementation

**Goal:** Refactor services to accept custom price, implement visual state changes, and ensure calculations use custom price only in Reports tab.

**Requirements:** UI-04, UI-05, UI-06, DATA-01, DATA-02, DATA-03, DATA-04, DATA-05

**Success Criteria:**
1. When custom price is active, "Alterar Preço" button replaces "Simular"
2. "Resetar" button appears when custom price is active and resets to real-time price
3. Visual indication clearly shows custom price is active (highlighted state, badge, or color change)
4. Dashboard calculations (total value, leveraged positions, etc.) use custom price when active
5. Custom price resets on app close (no persistence across sessions)
6. Other app areas (transactions, goals, avg price, etc.) remain unaffected by custom price
7. Service layer refactored with optional custom price parameter without breaking existing consumers

**Depends on:** Phase 4

---

### Phase 6: Polish

**Goal:** Ensure architecture compliance, no regressions, and full localization.

**Requirements:** INT-01, INT-02, INT-03

**Success Criteria:**
1. Architecture tests pass with zero layer violations
2. No regression in other app areas using same services (existing functionality preserved)
3. All UI strings localized in en-US, pt-BR, and es
4. Manual testing confirms all requirements from Phases 4 and 5

**Depends on:** Phase 5

---

## Requirements Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| UI-01 | Phase 4 | Pending |
| UI-02 | Phase 4 | Pending |
| UI-03 | Phase 4 | Pending |
| UI-04 | Phase 5 | Pending |
| UI-05 | Phase 5 | Pending |
| UI-06 | Phase 5 | Pending |
| DATA-01 | Phase 5 | Pending |
| DATA-02 | Phase 5 | Pending |
| DATA-03 | Phase 5 | Pending |
| DATA-04 | Phase 5 | Pending |
| DATA-05 | Phase 5 | Pending |
| INT-01 | Phase 6 | Pending |
| INT-02 | Phase 6 | Pending |
| INT-03 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0 ✓

---
*Roadmap created: 2026-06-01*
*Last updated: 2026-06-01 after initial creation*
