# Stack Research

**Domain:** BTC Loan Simulator (what-if calculator modal) for Valt
**Researched:** 2026-08-13
**Confidence:** HIGH

## Executive Summary

The BTC Loan Simulator requires **zero new NuGet packages**. Every capability it needs — interest math, charting, date handling, fiat↔sats conversion, MVVM plumbing — is already present and validated in the codebase. The work is composition of existing infrastructure, not stack acquisition. The only genuinely new code is a small, pure, testable simulation calculator (simple/compound interest over a date range) that belongs in `Valt.Core` as a domain service so it can be unit-tested with the existing NUnit setup and reused by reports later.

## Recommended Stack

### Core Technologies (all already in the project — no changes)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET | net10.0 (`LangVersion 14`) | Runtime for simulation math | Already the solution TFM (`Valt.UI.csproj` targets `net10.0`). `DateOnly.DayNumber`, `decimal`, and `System.Threading.Lock` cover all needs. |
| Avalonia UI | 12.1.0 (Fluent theme) | Modal UI: inputs left / results right | Matches `LeverageSimulatorView.axaml` layout exactly. `DatePicker`, `NumericUpDown`/`TextBox`, `ComboBox`, and `RadioButton` (simple/compound toggle) are stock controls. |
| CommunityToolkit.Mvvm | 8.4.2 | ViewModel with `[ObservableProperty]` / `[RelayCommand]` | Same pattern as `LeverageSimulatorViewModel`: text inputs trigger `Recalculate()` via `partial void OnXChanged`. |
| LiveChartsCore.SkiaSharpView.Avalonia | 2.1.0-dev-365 | Cost-over-time chart (debt accrual curve) | Already referenced and used in modals (`SpendingEvolution`, `PriceHistory`) and in `LoanCostChartData`. A `LineSeries<ObservablePoint>` with a date/category X axis is the established in-modal chart pattern. |

### Supporting Libraries (existing — reuse, don't re-add)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Valt.Core` value objects (`BtcValue`, `FiatValue`, `FiatCurrency`) | in-repo | Sats/fiat representation and formatting | All simulator outputs; `FiatCurrency.GetFromCode` for the currency dropdown (mirror `LoadAvailableCurrencies()` in `LeverageSimulatorViewModel`). |
| `RatesState` (Valt.UI.State) | in-repo | Live BTC price + fiat rates for fiat→sats conversion | Same conversion as the Leverage Simulator: `sats = fiat / btcPriceInCurrency * 100_000_000`. |
| `CurrencyDisplay` (Valt.Infra.Kernel) | in-repo | Axis labelers / fiat formatting | Chart Y-axis labeler, as in `LoanCostChartData.FiatLabeler`. |
| `IClock` / `FakeClock` | in-repo | Deterministic "today" for elapsed-day and schedule generation | Inject into the calculator so tests can pin the start date; default schedule runs start→end date regardless of today. |
| `IQueryDispatcher` + `GetAssetsQuery` | in-repo | Prefill from existing `AssetTypes.BtcLoan` assets | Filter by `AssetTypeId == (int)AssetTypes.BtcLoan` and map `BtcLoanDetails` fields (collateral sats, loan amount, APR, fees, liquidation LTV, start/repayment dates) into inputs — mirrors `LoadPositionsAsync()`. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| NUnit 4.4 + NSubstitute 5.3 | Unit tests for the simulation calculator | Add tests under `tests/Valt.Tests/` using a new `BtcLoanSimulationBuilder` if needed; `FakeClock` pins dates. No `IdGenerator` setup needed — the calculator is pure. |

## New Code (not new packages)

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `LoanSimulationCalculator` (domain service, static or injected) | `Valt.Core/Modules/Assets/` (or `Valt.Core/Common/`) | Given principal, APR, fees, start/end dates, and interest mode, produce: total to repay, interest portion, fee portion, and a per-period cost schedule (`IReadOnlyList<LoanCostPoint>` with date, cumulative interest, cumulative total). Pure `decimal` math — no UI or infra dependencies. |
| `BtcLoanSimulatorViewModel` + `BtcLoanSimulatorView.axaml` | `Valt.UI/Views/Main/Modals/BtcLoanSimulator/` | Clone of the Leverage Simulator layout: inputs left (collateral BTC, amount taken, liquidation LTV, dates, rate, fees, simple/compound selector), results right (totals + breakdown in fiat and sats), optional chart below results. |
| `BtcLoanSimulationChartData` | same modal folder | Follows `SpendingEvolutionChartData` pattern: `ObservableCollection<ISeries>` with a `LineSeries<ObservablePoint>` per series (total debt; optionally interest-only), category X axis of period labels, themed paints from the existing palette constants. |

## Interest Math Decisions

**Day-count convention: Actual/365 fixed — match the existing code.**
`BtcLoanDetails.CalculateAccruedInterest()` already uses `principal * apr / 365 * days` with `DateOnly.DayNumber` arithmetic. The simulator must use the same convention or its numbers will contradict the loan dashboards for identical inputs. Do not introduce Actual/360 or 30/360.

**Simple interest:** `interest = principal × apr/365 × days` (existing formula, verbatim).

