# Phase 44: Core Loan Simulation Calculator - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-13
**Phase:** 44-Core Loan Simulation Calculator
**Areas discussed:** Compound frequency, Effective APR, Schedule scope

---

## Compound frequency

| Option | Description | Selected |
|--------|-------------|----------|
| Daily compounding | Accrue interest every day using a decimal loop. Matches the crypto-lending convention and makes schedule rows the same calculation as the total. | ✓ |
| Monthly compounding | Accrue interest once per calendar month using a decimal loop. Avoids double-precision risk but does not match the daily convention. | |
| Daily Math.Pow | Use `Math.Pow(1 + apr/365, days)` with double cast. Daily formula but introduces floating-point rounding differences. | |

**User's choice:** Daily compounding
**Notes:** Follow-up confirmed act/365 day-count convention and principal-only compounding balance. Fees do not earn interest.

---

## Effective APR

| Option | Description | Selected |
|--------|-------------|----------|
| Annualize total cost | `((totalRepay - principal) / principal) × 365 / days`. Includes both interest and fees; mirrors `DeriveAprFromFixedDebt`. | ✓ |
| Separate fees | Show base input APR and add a separate fees-in-amount figure; no single headline effective APR. | |
| Interest only | Annualize only interest; ignore fees in the APR percentage. | |

**User's choice:** Annualize total cost
**Notes:** Follow-up confirmed 2 decimal places for display and that the effective APR for compound mode must reflect the actual compounded total repayment, not the nominal input APR.

---

## Schedule scope

| Option | Description | Selected |
|--------|-------------|----------|
| Core generates schedule | Core calculator returns the full schedule list; Phase 46 only renders. Guarantees total equals sum of rows. | ✓ |
| Phase 46 generates schedule | Core returns only totals; Phase 46 independently computes schedule. Cleaner boundary but risk of mismatch. | |
| Core returns accrual points | Core returns compact accrual points; Phase 46 aggregates. Middle ground with extra transformation layer. | |

**User's choice:** Core generates schedule
**Notes:** Follow-up confirmed monthly anchors plus end-date row granularity, and that the schedule is generated for both simple and compound modes.

---

## the agent's Discretion

None. All discussed areas were explicitly decided by the user.

## Deferred Ideas

None. Discussion stayed within the Phase 44 boundary.
