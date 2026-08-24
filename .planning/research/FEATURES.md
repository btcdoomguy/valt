# Feature Research

**Domain:** BTC-backed loan simulators / what-if loan cost calculators (Valt v0.8 — BTC Loan Simulator)
**Researched:** 2026-08-13
**Confidence:** HIGH (codebase math verified in `BtcLoanDetails`; industry behavior verified against Unchained's live loan calculator; Ledn/Salt/DeFi norms from established product knowledge)

## How Real BTC-Backed Loan Products Work (Grounding)

Verified against Unchained's public loan calculator and established industry norms (Ledn, Salt, Sovryn/Morpho/Aave-style DeFi):

| Concept | Industry Behavior | Valt Relevance |
|---|---|---|
| Interest accrual | Simple daily interest on outstanding principal, 365-day year is the standard (Unchained states this explicitly; Valt already uses `LoanAmount * Apr / 365 * days`). DeFi protocols (Aave, Morpho, Sovryn) compound continuously/daily — hence the "simple or compound" toggle is the right two-mode model. | Simulator "simple" mode must reuse the exact existing domain formula; "compound" mode = `P * (1 + apr/365)^days`. |
| APR vs interest rate | Lenders quote a nominal rate plus an APR that folds in origination fees (e.g., Unchained: 14% rate / 16.21% APR). | Fees input should be a flat amount (matching existing `BtcLoanDetails.Fees`), reported separately in the breakdown. |
| Fees | Origination fee typically 0.5–2% of principal, charged once at origination. | One-time fee, added to total to repay, not accrued. |
| LTV / collateral | Starting LTV 25–50% (Unchained uses 200% collateral-to-principal = 50% LTV). Liquidation triggers ~70–90% LTV (Ledn margin call ~70% / liquidation ~80%; DeFi liquidation thresholds ~82–86%). | Liquidation LTV input drives the "liquidation BTC price" output — the most decision-relevant number for borrowers. |
| Payment structure | Interest-only with balloon principal at term end (CeFi), or open-ended with accrued debt (DeFi, HodlHodl-style fixed debt). | Simulator is a cost projection, not an amortization schedule: total repay = principal + accrued interest + fees at end date. |
| Loan term | Fixed terms 6–36 months typical; start/end dates define accrual window. | Start date + end date inputs; open-ended loans (null repayment date) exist in domain but simulator needs an explicit end date. |

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist in any loan cost calculator. Missing these = the simulator feels broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Loan parameter inputs (collateral BTC, amount taken, interest rate, fees, start date, end date) | Every loan calculator (Ledn, Unchained, Salt) starts with these | LOW | Mirror Leverage Simulator input column; reuse existing text-parsing pattern (`TryParseDecimal` with culture fallback). Dates via `CalendarDatePicker` as in other Valt modals. |
| Liquidation LTV input → liquidation BTC price output | The #1 question a BTC borrower asks: "at what BTC price do I get liquidated?" Liquidation price = `TotalDebt / (CollateralBtc * LiquidationLtv)` | LOW | Pure arithmetic over existing inputs; no new domain math required. Show alongside distance-to-liquidation from current price (pattern already exists in `CalculateDistanceToLiquidation`). |
| Simple vs compound interest mode | Explicitly scoped in the milestone; maps to CeFi (simple, 365-day) vs DeFi (compound) models | LOW | Simple: `P * apr / 365 * days` (identical to existing `BtcLoanDetails.CalculateAccruedInterest`). Compound: daily compounding `P * (1 + apr/365)^days - P`. Pure functions, easily unit-tested. |
| Total to repay (principal + interest + fees) | The headline output of every loan calculator | LOW | `LoanAmount + Interest + Fees` — matches `CalculateTotalDebt` semantics. |
| Interest/fees breakdown | Unchained separates "finance charge" (interest + origination fee) from principal; users want to see cost composition | LOW | Three-line breakdown: principal, accrued interest, fees. |
| Fiat AND sats values for all outputs | Core Value of Valt: "users always know where they stand in sats." Existing Leverage Simulator already shows fiat + sats side by side | LOW | Sats conversion: `fiatValue / btcPrice * 100_000_000` using `RatesState.BitcoinPrice` (same as Leverage Simulator lines 250-252). |
| Live recalculation as inputs change | Leverage Simulator recalculates on every `OnXxxChanged`; users will expect the same | LOW | Follow the existing `partial void OnXTextChanged(...) => Recalculate()` pattern exactly. |
| Prefill from existing BTC loan asset | Milestone requirement; mirrors the position dropdown in Leverage Simulator (`AvailablePositions` + `IsNewSimulation` sentinel) | MEDIUM | Query `GetAssetsQuery`, filter `AssetTypes.BtcLoan`, map `BtcLoanDetails` fields (collateral sats, loan amount, APR, fees, liquidation LTV, start/repayment dates) into input fields. Same try/catch fallback as `LoadPositionsAsync`. |
| Currency selector | Fiat values must render in the user's configured fiat; Leverage Simulator already does this | LOW | Reuse `AvailableCurrencies` + `CurrencySettings.MainFiatCurrency` pattern verbatim. |

### Differentiators (Competitive Advantage)

Not required for v0.8, but valuable and aligned with Valt's sats-denominated worldview.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Cost-over-time schedule table | Milestone requirement. Shows debt accrual per period (monthly rows) until end date — answers "how much do I owe if I repay in month N?" | MEDIUM | Generate rows: date, accrued interest, cumulative total (fiat + sats). A simple `DataGrid`/ItemsControl; no chart library needed for v0.8. Monthly granularity is enough; daily rows would flood the UI. |
| Liquidation price per schedule row (LTV creep) | Shows how liquidation price RISES over time as interest accrues — the insight no basic calculator gives: your safety margin decays even if BTC price is flat | MEDIUM | For each schedule row, recompute liquidation price from that row's total debt. High value for risk-aware bitcoiners; natural extension of the schedule. |
| Effective APR display (fee-inclusive) | Unchained shows 14% rate / 16.21% APR. Folding the one-time fee into an annualized effective rate makes short loans' true cost visible | LOW | `(totalCost / principal) * 365 / days` annualized — `BtcLoanDetails.DeriveAprFromFixedDebt` already implements this exact math and can be reused/mirrored. |
| Compare against live price: distance to liquidation | "At current price you are X% away from liquidation at end of term" — ties simulation to reality via `RatesState` | LOW | Reuse `RatesState.BitcoinPrice`; same concept as existing `CalculateDistanceToLiquidation`. |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Amortizing payment schedule (periodic principal payments) | Standard in mortgage calculators | Real BTC loans are interest-only + balloon or open-ended accrual; amortization math adds scheduling complexity for a structure users don't actually have | Keep total-debt projection; the cost-over-time schedule already answers "what if I repay early" |
| Simulated BTC price path / volatility scenarios (liquidation "when" projection) | Users want "when will I get liquidated" | Requires price forecasting — speculative, out of scope, and Valt already has custom price simulation in Reports for this | Output liquidation *price* (a fact), not liquidation *time* (a prediction) |
| Persisting simulations as assets | "Save this simulation" feels natural | Pollutes the Assets ledger with hypothetical data; conflicts with the real-loan tracking semantics and snapshot timeline | Simulator is ephemeral (like Leverage Simulator); prefill-from-asset is the bridge, not save-as-asset |
| Fixed total debt mode (HodlHodl-style) in the simulator | Domain supports `FixedTotalDebt` | A fixed debt has nothing to simulate — total is known up front; compound/simple toggle becomes meaningless | If prefilled loan has `FixedTotalDebt`, show it directly as the total and disable accrual controls (or skip prefill) |
| Origination fee as percentage | Some lenders quote fee % | Flat fee matches the existing `BtcLoanDetails.Fees` decimal; two input modes = validation ambiguity | Flat fiat amount only; users can compute % themselves |

## Feature Dependencies

```
Sats/fiat conversion ──requires──> RatesState.BitcoinPrice (live rate) [EXISTS]
Simple interest mode ──requires──> Apr/365 daily accrual formula [EXISTS in BtcLoanDetails]
Compound interest mode ──requires──> (nothing — new pure function)
Liquidation price output ──requires──> collateral + liquidation LTV + total debt [all inputs]
Cost-over-time schedule ──requires──> simple/compound interest engine + end date
Schedule liquidation column ──enhances──> cost-over-time schedule ──requires──> liquidation price math
Prefill from loan asset ──requires──> GetAssetsQuery + BtcLoanDetails DTO fields [EXISTS]
Effective APR row ──enhances──> results panel ──reuses──> DeriveAprFromFixedDebt math [EXISTS]
All UI strings ──requires──> language.resx + pt-BR + es (3 files + Designer) [PROJECT CONVENTION]
```

### Dependency Notes

- **Everything depends on the interest engine, not the other way around.** Build the pure calculation functions first (they're testable in isolation with NUnit, no DI needed) — UI, schedule, and prefill all consume them.
- **Prefill depends on existing `AssetDTO` exposure of `BtcLoanDetails` fields.** Verify `GetAssetsQuery` DTO carries collateral sats, APR, fees, liquidation LTV, and dates (Leverage Simulator reads `EntryPrice`, `Collateral`, `Leverage` etc. from the same DTO, so the pattern exists — confirm loan fields are present; if not, that's a small Valt.App DTO extension, which is why prefill is MEDIUM not LOW).
- **Sats display depends on a live BTC price in `RatesState`.** The Leverage Simulator already handles the null/offline case; reuse the same guard.
- **Localization is a hard project convention** — every new string lands in three resx files plus `language.Designer.cs`; factor this into any phase that adds UI text.

## MVP Definition

### Launch With (v0.8)

- [ ] Input panel: collateral BTC, amount taken, liquidation LTV, start date, interest rate, fees, end date — the milestone contract
- [ ] Simple/compound toggle — the milestone contract; the only genuinely new math
- [ ] Results panel: total to repay, interest/fees breakdown, fiat + sats — the milestone contract
- [ ] Liquidation BTC price output — table stakes for any BTC borrower; nearly free given the inputs
- [ ] Live recalc + currency selector + modal layout mirroring Leverage Simulator — UX consistency with the existing tool

### Add After Core Works (same milestone, later phase)

- [ ] Cost-over-time schedule (monthly rows, fiat + sats) — milestone contract, but depends on the interest engine being correct first
- [ ] Prefill from existing BTC loan asset — milestone contract; independent of the schedule, can ship in either order
- [ ] Effective APR display — one-liner reuse of `DeriveAprFromFixedDebt` math

### Future Consideration (v2+)

- [ ] Liquidation price per schedule row — real differentiator, but only valuable once the schedule exists and is trusted
- [ ] Distance-to-liquidation vs live price — small, but schedule work should land first
- [ ] "What-if BTC price" slider — defer: Reports already has custom price simulation; avoid duplicating speculative-price UX in two places

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Loan inputs + live recalc | HIGH | LOW | P1 |
| Simple interest total repay | HIGH | LOW (reuse existing formula) | P1 |
| Compound interest mode | HIGH | LOW | P1 |
| Fiat + sats results breakdown | HIGH | LOW | P1 |
| Liquidation price output | HIGH | LOW | P1 |
| Cost-over-time schedule | HIGH | MEDIUM | P2 |
| Prefill from existing loan | MEDIUM | MEDIUM | P2 |
| Effective APR display | MEDIUM | LOW | P2 |
| Per-row liquidation price | MEDIUM | MEDIUM | P3 |
| Distance to liquidation vs live | MEDIUM | LOW | P3 |

## Competitor Feature Analysis

| Feature | Unchained (CeFi) | Ledn / Salt (CeFi) | DeFi (Aave/Morpho/Sovryn) | Valt Simulator Approach |
|---------|------------------|--------------------|--------------------------|-------------------------|
| Interest model | Simple daily, 365-day year, interest-only | Simple daily, some fixed-term fixed-cost | Compound (continuous/daily) | Both modes via toggle — covers CeFi and DeFi users |
| Key outputs | Monthly interest payment, final payment, origination fee, finance charge, APR | Total repay amount, LTV, liquidation price | Health factor, liquidation price, accrued debt | Total repay, interest/fees split, liquidation price, schedule — superset for a personal tool |
| Liquidation display | "First CTP violation price" (= liquidation price) | Margin call price + liquidation price | Liquidation price / health factor | Liquidation BTC price from liquidation LTV input; matches domain `LiquidationLtv` semantics |
| Fees | Origination fee folded into APR disclosure | Origination/processing fees 1–2% | Usually none (protocol rates only) | Flat fee input matching `BtcLoanDetails.Fees` |
| Schedule | 12-payment interest-only term | Term-based | Continuous accrual graph | Monthly cost-over-time rows until end date |

## Sources

- Unchained commercial loans calculator (unchained.com/loans) — verified inputs/outputs, 365-day simple-interest convention, APR-vs-rate disclosure, CTP/liquidation-price display — HIGH confidence
- Valt codebase: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` — existing accrual formula, LTV/liquidation/fixed-debt semantics — HIGH confidence
- Valt codebase: `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs` — layout, prefill, currency, sats-conversion, live-recalc patterns to mirror — HIGH confidence
- Industry norms for Ledn/Salt liquidation thresholds (~70–80% margin-call/liquidation LTV) and DeFi liquidation thresholds (~82–86%) — MEDIUM confidence (established product knowledge, not re-fetched this session)

---
*Feature research for: Valt v0.8 BTC Loan Simulator*
*Researched: 2026-08-13*
