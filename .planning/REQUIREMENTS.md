# Requirements: Valt

**Defined:** 2026-08-04
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v0.8 Requirements (BTC Loan Simulator)

A what-if calculator for BTC-backed loans, mirroring the Leverage Simulator layout (inputs left, results right). Accessible from the Tools menu.

### Simulator Core

- [ ] **SIM-01**: User can input loan parameters: collateral (BTC), amount taken, liquidation LTV, start date, interest rate, fees, end date
- [ ] **SIM-02**: Results recalculate live as inputs change (mirroring Leverage Simulator behavior)
- [ ] **SIM-03**: User can choose simple or compound interest mode
- [ ] **SIM-04**: Simple interest mode uses the app's existing act/365 convention (parity with current loan math); compound mode uses daily accrual

### Results Panel

- [ ] **SIM-05**: User can view total to repay (principal + interest + fees) with an interest/fees breakdown
- [ ] **SIM-06**: User can view all result values in fiat and sats, converted at the current BTC price with a visible conversion basis
- [ ] **SIM-07**: User can view the liquidation BTC price derived from liquidation LTV and total debt
- [ ] **SIM-08**: User can view the effective APR (fee-inclusive annualized rate)
- [ ] **SIM-09**: User can view distance to liquidation versus the current live BTC price

### Cost-over-time Schedule

- [ ] **SIM-10**: User can view a monthly schedule (date, accrued interest, cumulative total in fiat and sats) until the end date

### Integration

- [ ] **SIM-11**: User can load an existing BTC-backed loan from Assets to prefill the simulator, with a "New simulation" option
- [ ] **SIM-12**: AI assistant can run a BTC loan simulation via an MCP tool
- [ ] **SIM-13**: All new user-facing strings are localized (en-US, pt-BR, es) and module documentation is updated

## v0.7 Requirements (Insights & Metrics Expansion)

New analytics derived from existing app data, surfaced in the Reports tab. Each maps to roadmap phases 39+.

### Spending Analytics

- [x] **SPA-01**: User can view monthly savings rate ((income − expenses) / income) with trend over time
- [x] **SPA-02**: User can view burn rate — average daily spend and projected month-end total vs median
- [x] **SPA-03**: User can view fixed vs variable expense ratio per month (FixedExpenses vs actual transactions)

### BTC-Denominated Metrics

- [x] **BTC-01**: User can view sats earned per month (income converted at receipt-date rates)
- [x] **BTC-02**: User can view sats spent per month and per category
- [x] **BTC-03**: User can view stack velocity — net sats accumulated per month with trend chart

### Wealth & Performance

- [x] **WLT-01**: User can view net worth CAGR / compound growth in fiat and BTC terms
- [x] **WLT-02**: User can view fiat vs BTC allocation % over time
- [x] **WLT-03**: User can view best and worst months ranked by wealth delta
- [x] **WLT-04**: User can view days under water (time since all-time high)

### Loans & Leverage

- [x] **LON-01**: User can view total interest/fees paid and per-month breakdown from loan state timeline
- [x] **LON-02**: User can view liquidation-price distance trend over time

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

### Simulator

- **SIM-14**: Liquidation price per schedule row (LTV creep over time)
- **SIM-15**: What-if BTC price slider in the simulator

### Goals

- **GOL-01**: Forecast goal completion dates from progress velocity
- **GOL-02**: Historical goal achievement rate

## Out of Scope

| Feature | Reason |
|---------|--------|
| Cost basis & profit reports (realized/unrealized, cost-basis bands) | Belongs to the AvgPrice module; separate milestone |
| Account analytics (per-account balance evolution, concentration) | Lower value density than selected metrics |
| v0.4 quality/hardening items (async void, HttpClient factory, job throttling, LiteDB indexes, god-VM refactor, test isolation, handler tests) | Kept for a dedicated quality milestone; v0.7 stays feature-focused |
| Amortizing payment schedule in loan simulator | Real BTC loans are interest-only + balloon; amortization adds complexity for a structure users don't have |
| Simulated BTC price path / liquidation-time projection in loan simulator | Speculative price forecasting; Reports already has custom price simulation |
| Persisting loan simulations as assets | Pollutes the Assets ledger with hypothetical data; simulator is ephemeral like the Leverage Simulator |
| Origination fee as percentage input | Flat fee matches existing `BtcLoanDetails.Fees`; two input modes create validation ambiguity |
| Mobile or web port | Not in scope |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SIM-01 | — | Pending |
| SIM-02 | — | Pending |
| SIM-03 | — | Pending |
| SIM-04 | — | Pending |
| SIM-05 | — | Pending |
| SIM-06 | — | Pending |
| SIM-07 | — | Pending |
| SIM-08 | — | Pending |
| SIM-09 | — | Pending |
| SIM-10 | — | Pending |
| SIM-11 | — | Pending |
| SIM-12 | — | Pending |
| SIM-13 | — | Pending |
| SPA-01 | Phase 39 | Complete |
| SPA-02 | Phase 39 | Complete |
| SPA-03 | Phase 39 | Complete |
| BTC-01 | Phase 40 | Complete |
| BTC-02 | Phase 40 | Complete |
| BTC-03 | Phase 40 | Complete |
| WLT-01 | Phase 41 | Complete |
| WLT-02 | Phase 41 | Complete |
| WLT-03 | Phase 41 | Complete |
| WLT-04 | Phase 41 | Complete |
| LON-01 | Phase 42 | Complete |
| LON-02 | Phase 42 | Complete |

**Coverage:**

- v0.8 requirements: 13 total
- Mapped to phases: 0 (roadmap pending)
- Unmapped: 13 (roadmap pending)
- v0.7 requirements: 12 total, all Complete ✓

---
*Requirements defined: 2026-08-04*
*Last updated: 2026-08-13 after v0.8 milestone requirements definition*
