# Requirements: Valt

**Defined:** 2026-08-04
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v0.7 Requirements (Insights & Metrics Expansion)

New analytics derived from existing app data, surfaced in the Reports tab. Each maps to roadmap phases 39+.

### Spending Analytics

- [x] **SPA-01**: User can view monthly savings rate ((income − expenses) / income) with trend over time
- [x] **SPA-02**: User can view burn rate — average daily spend and projected month-end total vs median
- [ ] **SPA-03**: User can view fixed vs variable expense ratio per month (FixedExpenses vs actual transactions)

### BTC-Denominated Metrics

- [ ] **BTC-01**: User can view sats earned per month (income converted at receipt-date rates)
- [ ] **BTC-02**: User can view sats spent per month and per category
- [ ] **BTC-03**: User can view stack velocity — net sats accumulated per month with trend chart

### Wealth & Performance

- [ ] **WLT-01**: User can view net worth CAGR / compound growth in fiat and BTC terms
- [ ] **WLT-02**: User can view fiat vs BTC allocation % over time
- [ ] **WLT-03**: User can view best and worst months ranked by wealth delta
- [ ] **WLT-04**: User can view days under water (time since all-time high)

### Loans & Leverage

- [ ] **LON-01**: User can view total interest/fees paid and per-month breakdown from loan state timeline
- [ ] **LON-02**: User can view liquidation-price distance trend over time

## v2 Requirements

Deferred to a future release. Tracked but not in the v0.7 roadmap.

### Spending Analytics

- **SPA-04**: Spending patterns heatmap (day-of-week / day-of-month)
- **SPA-05**: Top-N largest expenses of the period
- **SPA-06**: Multi-category spending trend lines over time

### BTC-Denominated Metrics

- **BTC-04**: Hindsight cost — past expenses valued at today's BTC price
- **BTC-05**: DCA consistency — purchase cadence and avg buy price vs current

### Wealth & Performance

- **WLT-05**: Rolling 3/6/12-month income/expense averages

### Loans & Leverage

- **LON-03**: Debt-to-wealth ratio over time

### Goals

- **GOL-01**: Forecast goal completion dates from progress velocity
- **GOL-02**: Historical goal achievement rate

## Out of Scope

| Feature | Reason |
|---------|--------|
| Cost basis & profit reports (realized/unrealized, cost-basis bands) | Belongs to the AvgPrice module; separate milestone |
| Account analytics (per-account balance evolution, concentration) | Lower value density than selected metrics |
| v0.4 quality/hardening items (async void, HttpClient factory, job throttling, LiteDB indexes, god-VM refactor, test isolation, handler tests) | Kept for a dedicated quality milestone; v0.7 stays feature-focused |
| Mobile or web port | Not in scope |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SPA-01 | Phase 39 | Complete |
| SPA-02 | Phase 39 | Complete |
| SPA-03 | Phase 39 | Pending |
| BTC-01 | Phase 40 | Pending |
| BTC-02 | Phase 40 | Pending |
| BTC-03 | Phase 40 | Pending |
| WLT-01 | Phase 41 | Pending |
| WLT-02 | Phase 41 | Pending |
| WLT-03 | Phase 41 | Pending |
| WLT-04 | Phase 41 | Pending |
| LON-01 | Phase 42 | Pending |
| LON-02 | Phase 42 | Pending |

**Coverage:**

- v0.7 requirements: 12 total
- Mapped to phases: 12 ✓
- Unmapped: 0 ✓

---
*Requirements defined: 2026-08-04*
*Last updated: 2026-08-04 after v0.7 roadmap creation*
