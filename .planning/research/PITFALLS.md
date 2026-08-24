# Pitfalls Research

**Domain:** BTC Loan Simulator (what-if loan cost calculator) added to Valt, a bitcoin-denominated personal budget desktop app (.NET 9, Avalonia, LiteDB, CQRS, MVVM)
**Researched:** 2026-08-13
**Confidence:** HIGH — pitfalls are grounded in the existing codebase (`BtcLoanDetails`, `LeverageSimulator`, `RatesState`, AGENTS.md checklists) and well-known interest-math failure modes.

## Critical Pitfalls

### Pitfall 1: Simulator interest math diverges from the domain's existing convention

**What goes wrong:**
The simulator computes simple interest differently from `BtcLoanDetails.CalculateAccruedInterest()` (which uses `LoanAmount * Apr / 365 * days`, act/365, decimal, rounded to 2). Users compare the simulator's "total to repay" against their tracked loan's current debt and see mismatched numbers, concluding the app is buggy. The new compound mode adds a second failure surface: compounding frequency (daily? monthly? annual?) is left implicit, and each choice yields materially different results.

**Why it happens:**
The developer writes fresh interest code in the simulator ViewModel instead of extracting/reusing the domain formula, and treats "compound interest" as self-evident when it always requires a stated frequency. APR vs. effective-annual-rate confusion compounds this: `DeriveAprFromFixedDebt` in the domain produces a simple annualized rate, not an effective compounded rate.

**How to avoid:**
- Put the interest engine in `Valt.Core` (or `Valt.App`) as a pure, testable calculator — never in the ViewModel.
- Simple mode must be byte-for-byte the existing convention: `principal * apr / 365 * days`, `decimal`, `Math.Round(..., 2)`. Write a parity test asserting simulator simple-mode output equals `BtcLoanDetails.CalculateAccruedInterest()` for the same inputs.
- Compound mode: pick one frequency explicitly (daily is the standard for BTC lending platforms), state it in the UI label and docs, and use `principal * (1 + apr/365)^days`.
- Document in the UI that APR input is a nominal annual rate over a 365-day year.

**Warning signs:**
- Interest formula appears in a `*ViewModel.cs` file.
- The word "compound" appears in code with no frequency constant.
- Tests exist for the UI but no unit tests compare simulator output to `BtcLoanDetails`.

**Phase to address:**
The core calculator phase (first phase of v0.8) — the convention must be locked before any UI exists.

---

### Pitfall 2: Fiat↔sats conversion uses the wrong price date (or mixed dates)

**What goes wrong:**
The results panel shows "total to repay in sats." A naive implementation converts fiat to sats using the historical BTC price at the loan start date, or — worse — uses start-date price for some rows and live price for others in the cost-over-time schedule. Sats values then fluctuate nonsensically across the schedule or disagree with the rest of the app, which denominates everything at the current live price via `RatesState`.

**Why it happens:**
"Cost over time" sounds like it should use historical prices, and the app has a price database, so it feels natural to look up per-date prices. But the simulator is a *what-if* tool about the future/present: the user's mental model (and the app's core value — "where you stand in sats" — per PROJECT.md) is current-price denomination.

**How to avoid:**
- Use exactly one price for the whole simulation: the current live price from `RatesState` for the selected currency (same source `LeverageSimulatorViewModel` uses).
- Show the price used ("at 1 BTC = X USD") near the sats outputs so the basis is explicit.
- Handle the offline/no-price case: disable sats outputs or show a placeholder rather than dividing by zero or using a stale cached price silently.

**Warning signs:**
- Schedule rows show sats values that don't move proportionally to fiat values.
- Sats totals differ from what the main Assets/Reports screens imply for the same fiat amount.
- Code queries the price database with a date parameter inside the simulator.

**Phase to address:**
The results-panel phase — conversion basis must be decided before the schedule UI is built.

---

### Pitfall 3: Prefill from an existing loan silently produces wrong parameters for fixed-debt or snapshot-based loans

**What goes wrong:**
"Load existing loan" reads a `BtcLoanDetails` and fills the form. Two traps: (a) HodlHodl-style loans with `FixedTotalDebt` do NOT accrue daily interest — running them through the APR simulator yields a different total than the loan actually tracks; (b) loans with state snapshots have their real current state (borrowed principal, accrued interest, fees, LTVs) in the *latest snapshot* (`GetEffectiveSnapshot()` / `MaxBy(s => s.EffectiveDate)`), not in the immutable setup fields — prefilling from setup values simulates a loan that no longer exists.

