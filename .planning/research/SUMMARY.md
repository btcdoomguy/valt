# Project Research Summary

**Project:** Valt v0.8 — BTC Loan Simulator (what-if loan cost calculator modal)
**Domain:** Desktop personal-finance app feature (bitcoin-denominated), BTC-backed loan simulation
**Researched:** 2026-08-13
**Confidence:** HIGH

## Executive Summary

This is an ephemeral what-if calculator modal added to an existing, well-structured Avalonia desktop app — not a new product. All four research streams converge on the same conclusion: the work is **composition, not acquisition**. Every capability the simulator needs (interest math conventions, charting, fiat↔sats conversion, modal plumbing, prefill-from-asset) already exists in the codebase with proven precedents (`LeverageSimulator` modal, `BtcLoanDetails` domain math, `FinancialCalculator` static-calculator pattern, `GetAssetsQuery`). Zero new NuGet packages are required.

The recommended approach is a three-layer slice: a pure, static `BtcLoanSimulationCalculator` in `Valt.Core` (simple + compound interest, cost-over-time schedule, liquidation price), a `LoanSimulator` modal in `Valt.UI` that is a mechanical clone of the Leverage Simulator layout (inputs left / results right), and reuse of the existing `GetAssetsQuery` for loan prefill. No App-layer CQRS additions, no persistence, no migrations — the simulator writes nothing.

The dominant risk is **numerical inconsistency**: the simulator's simple-interest mode must be byte-for-byte identical to `BtcLoanDetails.CalculateAccruedInterest()` (`principal × apr/365 × days`, decimal, 2dp rounding) or users comparing simulation vs. tracked loans will see contradictory numbers. Secondary risks: fiat↔sats conversion using anything other than the single live price from `RatesState`, and prefill silently producing wrong parameters for snapshot-based or `FixedTotalDebt` loans. All three are preventable with parity tests and explicit conventions locked in the first phase.

## Key Findings

### Recommended Stack

No new dependencies. Everything is already present and validated in production modals. The only new code is a pure domain calculator, a modal (View + ViewModel), and NUnit tests.

**Core technologies (all existing):**
- **.NET net10.0 / LangVersion 14**: runtime for simulation math — already the solution TFM (note: AGENTS.md still says ".NET 9"; plan against the csproj)
- **Avalonia 12.1.0 (Fluent)**: modal UI — stock controls (`DatePicker`, `ComboBox`, `RadioButton` for simple/compound toggle) cover all inputs
- **CommunityToolkit.Mvvm 8.4.2**: ViewModel with `[ObservableProperty]`/`[RelayCommand]` — same reactive `OnXChanged → Recalculate()` pattern as Leverage Simulator
- **LiveChartsCore.SkiaSharpView.Avalonia 2.1.0-dev-365** (optional): cost-over-time chart — already used in sibling modals; schedule table is the primary output, chart optional
- **`BtcValue`/`FiatValue`/`FiatCurrency`, `RatesState`, `IClock`/`FakeClock`**: value objects, live rates, and deterministic time — all in-repo