**Compound interest — use a period loop in `decimal`, not `Math.Pow`:**
`decimal` has no `Pow`; `Math.Pow` is `double` and introduces rounding drift that conflicts with the app's 2-decimal fiat rounding. Instead iterate compounding periods (recommend **monthly** as the default frequency, Actual/365 within each partial period):

```csharp
decimal balance = principal;
for each period:
    balance += Round(balance * apr / 365 * daysInPeriod, 2);
interest = balance - principal;
```

This also naturally produces the cost-over-time schedule (one point per period) that the chart needs — the schedule and the compound total come from the same loop, guaranteeing they agree. For simple interest, emit the same schedule linearly.

**Fees:** flat addition to total (`principal + interest + fees`), consistent with `BtcLoanDetails.CalculateTotalDebt()`.

**Liquidation LTV in the simulator:** inputs-only context — compute and display the liquidation BTC price (`liquidationPrice = loanAmount / (liquidationLtv% × collateralBtc)`) as an informational output, reusing the LTV formulas already in `BtcLoanDetails.CalculateCurrentLtv`. No new math library needed.

## Installation

```bash
# Nothing to install.
# All dependencies are already present:
#   LiveChartsCore.SkiaSharpView.Avalonia 2.1.0-dev-365  (Valt.UI)
#   CommunityToolkit.Mvvm 8.4.2                          (Valt.UI)
#   Avalonia 12.1.0                                      (Valt.UI)
# New code only: Valt.Core calculator + Valt.UI modal + NUnit tests.
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Hand-rolled `decimal` compounding loop in `Valt.Core` | Financial math NuGet (e.g., `StringMath` is already present but string-expression oriented) | If the simulator later needs user-entered freeform formulas — not this milestone |
| LiveChartsCore line chart for cost-over-time | Plain DataGrid/text schedule only | If chart proves visually noisy for very short loans — keep the schedule list as the primary output and the chart as a toggle |
| Monthly compounding default | Daily compounding | If users compare against platforms that compound daily (e.g., some DeFi lenders) — consider exposing frequency later; out of scope for v0.8 |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `Math.Pow` / `double` for compound interest | Precision drift vs. the app's `decimal` + `Math.Round(..., 2)` fiat convention; totals would disagree with `BtcLoanDetails` | Period-by-period `decimal` loop |
| NodaTime or any date library | `DateOnly.DayNumber` already drives all loan math; a second date model invites inconsistency | `DateOnly` + `IClock` |
| New charting library (OxyPlot, ScottPlot) | LiveChartsCore is already themed, tested on Avalonia 12, and used in sibling modals | `LiveChartsCore.SkiaSharpView.Avalonia` (existing reference) |
| Persisting simulations to LiteDB | Milestone scope is a what-if calculator; persistence adds migration surface for zero stated requirement | Prefill-from-asset only; no new entities or migrations |
| Different day-count convention (Actual/360, 30/360) | Would contradict existing loan interest figures for the same loan | Actual/365 fixed, matching `CalculateAccruedInterest()` |
| New MCP tools for the simulator | v0.8 requirement list includes no MCP exposure; simulator is a pure client-side what-if | Skip; revisit if a future milestone asks for it |

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| LiveChartsCore.SkiaSharpView.Avalonia 2.1.0-dev-365 | Avalonia 12.1.0 | Dev build already validated in production modals (`SpendingEvolution`, `PriceHistory`); do not "upgrade" mid-milestone — version pinning is centralized in `Directory.Packages.props`. |
| net10.0 / LangVersion 14 | All solution projects | Note: `AGENTS.md` still says ".NET 9" — the csproj has since moved to `net10.0`; plan against the csproj. |

## Integration Points

1. **Prefill**: `GetAssetsQuery` → filter `AssetTypes.BtcLoan` → map `BtcLoanDetails` (`CollateralSats`, `LoanAmount`, `Apr`, `Fees`, `LiquidationLtv`, `LoanStartDate`, `RepaymentDate`, `CurrencyCode`) into simulator inputs — same dropdown pattern as `LeverageSimulatorViewModel.LoadPositionsAsync()` including the "New simulation" sentinel item.
2. **Sats conversion**: `RatesState.BitcoinPrice` × `RatesState.FiatRates[currency]` → `sats = fiat / priceInCurrency × 100_000_000` (exact formula from `LeverageSimulatorViewModel.Recalculate`).
3. **Modal registration**: follow the existing `IModalFactory` pattern used to open the Leverage Simulator; observe `MinWidth/MinHeight` modal conventions and custom title bar.
4. **Localization**: all new strings in `language.resx`, `language.pt-BR.resx`, `language.es.resx` + `language.Designer.cs` property.
5. **Tests**: pure calculator tests with `FakeClock`; no `DatabaseTest` needed since nothing persists.

## Sources

- Codebase inspection: `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs` (modal + prefill + sats conversion pattern) — HIGH
- Codebase inspection: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` (Actual/365 day-count, simple-interest formula, fee handling, LTV math) — HIGH
- Codebase inspection: `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionChartData.cs`, `src/Valt.UI/Views/Main/Tabs/Reports/LoanCostChartData.cs` (LiveChartsCore modal/report chart patterns) — HIGH
- `Directory.Packages.props`, `src/Valt.UI/Valt.UI.csproj` (versions, net10.0 TFM) — HIGH

---
*Stack research for: Valt v0.8 BTC Loan Simulator*
*Researched: 2026-08-13*
