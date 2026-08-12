# Roadmap: Valt

## Milestones

- ✅ **v0.5 Asset Sold History** — Phases 29-31 (shipped 2026-07-14)
- ✅ **v0.6 Documentation Site Refresh** — Phases 32-38 (shipped 2026-07-17)
- 🚧 **v0.7 Insights & Metrics Expansion** — Phases 39-43 (planned)

## Phases

<details>
<summary>✅ v0.5 Asset Sold History (Phases 29-31) — SHIPPED 2026-07-14</summary>

- [x] **Phase 29: Domain, Persistence, and Active-View Filtering** — 4/4 plans — completed 2026-07-13
- [x] **Phase 30: History UI and Details Reuse** — 3/3 plans — completed 2026-07-13
- [x] **Phase 31: MCP, Localization, Documentation, and Verification** — 5/5 plans — completed 2026-07-14

</details>

<details>
<summary>✅ v0.6 Documentation Site Refresh (Phases 32-38) — SHIPPED 2026-07-17</summary>

- [x] **Phase 32: Factual Fixes and Cross-Page Accuracy** — 2/2 plans — completed 2026-07-15
- [x] **Phase 33: Assets Page Rewrite** — 3/3 plans — completed 2026-07-15
- [x] **Phase 34: Reports Page Update** — 2/2 plans — completed 2026-07-15
- [x] **Phase 35: Goals Page Completion** — 3/3 plans — completed 2026-07-16
- [x] **Phase 36: Fixed Expenses Page Enhancement** — 2/2 plans — completed 2026-07-16
- [x] **Phase 37: MCP Server Page Update** — 2/2 plans — completed 2026-07-16
- [x] **Phase 38: Navigation, New Pages, and Quality Assurance** — 3/3 plans — completed 2026-07-17

_Full phase details are archived in `.planning/milestones/v0.6-ROADMAP.md`._

</details>

### 🚧 v0.7 Insights & Metrics Expansion (Phases 39-43)

- [x] **Phase 39: Spending Analytics Reports & UI** — Savings rate, burn rate, and fixed vs variable ratio in the Reports tab (completed 2026-08-05)
  - [x] **Phase 40: BTC-Denominated Metrics Reports & UI** — Sats earned, sats spent (month + per category), and stack velocity
- [x] **Phase 41: Wealth & Performance Reports & UI** — Net worth CAGR, fiat vs BTC allocation, best/worst months, days under water (completed 2026-08-10)
- [x] **Phase 42: Loans & Leverage Reports & UI** — Interest/fees paid and liquidation-price distance trend
- [ ] **Phase 43: MCP, Localization, Documentation & Verification** — Tool exposure, 3-language strings, module docs, end-to-end sign-off

## Phase Details

### Phase 39: Spending Analytics Reports & UI

**Goal**: Users can understand their spending behavior — how much they save, how fast they burn cash, and how rigid their expense structure is
**Depends on**: Nothing (first v0.7 phase)
**Requirements**: SPA-01, SPA-02, SPA-03
**Success Criteria** (what must be TRUE):

  1. User can view monthly savings rate ((income − expenses) / income) with a trend over time in the Reports tab
  2. User can view burn rate — average daily spend and projected month-end total compared against the median
  3. User can view the fixed vs variable expense ratio per month (FixedExpenses vs actual transactions)
  4. New panels render sensible zero/empty states when no transaction data exists for the period

**Plans**: 8/8 plans complete

**Wave 1**

- [x] 39-01-PLAN.md — Savings rate + burn rate query backend (SpendingAnalytics module, tests)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 39-02-PLAN.md — Fixed vs variable query backend (Paid-record join, tests)
- [x] 39-03-PLAN.md — Burn rate dashboard card UI (RowItem foreground, panel VM, wiring)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 39-04-PLAN.md — Savings rate + fixed/variable chart sections UI (chart-data classes, wiring, empty states)

**Gap Closure** *(completed 2026-08-05)*

- [x] 39-05-PLAN.md — Backend category filter persistence and query wiring
- [x] 39-06-PLAN.md — UI centralized category filter button and wiring
- [x] 39-07-PLAN.md — Adjust filter placement and extend to Statistics dashboard
- [x] 39-08-PLAN.md — Migrate legacy Statistics excluded-category settings to centralized filter

**UI hint**: yes

### Phase 40: BTC-Denominated Metrics Reports & UI

**Goal**: Users can see their income and spending denominated in sats, including how fast their stack is growing
**Depends on**: Phase 39
**Requirements**: BTC-01, BTC-02, BTC-03
**Success Criteria** (what must be TRUE):

  1. User can view sats earned per month, with income converted at receipt-date rates
  2. User can view sats spent per month and broken down per category
  3. User can view stack velocity — net sats accumulated per month — as a trend chart
  4. Metrics honor the existing Reports tab filters (date range, accounts, categories)

**Plans**: 4/4 plans executed

- [x] 40-03-PLAN.md

**Wave 1**

- [x] 40-01-PLAN.md — BTC-denominated metrics backend and Reports-tab UI (tracer slice)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 40-02-PLAN.md — Sats spent per category breakdown and comprehensive tests