**What NOT to use:** `Math.Pow`/`double` in money paths (precision drift vs. the app's decimal convention — but see gap below), NodaTime, new charting libs, LiteDB persistence of simulations, different day-count conventions, new CQRS commands/queries for a stateless calculator.

### Expected Features

Grounded against Unchained's live loan calculator (verified) and Ledn/Salt/DeFi norms. Real BTC loans are interest-only + balloon or open-ended accrual — the simulator is a cost projection, not an amortization schedule.

**Must have (table stakes / milestone contract):**
- Loan parameter inputs: collateral BTC, amount taken, liquidation LTV, start/end dates, interest rate, fees — every loan calculator starts with these
- Simple vs compound interest toggle — the only genuinely new math (maps to CeFi simple vs DeFi compound)
- Total to repay + interest/fees breakdown, in fiat AND sats — the headline output; sats display is Valt's core value
- Liquidation BTC price output — the #1 question a BTC borrower asks; nearly free given the inputs
- Live recalculation on input change + currency selector — UX parity with Leverage Simulator
- Prefill from existing BTC loan asset (milestone contract, MEDIUM complexity)
- Cost-over-time schedule, monthly rows (milestone contract, depends on engine correctness)

**Should have (differentiators, same milestone if cheap):**
- Effective APR display (fee-inclusive) — one-liner reuse of `DeriveAprFromFixedDebt` math
- Distance-to-liquidation vs. live price — small reuse of `RatesState`
- Liquidation price per schedule row (LTV creep) — the insight no basic calculator gives

**Defer / anti-features (explicitly avoid):**
- Amortizing payment schedule — real BTC loans don't amortize
- Simulated BTC price paths / "when will I get liquidated" — speculative; Reports already does custom price simulation
- Persisting simulations as assets — pollutes the real Assets ledger with hypothetical data
- Origination fee as percentage — flat amount only, matching `BtcLoanDetails.Fees`
- MCP exposure — no requirement in v0.8; decide explicitly (architecture marks it optional/cheap since the calculator is pure static)

### Architecture Approach

An ephemeral calculator modal that persists nothing, migrates nothing, adds no commands. The math core lives in `Valt.Core` as a pure static class following the `FinancialCalculator` precedent; the UI is a clone of the Leverage Simulator's shape and plumbing; prefill reuses `GetAssetsQuery` as-is (`AssetDTO` already exposes every BTC-loan field). The only MCP implication is an optional static tool calling the Core calculator directly — no DI forwarding needed.

**Major components:**
1. `BtcLoanSimulationCalculator` + `BtcLoanInterestMode` enum + input/result/`LoanScheduleEntry` records (Core, NEW) — all loan math, pure and unit-testable
2. `LoanSimulatorViewModel` + `LoanSimulatorView` (UI, NEW) — parsing/formatting only; math delegated to Core
3. Modal plumbing (UI, MODIFIED) — `ApplicationModalNames` enum, `Extensions.cs` DI + factory case, `MainViewModel` command, `MainView.axaml` menu item
4. Localization (MODIFIED) — all three `.resx` files + `language.Designer.cs` (hard project rule)
5. `LoanSimulatorTools` (Infra/MCP, NEW, optional) — static tool, own DTOs, no service forwarding

**Modal registration checklist (6 touch points; missing one = runtime failure):** enum value → DI transient → factory case → MainViewModel RelayCommand → MainView menu item → 4-file localization.

### Critical Pitfalls

1. **Interest math divergence from domain convention** — simple mode must be `principal × apr/365 × days`, decimal, 2dp rounding, exactly as `BtcLoanDetails.CalculateAccruedInterest()`; write a parity test. Compound mode must state its frequency explicitly. Lock this in the first phase before any UI exists.
2. **Wrong fiat↔sats conversion basis** — use exactly one price for the whole simulation: the current live price from `RatesState` (never per-date historical prices); display the basis ("at 1 BTC = X USD"); handle the no-price case by hiding sats outputs.
3. **Prefill traps for snapshot-based and FixedTotalDebt loans** — prefill from the effective snapshot (not setup fields); for `FixedTotalDebt` loans use `DeriveAprFromFixedDebt` with an "approximate" indicator or block with explanation. Never silently pretend a fixed-debt loan accrues daily.
4. **Precision/rounding debt** — `decimal` for fiat, `long` for sats, never `double` in money paths; accumulate schedule rows unrounded, round only for display, so rows sum to the total.
5. **"Looks done but isn't" checklist** — localization in all 4 places, all 6 modal registration points, explicit MCP decision recorded, end≤start date validation, same-day loan edge case, no-persistence invariant verified.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Core Loan Simulation Calculator
**Rationale:** Everything depends on the interest engine; nothing else can be validated until the math convention is locked. Pure static calculator = testable in isolation with no DI or DB.
**Delivers:** `BtcLoanSimulationCalculator`, `BtcLoanInterestMode` enum, input/result/schedule records in `Valt.Core/Modules/Assets/Simulation/`; NUnit test suite (no DB needed).
**Addresses:** Simple/compound interest modes, total repay, interest/fees breakdown, liquidation price math, cost-schedule generation (monthly anchors + end date, ~120-row cap).
**Avoids:** Pitfall 1 (math divergence — parity test vs `BtcLoanDetails`), precision pitfall (decimal-only, rounding policy), date edge cases (end≤start, zero-day loan, Feb 29 leap span).

### Phase 2: Simulator Modal UI (inputs + results panel)
**Rationale:** Standard, fully-precedented pattern (Leverage Simulator clone); delivers the milestone's core user-visible contract. Localization keys must exist before XAML compiles, so they land here or at the end of Phase 1.
**Delivers:** `LoanSimulatorView`/`ViewModel`, modal registration (6 touch points), Tools menu entry, live recalc, currency selector, results panel with fiat+sats, liquidation price output, conversion-basis label.
**Uses:** Avalonia stock controls, CommunityToolkit.Mvvm reactive pattern, `RatesState` for sats conversion, `CurrencySettings` default.
**Avoids:** Pitfall 2 (single live price + basis label + no-price guard), modal sizing rule, culture-tolerant numeric parsing, incomplete localization/registration checklist.

### Phase 3: Cost-Over-Time Schedule
**Rationale:** Milestone contract, but explicitly deferred behind engine correctness (FEATURES.md dependency: schedule requires the interest engine). Independent of prefill — either order works.
**Delivers:** Monthly schedule rows (date, accrued interest, cumulative total in fiat + sats) in the results panel; optional LiveChartsCore line chart; schedule liquidation-price column if cheap.
**Avoids:** Rounding-sum pitfall (accumulate unrounded), daily-row explosion for long terms (granularity + cap).

### Phase 4: Prefill from Existing BTC Loan Asset
**Rationale:** Milestone contract; MEDIUM complexity because it touches the layered loan model (setup + snapshots + fixed-debt variant). Benefits from the calculator and results already existing for visual verification.
**Delivers:** Loan dropdown ("New simulation" sentinel + BTC-loan assets via `GetAssetsQuery`), snapshot-aware field mapping, `FixedTotalDebt` handling (derived APR + "approximate" indicator, or blocked with explanation), integration tests.
**Avoids:** Pitfall 3 (snapshot/fixed-debt prefill traps) — this is the riskiest remaining item and deserves a design discussion at plan time.

### Phase 5: Integration, MCP Decision & Verification
**Rationale:** Cross-cutting completion concerns that apply once the feature works end-to-end.
**Delivers:** Explicit MCP yes/no decision (+ optional static `LoanSimulatorTools` if yes), `.claude/docs/` update, `MainViewModelModalCommandTests` launcher assertion, E2E manual verification (prefill real loan, toggle modes, verify sats vs RatesState, no-persistence DB check), language-switch walkthrough.
**Avoids:** "Looks done but isn't" checklist items (localization, MCP decision recorded, docs drift).

### Phase Ordering Rationale

- **Engine before UI before schedule before prefill** — the dependency graph is linear: every feature consumes the calculator; the schedule consumes the engine's per-period output; prefill is only verifiable once results render.
- **Schedule and prefill are mutually independent** (Phases 3/4 could swap) — order as shown puts the milestone-visible deliverable first and the riskiest data-semantics item last where it can be tested against a working UI.
- **Localization travels with the UI phase** (hard project convention; XAML won't compile with `{x:Static}` bindings to missing Designer properties).
- **This ordering front-loads the two pitfalls that are expensive if shipped wrong** (math divergence, conversion basis) into phases with unit-test verification gates.

### Research Flags

Phases likely needing deeper research/discussion during planning:
- **Phase 1:** Resolve the compound-frequency conflict between research files (see Gaps) — run `/gsd-discuss-phase` or decide explicitly at plan time.
- **Phase 4:** Prefill semantics for snapshot-based and `FixedTotalDebt` loans — layered loan model warrants a design discussion (`/gsd-discuss-phase` recommended).

Phases with standard patterns (skip research-phase):
- **Phase 2:** Mechanical clone of the Leverage Simulator; every touch point enumerated in ARCHITECTURE.md.
- **Phase 3:** Display-only consumption of the engine's schedule output; established ItemsControl/chart patterns.
- **Phase 5:** Checklist-driven verification; no unknowns.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Grounded entirely in direct codebase inspection (`Directory.Packages.props`, csproj, existing modals); zero external dependencies proposed |
| Features | HIGH | Codebase math verified; industry behavior verified against Unchained's live calculator; Ledn/Salt/DeFi norms MEDIUM (established knowledge, not re-fetched) but not load-bearing |
| Architecture | HIGH | Direct inspection of all integration points (`LeverageSimulator`, `Extensions.cs`, `ApplicationModalNames`, `AssetDTO`, MainView/MainViewModel); field-level confirmation that `AssetDTO` exposes all prefill fields |
| Pitfalls | HIGH | Grounded in the existing domain model (snapshot timeline, `FixedTotalDebt`, act/365) and documented project checklists (AGENTS.md localization/MCP/modal rules) |

**Overall confidence:** HIGH — this is an unusually well-researched feature because it is an extension of an existing codebase the researchers could inspect directly. Nearly every recommendation cites a verified precedent file.

### Gaps to Address

- **Compound-interest frequency + precision conflict:** STACK.md recommends a monthly compounding loop in pure `decimal` (avoiding `Math.Pow`); ARCHITECTURE.md and FEATURES.md recommend daily compounding via `Math.Pow` on `double` cast back to `decimal` (`P × (1 + apr/365)^days`), calling daily "the standard for crypto lending." PITFALLS.md only demands the frequency be explicit. **Resolution needed in Phase 1 planning:** pick daily (industry standard, matches FEATURES/PITFALLS) vs. monthly (STACK's precision argument), and if daily, decide whether the double-precision rounding risk is acceptable or whether a `decimal` period loop (daily or monthly) satisfies both. Recommend: daily frequency via daily `decimal` loop — same loop produces the schedule, guaranteeing total and schedule agree.
- **MCP exposure:** STACK.md says skip (no v0.8 requirement); ARCHITECTURE.md marks it optional-and-trivial (static tool, no forwarding). Make an explicit yes/no decision in Phase 5 rather than letting it happen by default.
- **Open-ended loans (null RepaymentDate):** domain supports them; simulator requires an explicit end date. Decide prefill behavior (block, or default end = start + 12 months) in Phase 4 planning.
- **AGENTS.md version drift:** docs say ".NET 9" but the TFM is `net10.0` — plan against csproj; optionally fix the doc in Phase 5.

## Sources

### Primary (HIGH confidence)
- Codebase: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` — act/365 convention, simple-interest formula, LTV/liquidation math, snapshot timeline, `FixedTotalDebt`, `DeriveAprFromFixedDebt`
- Codebase: `src/Valt.UI/Views/Main/Modals/LeverageSimulator/` — modal layout, prefill, currency, sats-conversion, live-recalc patterns
- Codebase: `src/Valt.App/Modules/Assets/DTOs/AssetDTO.cs`, `src/Valt.UI/Extensions.cs`, `src/Valt.UI/Views/ApplicationModalNames.cs`, `MainViewModel.cs`/`MainView.axaml`, `Directory.Packages.props` — integration points and versions
- Unchained commercial loans calculator (unchained.com/loans) — verified inputs/outputs, 365-day simple-interest convention, APR disclosure, liquidation-price display
- `AGENTS.md` project conventions; `.planning/PROJECT.md` v0.8 scope and snapshot-semantics decisions

### Secondary (MEDIUM confidence)
- Ledn/Salt liquidation thresholds (~70–80% LTV) and DeFi thresholds (~82–86%) — established product knowledge, not re-fetched
- Industry lending-math conventions (day-count, APR vs effective rate, compounding-frequency disclosure)

### Tertiary (LOW confidence)
- None — no load-bearing finding rests on a single unverified source

---
*Research completed: 2026-08-13*
*Ready for roadmap: yes*
