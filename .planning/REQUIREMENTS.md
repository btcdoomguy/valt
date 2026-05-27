# Requirements: Valt — Spending Evolution Module

**Defined:** 2026-05-27
**Core Value:** Users can understand their financial life denominated in bitcoin, making it clear how fiat inflation affects their purchasing power while maintaining a complete view of their wealth.

## v1 Requirements

### User Interface

- [ ] **UI-01**: User can open Spending Evolution modal from Main Menu > Tools > Evolução de gastos
- [ ] **UI-02**: User can open Spending Evolution modal by right-clicking on a debit transaction and selecting "Analisar evolução"
- [ ] **UI-03**: Modal displays category selector on left side with multi-select tree (similar to Reports module)
- [ ] **UI-04**: Modal displays dual-axis line chart on right side with fiat total (left Y-axis) and sats total (right Y-axis)
- [ ] **UI-05**: Modal includes time range dropdown above chart with options: 12, 24, 36, 48, 60 months
- [ ] **UI-06**: Modal displays cost of living indicators below chart: fiat increase percentage and BTC increase percentage
- [ ] **UI-07**: Chart and indicators update automatically when user selects/deselects categories
- [ ] **UI-08**: When opened via right-click, modal pre-selects only the category of the clicked transaction
- [ ] **UI-09**: When opened via menu, modal pre-selects all categories by default

### Data & Calculations

- [ ] **DATA-01**: System aggregates transactions by month for selected categories and time range
- [ ] **DATA-02**: System calculates monthly fiat total using transaction amounts converted to user's primary fiat currency
- [ ] **DATA-03**: System calculates monthly sats total using transaction's PriceInSats field
- [ ] **DATA-04**: System calculates cost of living increase percentage in fiat terms (first month vs last month)
- [ ] **DATA-05**: System calculates cost of living increase percentage in BTC terms (first month vs last month)
- [ ] **DATA-06**: System handles missing PriceInSats gracefully (show warning, exclude from sats calculation or use available data)
- [ ] **DATA-07**: Query performance remains acceptable (<500ms) for 5 years of transaction data

### Integration

- [ ] **INT-01**: Module follows existing CQRS pattern (Query + Handler in Valt.App)
- [ ] **INT-02**: Module follows existing layered architecture (Core → App → Infra → UI)
- [ ] **INT-03**: Module reuses existing chart components and patterns from Reports module
- [ ] **INT-04**: Module registers in main menu following existing menu patterns
- [ ] **INT-05**: Module registers context menu handler for debit transactions
- [ ] **INT-06**: Module includes localization strings for all three languages (en-US, pt-BR, es)

## v2 Requirements

### Export & Sharing

- **EXP-01**: User can export spending evolution data as CSV
- **EXP-02**: User can export chart as image (PNG)

### Advanced Analysis

- **ANAL-01**: User can compare multiple category groups side-by-side
- **ANAL-02**: User can view year-over-year comparison
- **ANAL-03**: User can set custom date ranges (not just preset 12-60 months)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Predictive forecasting | Requires ML, not core to financial visibility |
| Budget target comparison | Covered by existing Goals module |
| Transaction-level drill-down | Keep analysis high-level; use Reports for details |
| Real-time price updates for historical data | Use existing historical price data, don't recalculate |
| Multi-user/shared views | Single-user desktop app by design |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| UI-01 | Phase 1 | Pending |
| UI-02 | Phase 1 | Pending |
| UI-03 | Phase 2 | Pending |
| UI-04 | Phase 2 | Pending |
| UI-05 | Phase 2 | Pending |
| UI-06 | Phase 2 | Pending |
| UI-07 | Phase 2 | Pending |
| UI-08 | Phase 2 | Pending |
| UI-09 | Phase 2 | Pending |
| DATA-01 | Phase 2 | Pending |
| DATA-02 | Phase 2 | Pending |
| DATA-03 | Phase 2 | Pending |
| DATA-04 | Phase 2 | Pending |
| DATA-05 | Phase 2 | Pending |
| DATA-06 | Phase 2 | Pending |
| DATA-07 | Phase 2 | Pending |
| INT-01 | Phase 2 | Pending |
| INT-02 | Phase 2 | Pending |
| INT-03 | Phase 2 | Pending |
| INT-04 | Phase 1 | Pending |
| INT-05 | Phase 1 | Pending |
| INT-06 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 22 total
- Mapped to phases: 22
- Unmapped: 0 ✓

---
*Requirements defined: 2026-05-27*
*Last updated: 2026-05-27 after initial definition*