**Why it happens:**
The v0.5-era loan model is layered (setup + snapshot timeline + fixed-debt variant), and a prefill feature written against the setup fields "works" in the simple demo case and fails quietly for migrated or updated loans.

**How to avoid:**
- Prefill from the effective snapshot values (fall back to setup when no snapshots), matching the decision already logged in PROJECT.md: "Latest recorded loan state snapshot wins for calculations."
- For `FixedTotalDebt` loans: either prefill a derived APR via the existing `BtcLoanDetails.DeriveAprFromFixedDebt` (and show an "approximate" indicator), or block prefill with an explanatory message. Never silently pretend the loan accrues daily.
- Add a prefill integration test using a loan with snapshots and one with `FixedTotalDebt`.

**Warning signs:**
- Prefill code accesses `LoanAmount`/`Apr`/`Fees` directly without checking `Snapshots` or `HasFixedTotalDebt`.
- Simulated total for a freshly-loaded real loan doesn't match the Assets tab's current debt figure.

**Phase to address:**
The prefill phase (after the calculator and results exist) — flag for deeper design discussion at plan time.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Interest math in the ViewModel | Fast to wire up | Divergence from domain, untestable, duplicated when MCP/reports want it later | Never |
| `double` for intermediate interest math | No rounding thought needed | Binary float drift (0.1+0.2), schedule rows that don't sum to the total | Never — use `decimal` for fiat, `long` sats |
| Rounding each schedule row to 2dp and summing the rounded rows | Rows display cleanly | Sum of rows ≠ displayed total by a cent or two | Acceptable only if the total is computed as the sum of rounded rows AND labeled as such; better: accumulate unrounded, round only for display |
| Reusing `CreateBtcLoanCommand` to "save" a simulation | Reuses CQRS plumbing | Simulator pollutes the user's real Assets data with what-if entries | Never — the simulator is read-only/what-if; if saving is wanted later, add an explicit "create loan from simulation" action |
| Hardcoded 365-day year inline in formulas | Simple | Inconsistent with any future convention change; leap-year confusion | Only inside a single named constant in the calculator |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| `RatesState` (live prices) | Subscribing to live price updates and re-rendering the whole schedule every 30s, or capturing the price once and letting it go stale silently | Capture price at calculation time; show it in the UI; recalc only on explicit user action (button/input change) |
| `IQueryDispatcher` for prefill | Injecting `IAssetQueries` / repository directly in the ViewModel, bypassing the App layer | Use `IQueryDispatcher` with a `GetAssetsQuery`-style query, per AGENTS.md CQRS rules; filter to `BtcLoanDetails` assets only |
| LiteDB | Persisting simulation inputs/results "for convenience" | The milestone scope is a standalone what-if modal — no persistence. If session persistence is desired, it's a separate, explicitly-scoped feature |
| Existing `LeverageSimulator` modal | Copy-pasting the ViewModel wholesale, inheriting position/leverage concepts that don't apply to loans | Mirror the layout (inputs left, results right) and DI/modal registration pattern, but write a fresh ViewModel with loan semantics |
| `CurrencySettings` | Hardcoding USD for the simulator | Follow `LeverageSimulatorViewModel`: dropdown of `FiatCurrency` defaulting to `CurrencySettings.MainFiatCurrency` |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Daily-granularity schedule over long end dates | UI hangs generating/binding tens of thousands of rows for a 30-year end date | Cap schedule length (e.g., max 10 years / 3650 rows); aggregate to weekly/monthly rows beyond a threshold | End dates > ~5 years out |
| Recalculating on every keystroke of every input | Laggy input, flickering results | Debounce or calculate on explicit action/focus-loss, as the Leverage Simulator does | Immediately on low-end machines |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Exposing the simulator via MCP without an explicit decision | AI assistant gets an undocumented, unreviewed tool surface | Apply the AGENTS.md MCP Impact Checklist deliberately: decide yes/no; if yes, add a read-only tool with its own DTOs and register forwarding in `McpServerService.ForwardServicesFromMainApp()` |
| Parsing numeric input with `double.Parse`/`decimal.Parse` default culture | pt-BR users typing "0,12" get `FormatException` or silently wrong values (comma = thousands separator in en-US) | Parse with the current UI culture, as existing modals do; add tests for `,` and `.` decimal separators |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No validation of end date ≤ start date | Compound formula with negative exponent or empty schedule; confusing zero results | Validate in the command/query validator: end date must be after start date; show inline error |
| Zero-day loan (start == end) | Division by zero or zero-interest result that looks like a bug | Accept it explicitly: total = principal + fees, interest = 0; test this case |
| Missing modal `MinWidth`/`MinHeight` | Content cut off on small screens (violates AGENTS.md modal rule) | Set `MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight` matching `d:DesignWidth`/`d:DesignHeight`, as `LeverageSimulatorView` does (700×615) |
| Compound mode unlabeled by frequency | User can't reconcile results with their platform's quote | Label it: "Compound (daily, act/365)" |
| Sats values without the conversion basis | User can't tell why sats ≠ their exchange's quote | Display "at current price 1 BTC = X <currency>" next to sats outputs |

