# Phase 44: Core Loan Simulation Calculator - Context

**Gathered:** 2026-08-13
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers a pure, static interest engine in `Valt.Core` for a BTC-backed loan what-if calculator. It computes total repayment, interest/fees breakdown, liquidation price, effective fee-inclusive APR, and a monthly cost-over-time schedule for both simple and compound interest modes. The engine must match the existing `BtcLoanDetails` simple-interest math byte-for-byte and produce numbers that downstream UI phases (45-48) can render without re-implementing any math.

</domain>

<decisions>
## Implementation Decisions

### Compound Interest Frequency

- **D-01:** Compound-interest mode uses **daily compounding** via a pure `decimal` loop, not `Math.Pow` on `double`. — **Reversibility:** costly — changing this after Phase 45-48 build on the engine would require re-verifying schedule totals and parity tests across UI and tests.
- **D-02:** The daily loop uses the **act/365** day-count convention, matching the existing `BtcLoanDetails` simple-interest convention and keeping the two modes comparable in the same simulator modal.
- **D-03:** Compound interest accrues on the **principal only**; fees are a flat one-time cost added to total repayment and do not earn interest. This mirrors the treatment of origination/closing fees in the app's existing loan model.

### Effective Fee-Inclusive APR

- **D-04:** The effective APR is calculated as **annualized total cost**: `((totalRepay - principal) / principal) × 365 / days`. This includes both interest and fees in the cost and mirrors the existing `BtcLoanDetails.DeriveAprFromFixedDebt` pattern. — **Reversibility:** costly — the UI label and tests will be built around this single headline figure.
- **D-05:** The effective APR is **rounded to 2 decimal places** for display (e.g., 12.34%), matching the app's standard percentage display convention.
- **D-06:** For compound mode, the effective APR is derived from the **actual total repayment** produced by the selected mode, not from the nominal input APR. This ensures the displayed APR always reflects the true annualized cost.

### Schedule Generation Scope

- **D-07:** The Core calculator in Phase 44 **generates the monthly cost-over-time schedule rows** and returns them as part of its result. Phase 46 is responsible for rendering, not recomputing. — **Reversibility:** costly — moving schedule generation out of Core later would break the success-criterion guarantee that the headline total equals the sum of schedule rows.
- **D-08:** Schedule rows use **monthly anchors plus the final end date**: one row per calendar month plus a closing row on the end date. This keeps row counts bounded (e.g., ~120 rows for a 10-year loan) while preserving the monthly granularity requested in SIM-10.
- **D-09:** The schedule is generated for **both simple and compound modes**, keeping the UI consistent: whenever inputs are valid, the schedule panel is populated.

### the agent's Discretion

No areas were left to the agent's discretion. All discussed gray areas were decided by the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Scope & Requirements
- `.planning/ROADMAP.md` § Phase 44 — Core Loan Simulation Calculator goal, success criteria, and dependencies
- `.planning/REQUIREMENTS.md` § v0.8 Requirements — SIM-03, SIM-04, and related simulator requirements
- `.planning/research/SUMMARY.md` — Research summary including the compound-frequency conflict and recommended daily `decimal` loop approach

### Existing Loan Math & Patterns
- `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` — Existing act/365 simple-interest convention, `DeriveAprFromFixedDebt`, snapshot semantics, and liquidation math that the calculator must parity-match
- `src/Valt.Core/Common/FinancialCalculator.cs` — Precedent for a pure static calculator in `Valt.Core`

### UI Modal Precedent (for Phase 45 context, not this phase's implementation)
- `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs` — Pattern for live-recalc modal with inputs-left/results-right layout, currency selector, and prefill dropdown
- `src/Valt.UI/Views/Main/Modals/LeverageSimulator/` — Directory containing the view and modal registration pattern Phase 45 will clone

### Integration & Data Contracts
- `src/Valt.App/Modules/Assets/DTOs/AssetDTO.cs` — DTO fields available for future prefill from existing BTC-backed loan assets
- `src/Valt.UI/Extensions.cs` — Modal DI registration and factory pattern
- `src/Valt.UI/Views/ApplicationModalNames.cs` — Modal enum registration pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BtcLoanDetails` — The simulator's simple-interest mode must produce identical numbers to `CalculateAccruedInterest()` and `CalculateTotalDebt()`. Reuse the same `decimal`, act/365, 2dp rounding approach.
- `FinancialCalculator` — Static calculator precedent in `Valt.Core`; `BtcLoanSimulationCalculator` should follow the same shape (pure static class, no DI, no persistence).
- `LeverageSimulatorViewModel` — Provides the live-recalc pattern (`OnXChanged → Recalculate()`) and results formatting that Phase 45 will reuse, but Phase 44 must not depend on UI code.

### Established Patterns
- **Pure domain calculator** — Keep all math in `Valt.Core` so it can be unit-tested without LiteDB or DI. This is the same pattern as `FinancialCalculator` and the existing value-object math.
- **act/365 decimal money math** — The app uses `decimal` for fiat, `long` for sats, and `Math.Round(..., 2)` for fiat display. The simulator must never use `double` or `Math.Pow` in money paths.
- **Snapshot-based loan state** — Real loans in `BtcLoanDetails` use latest-snapshot-wins. The simulator is ephemeral and has no snapshots, but the prefill phase (Phase 47) will consume the latest snapshot from `AssetDTO`.

### Integration Points
- This phase touches only `Valt.Core` and `Valt.Core.Tests` (or `Valt.Tests.Domain`). No App/Infra/UI changes, no persistence, no migrations, no CQRS commands/queries.
- Phase 45 will consume the calculator via a new `LoanSimulatorViewModel` registered in `Extensions.cs` and invoked from `MainViewModel`.
- Phase 46 will render the schedule rows produced by the Core calculator; Phase 47 will prefill inputs from `GetAssetsQuery` results.

</code_context>

<specifics>
## Specific Ideas

No specific visual references or external examples were requested. The user directed that the calculator should reuse the app's existing conventions and the Leverage Simulator modal pattern.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within the Phase 44 boundary. Prefill semantics, UI layout, localization, MCP exposure, and documentation are explicitly scoped to Phases 45-48.

</deferred>

---

*Phase: 44-Core Loan Simulation Calculator*
*Context gathered: 2026-08-13*