**Gap Closure**

- [x] 40-04-PLAN.md — Wire account filter to BTC metrics and fix category empty state

**UI hint**: yes

### Phase 41: Wealth & Performance Reports & UI

**Goal**: Users can evaluate their long-term wealth performance in both fiat and BTC terms
**Depends on**: Phase 39
**Requirements**: WLT-01, WLT-02, WLT-03, WLT-04
**Success Criteria** (what must be TRUE):

  1. User can view net worth CAGR / compound growth in fiat and BTC terms
  2. User can view fiat vs BTC allocation % over time
  3. User can view best and worst months ranked by wealth delta
  4. User can view days under water (time since the all-time high)

**Plans**: 1/1 plans executed
**UI hint**: yes

**Wave 1**

- [x] 41-01-PLAN.md — Days under water: asset-aware ATH report and dashboard row

### Phase 42: Loans & Leverage Reports & UI

**Goal**: Users can see the true cost and evolving risk of their BTC-backed loans
**Depends on**: Phase 39
**Requirements**: LON-01, LON-02
**Success Criteria** (what must be TRUE):

  1. User can view total interest/fees paid with a per-month breakdown derived from the loan state timeline
  2. User can view the liquidation-price distance trend over time
  3. Loan panels appear only when the user has active BTC-backed loans, consistent with the existing conditional dashboard panels

**Plans**: 4/4 plans complete
**UI hint**: yes

**Wave 1**

- [x] 42-01-PLAN.md — End-to-end Loans & Leverage Reports section (backend + UI tracer)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 42-02-PLAN.md — Backend tests and edge-case hardening
- [x] 42-03-PLAN.md — ViewModel tests and full-suite verification

**Gap Closure** *(completed 2026-08-12)*

- [x] 42-04-PLAN.md — Fix monthly cost compounding bug and add regression test

### Phase 43: MCP, Localization, Documentation & Verification

**Goal**: The new v0.7 metrics are AI-accessible, fully localized, documented, and verified end-to-end
**Depends on**: Phases 39, 40, 41, 42
**Requirements**: None (cross-cutting milestone completion phase)
**Success Criteria** (what must be TRUE):

  1. AI assistant can query each new v0.7 metric through MCP report tools
  2. All new user-facing strings are available in English, Portuguese (pt-BR), and Spanish
  3. `.claude/docs/reports.md` documents the new reports, UI panels, and MCP tools
  4. Full test suite is green and every new Reports tab panel is verified end-to-end with real data

**Plans**: 3/5 plans executed

**Wave 1**

- [x] 43-01-PLAN.md — Tracer: Spending Analytics MCP tool + localization + docs

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 43-02-PLAN.md — Remaining MCP tools (BTC metrics, loan reports, wealth performance)
- [x] 43-03-PLAN.md — Complete pt-BR/es localization and regenerate Designer.cs

**Wave 3** *(blocked on Wave 2 completion)*

- [ ] 43-04-PLAN.md — Complete reports.md documentation for all v0.7 categories
- [ ] 43-05-PLAN.md — Full test suite green + end-to-end UI verification

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 29. Domain, Persistence, and Active-View Filtering | v0.5 | 4/4 | Complete | 2026-07-13 |
| 30. History UI and Details Reuse | v0.5 | 3/3 | Complete | 2026-07-13 |
| 31. MCP, Localization, Documentation, and Verification | v0.5 | 5/5 | Complete | 2026-07-14 |
| 32. Factual Fixes and Cross-Page Accuracy | v0.6 | 2/2 | Complete | 2026-07-15 |
| 33. Assets Page Rewrite | v0.6 | 3/3 | Complete | 2026-07-15 |
| 34. Reports Page Update | v0.6 | 2/2 | Complete | 2026-07-15 |
| 35. Goals Page Completion | v0.6 | 3/3 | Complete | 2026-07-16 |
| 36. Fixed Expenses Page Enhancement | v0.6 | 2/2 | Complete | 2026-07-16 |
| 37. MCP Server Page Update | v0.6 | 2/2 | Complete | 2026-07-16 |
| 38. Navigation, New Pages, and Quality Assurance | v0.6 | 3/3 | Complete | 2026-07-17 |
| 39. Spending Analytics Reports & UI | v0.7 | 4/4 | Complete    | 2026-08-05 |
| 40. BTC-Denominated Metrics Reports & UI | v0.7 | 4/4 | Complete    | 2026-08-06 |
| 41. Wealth & Performance Reports & UI | v0.7 | 1/1 | Complete    | 2026-08-10 |
| 42. Loans & Leverage Reports & UI | v0.7 | 4/4 | Complete    | 2026-08-12 |
| 43. MCP, Localization, Documentation & Verification | v0.7 | 3/5 | In Progress|  |

**Total phases:** 15 (11 complete, 4 planned)  
**v0.7 plans:** 12/12 (executed)  
**v0.7 tasks:** 14/14 (Phase 40 executed; human verification pending)

---
*Last updated: 2026-08-12 after completing Phase 43 plan 43-01*