## "Looks Done But Isn't" Checklist

- [ ] **Localization:** All new strings exist in ALL FOUR places — `language.resx`, `language.pt-BR.resx`, `language.es.resx`, AND a static property in `language.Designer.cs` (per AGENTS.md). Missing pt-BR/es keys fall back to English silently; a missing Designer property fails the build. Verify by switching the app language and walking the modal.
- [ ] **Menu/registration:** Modal added to `ApplicationModalNames` (next free enum value), `IModalFactory` mapping in `Extensions.cs`, transient ViewModel registration, and a menu entry in `MainView.axaml` — the Leverage Simulator shows all four touch points (enum 24, lines ~148/~266 of Extensions.cs, menu + F11 keybinding).
- [ ] **MCP decision recorded:** Either a simulator MCP tool exists (with own DTOs + service forwarding) or the decision to skip is explicit in the phase artifacts — per the AGENTS.md MCP Impact Checklist.
- [ ] **Docs:** `.claude/docs/assets.md` (or a new simulator doc) updated; module docs list in AGENTS.md keeps the simulator discoverable.
- [ ] **Simple/compound parity:** Simple-mode output matches `BtcLoanDetails` math for identical inputs; compound mode documented with frequency.
- [ ] **Date validation:** End ≤ start rejected; same-day loan returns principal + fees; Feb 29 spanning period tested (act/365 counts the leap day — confirm this is the intended convention and test it).
- [ ] **No-persistence invariant:** Opening, simulating, and closing the modal writes nothing to LiteDB — verified by test or manual DB diff.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Divergent interest math shipped | MEDIUM | Extract a single `LoanCostCalculator` in Core; make both simulator and any future consumers call it; add parity tests |
| Wrong conversion basis shipped | LOW | Swap price source to `RatesState` live price; add basis label; no data migration needed (nothing persisted) |
| Bad prefill for fixed-debt loans | LOW | Add `HasFixedTotalDebt` guard + derived-APR path; add regression test |
| Missing pt-BR/es strings | LOW | Add keys to both resx files; no code change needed beyond Designer regen |
| Accidental persistence of simulations | MEDIUM | Delete the write path; add cleanup query for polluted asset rows if any shipped |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Interest math divergence (P1) | Phase: core loan-cost calculator (first v0.8 phase) | Unit tests: simple parity vs `BtcLoanDetails`, compound vs hand-computed daily-compound values, leap-year case |
| Conversion timing (P2) | Phase: results panel / schedule | Test: single price captured; sats = fiat/price×1e8 within rounding; no-price state handled |
| Prefill traps (P3) | Phase: load-existing-loan prefill | Integration tests with snapshot-based and `FixedTotalDebt` loans |
| Date edge cases | Phase: input validation (with calculator phase) | Validator tests: end≤start rejected, same-day ok, leap span |
| Precision (decimal/long) | Phase: core calculator | Tests: schedule sums equal total; no `double` in money paths (code review + test) |
| Localization/Designer | Final integration phase | Language switch walkthrough; build enforces Designer |
| MCP/docs checklist | Final integration phase | Explicit decision recorded; docs updated; checklist item in phase verification |
| Modal sizing/UX | UI phase | Manual check at 1024×768; matches AGENTS.md modal rule |

## Sources

- Codebase inspection: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` (act/365 simple interest, `DeriveAprFromFixedDebt`, snapshot timeline, `FixedTotalDebt`) — HIGH confidence
- Codebase inspection: `src/Valt.UI/Views/Main/Modals/LeverageSimulator/` (modal pattern, sizing, DI, currency dropdown, `RatesState` usage) — HIGH confidence
- `AGENTS.md` — localization 4-file checklist, MCP Impact Checklist, modal MinWidth/MinHeight rule, CQRS dispatcher rules — HIGH confidence
- `.planning/PROJECT.md` — v0.8 scope, key decisions on loan snapshot semantics — HIGH confidence
- Standard lending-math conventions (day-count act/365, APR vs effective rate, compounding frequency disclosure) — MEDIUM/HIGH confidence, industry-standard

---
*Pitfalls research for: BTC Loan Simulator (v0.8) in Valt*
*Researched: 2026-08-13*
