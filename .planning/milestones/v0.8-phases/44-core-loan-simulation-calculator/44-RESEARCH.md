# Phase 44 Research: Core Loan Simulation Calculator

**Project:** Valt v0.8 — BTC Loan Simulator  
**Phase:** 44 — Core Loan Simulation Calculator  
**Researched:** 2026-08-13  
**Confidence:** HIGH

## Domain

Phase 44 delivers a pure, static interest engine in `Valt.Core` for an ephemeral BTC-backed loan what-if calculator. It is the foundation for the simulator modal (Phase 45), the cost-over-time schedule (Phase 46), prefill from existing loans (Phase 47), and the final MCP/docs pass (Phase 48). The engine must be fully testable without LiteDB, DI, or UI, and its numbers must be byte-for-byte consistent with the existing tracked-loan math in `BtcLoanDetails`.

## Key Findings

### Existing Math Conventions (Verified in Code)

- `BtcLoanDetails.CalculateAccruedInterest()` uses simple interest: `Math.Round(LoanAmount * Apr / 365 * daysSinceStart, 2)` when no snapshot exists and no fixed total debt is set.
- `BtcLoanDetails.CalculateAccruedInterestForSnapshot()` uses the same formula on the snapshot's `TotalBorrowed` and `Apr` from the snapshot's effective date to today.
- `BtcLoanDetails.DeriveAprFromFixedDebt()` annualizes a fixed total debt as: `Math.Round(interest / loanAmount * 365m / days, 6)`.
- The day-count convention is **act/365** and the money type is **decimal** rounded to 2 dp for display/totals. No `double` or `Math.Pow` is used in the existing money paths.

### Compound-Interest Decision

- Industry calculators (Unchained, Ledn, etc.) treat crypto lending as daily or monthly accrual. For a what-if simulator, the safest choice is **daily compounding via a pure `decimal` loop** over `act/365` days.
- This avoids the precision drift of casting to `double` and calling `Math.Pow`, and it guarantees that the headline total equals the sum of the daily schedule rows (the success criterion for Phase 44 and the schedule in Phase 46).
- Fees are a **one-time flat amount** added to the total repayment and do not earn interest, matching the existing `BtcLoanDetails.Fees` treatment.

### Effective APR

- The simulator should display the true annualized cost including fees: `((totalRepay - principal) / principal) * 365 / days`.
- For compound mode, the effective APR must be derived from the **actual total repayment** produced by the daily loop, not from the nominal input APR.
- Rounding for display should be 2 decimal places (e.g., `12.34%`), matching the app's standard percentage display convention.

### Liquidation Price

- Given collateral in BTC, a liquidation LTV, and total debt, the liquidation BTC price is: `totalDebt / (collateralBtc * liquidationLtv)`.
- The existing `BtcLoanDetails` does not expose this calculation, so the simulator must introduce it as a new pure-math function.

### Schedule Generation

- The engine must produce monthly anchors plus a closing row on the end date. This keeps the row count bounded (~120 rows for a 10-year loan) while preserving monthly granularity.
- Each row should expose the date, accrued interest up to that date, and cumulative total (principal + accrued interest + fees).
- The final row's cumulative total must exactly equal the headline total-repay figure.

### Scope Boundary

- Phase 44 is **Core-only**: no App-layer CQRS, no Infra persistence, no UI, no localization, no MCP.
- Downstream phases (45–48) will consume the engine; no simulator logic should be duplicated in the UI.

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Simple-interest mode diverges from `BtcLoanDetails` | Write a parity test that calls `BtcLoanDetails.CalculateAccruedInterest()` for the same principal/APR/days and asserts identical output. |
| `double`/`Math.Pow` precision drift in compound mode | Use a daily `decimal` loop over `act/365` days. |
| Schedule rows do not sum to headline total | Generate rows from the same daily loop state; accumulate unrounded and round only per-row display. |
| Edge cases (end ≤ start, same-day loan, leap-year spans) are missed | Add explicit unit tests for each. |
| UI phases later re-implement math | Keep all simulator math in `Valt.Core`; downstream phases only format/display results. |

## Recommended File Layout

```
src/Valt.Core/Modules/Assets/Simulation/
├── BtcLoanInterestMode.cs
├── BtcLoanSimulationInput.cs
├── BtcLoanSimulationResult.cs
├── LoanScheduleEntry.cs
└── BtcLoanSimulationCalculator.cs

tests/Valt.Tests/Domain/Assets/Simulation/
└── BtcLoanSimulationCalculatorTests.cs
```

## Sources

- `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs`
- `src/Valt.Core/Common/FinancialCalculator.cs`
- `.planning/research/SUMMARY.md`
- `.planning/REQUIREMENTS.md` § v0.8 Requirements (SIM-03, SIM-04)
- `.planning/phases/44-core-loan-simulation-calculator/44-CONTEXT.md`

---
*Research complete for Phase 44. Ready for planning.*
