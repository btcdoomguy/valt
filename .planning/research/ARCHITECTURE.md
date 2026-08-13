# Architecture Research

**Domain:** BTC Loan Simulator feature (what-if calculator) in the Valt desktop app
**Researched:** 2026-08-13
**Confidence:** HIGH — based on direct codebase inspection (Leverage Simulator modal, BtcLoanDetails domain, modal plumbing, localization, MCP, tests)

## Standard Architecture

### System Overview

The feature is an **ephemeral calculator modal** — it persists nothing, migrates nothing, and adds no commands. The only read-path dependency is the existing `GetAssetsQuery` for loan prefill. The calculation core belongs in `Valt.Core` (pure, static), following the established precedent of `FinancialCalculator`, `BtcPriceCalculator`, and `InstallmentDateCalculator`.

```
┌─────────────────────────────────────────────────────────────────┐
│                           Valt.UI                                │
│  Tools menu ──► MainViewModel.OpenLoanSimulator (RelayCommand)   │
│                          │                                       │
│              IModalLauncher.ShowAsync(ApplicationModalNames.     │
│                              LoanSimulator = 42, Window)         │
│                          │                                       │
│   LoanSimulatorView (axaml) ◄──► LoanSimulatorViewModel          │
│     inputs left │ results right     (ValtModalViewModel)         │
│                          │           │                           │
│         RatesState (BTC/fiat rates)  IQueryDispatcher            │
└──────────────────────────┼───────────┼───────────────────────────┘
                           │           │
┌──────────────────────────┼───────────┼───────────────────────────┤
│         Valt.App         │           │  Valt.Core                │
│   GetAssetsQuery ────────┘           │  BtcLoanSimulation-       │
│   (AssetDTO already has every        │  Calculator (static,      │
│    BTC-loan field for prefill)       │  pure math + schedule)    │
│                                      │  BtcLoanInterestMode enum │
└──────────────────────────────────────┼───────────────────────────┘
                                       │
┌──────────────────────────────────────┼───────────────────────────┐
│  Valt.Infra (MCP)                    ▼                           │
│  LoanSimulatorTools.SimulateBtcLoan ──► Core calculator directly │
│  (static tool, NO DI forwarding needed — calculator is pure)     │
└──────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Implementation |
|-----------|----------------|----------------|
| `BtcLoanSimulationCalculator` (Core, NEW) | Simple/compound interest math, total debt, fees, cost-over-time schedule, liquidation price | Static class, mirrors `FinancialCalculator` precedent; daily accrual (`APR/365`) consistent with `BtcLoanDetails.CalculateAccruedInterest` |
| `BtcLoanInterestMode` (Core, NEW) | Simple vs Compound selection | Enum in `Valt.Core/Modules/Assets/` |
| `LoanScheduleEntry` (Core, NEW) | One row of the schedule (date, accrued interest, total debt) | Immutable record |
| `LoanSimulatorViewModel` (UI, NEW) | Text inputs, parsing, dropdowns (currency, loan prefill), formatted results, `HasResults` state | `ValtModalViewModel` + CommunityToolkit.Mvvm, clone of `LeverageSimulatorViewModel` shape |
| `LoanSimulatorView` (UI, NEW) | Two-column layout (inputs left / results right + schedule list) | Clone of `LeverageSimulatorView.axaml` grid (`300, 20, *`) |
| `GetAssetsQuery` (App, EXISTING) | Prefill source | Reused as-is; filter `AssetTypeId == (int)AssetTypes.BtcLoan` |
| `LoanSimulatorTools` (Infra/MCP, NEW, optional) | Expose simulation to AI assistants | Static `[McpServerTool]` calling Core calculator; no service forwarding |

## Recommended Project Structure

```
src/
├── Valt.Core/
│   └── Modules/Assets/
│       ├── BtcLoanInterestMode.cs              # NEW: Simple | Compound enum
│       └── Simulation/                          # NEW folder (or Common/ to match FinancialCalculator)
│           ├── BtcLoanSimulationCalculator.cs   # NEW: static math entry points
│           ├── BtcLoanSimulationInput.cs        # NEW: input record
│           ├── BtcLoanSimulationResult.cs       # NEW: totals + breakdown record
│           └── LoanScheduleEntry.cs             # NEW: schedule row record
├── Valt.UI/
│   ├── Views/ApplicationModalNames.cs           # MODIFIED: + LoanSimulator = 42
│   ├── Extensions.cs                            # MODIFIED: + AddTransient + factory case
│   ├── Lang/language.resx / .pt-BR.resx / .es.resx / .Designer.cs  # MODIFIED: Menu_LoanSimulator + LoanSimulator_* keys
│   └── Views/Main/
│       ├── MainViewModel.cs                     # MODIFIED: + OpenLoanSimulator RelayCommand
│       ├── MainView.axaml                       # MODIFIED: + Tools MenuItem
│       └── Modals/LoanSimulator/                # NEW folder
│           ├── LoanSimulatorView.axaml
│           ├── LoanSimulatorView.axaml.cs
│           └── LoanSimulatorViewModel.cs
└── Valt.Infra/Mcp/Tools/
    └── LoanSimulatorTools.cs                    # NEW (optional): [McpServerToolType]

tests/Valt.Tests/
├── Domain/Assets/Simulation/
│   └── BtcLoanSimulationCalculatorTests.cs      # NEW: NUnit unit tests (no DB)
└── UI/ViewModels/MainViewModelModalCommandTests.cs  # MODIFIED: + launcher assertion
```

### Structure Rationale

- **Core owns the math:** `LeverageSimulatorViewModel` already delegates to `LeveragedPositionDetails` in Core rather than doing math in the VM — the loan simulator must follow the same rule. Pure static calculator = trivially unit-testable, reusable by MCP, and consistent with the "no external deps" Core constraint.
- **No App-layer changes:** The feature writes nothing. `AssetDTO` already exposes every prefill field (`CollateralSats`, `LoanAmount`, `Apr`, `Fees`, `LoanStartDate`, `RepaymentDate`, `LiquidationLtv`, `CurrencyCode`, `FixedTotalDebt`). Adding a dedicated query would duplicate `GetAssetsQuery` for zero gain.
- **UI is a mechanical clone of the Leverage Simulator:** same base class, same design-time constructor pattern, same `OnBindParameterAsync` load sequence (currencies → loans → defaults), same `partial void OnXChanged → Recalculate()` reactive pattern, same `IModalLauncher` plumbing.

## Architectural Patterns

### Pattern 1: Pure Core Calculator with Record In/Out

**What:** All loan math in a static class taking an input record and returning a result record.
**When to use:** Always, for what-if/simulation logic in this codebase (precedent: `FinancialCalculator`, `BtcPriceCalculator`).
**Trade-offs:** Slightly more types than inlining in the VM; pays off in testability and MCP reuse.

**Example:**
```csharp
// Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs
public static class BtcLoanSimulationCalculator
{
    public static BtcLoanSimulationResult Simulate(BtcLoanSimulationInput input)
    {
        var days = input.EndDate.DayNumber - input.StartDate.DayNumber;
        if (days <= 0) return BtcLoanSimulationResult.Empty(input);

        var interest = input.Mode == BtcLoanInterestMode.Simple
            ? Math.Round(input.Principal * input.Apr / 365m * days, 2)
            : Math.Round(input.Principal * ((decimal)Math.Pow((double)(1m + input.Apr / 365m), days) - 1m), 2);

        var totalDebt = input.Principal + interest + input.Fees;
        var schedule = BuildSchedule(input, days);
        // Liquidation price at end date: TotalDebt / (CollateralBtc * LiquidationLtv)
        ...
    }
}
```

**Consistency note:** existing `BtcLoanDetails.CalculateAccruedInterest` uses simple daily accrual (`LoanAmount * Apr / 365 * days`). Compound mode should use **daily compounding** (`(1 + Apr/365)^days`) — the standard for crypto lending — and the difference vs. the app's stored-loan math is intentional (simulator explores modes; stored loans keep their existing semantics).

### Pattern 2: Prefill Dropdown via Existing Query (Leverage Simulator clone)

**What:** A `ComboBox` whose first item is "New simulation", followed by BTC-loan assets loaded via `IQueryDispatcher.DispatchAsync(new GetAssetsQuery())`.
**When to use:** Exact pattern proven in `LeverageSimulatorViewModel.LoadPositionsAsync` / `OnSelectedPositionChanged`.
**Trade-offs:** Pulls all assets then filters client-side — acceptable (same as Leverage Simulator; asset counts are small).

**Example:**
```csharp
var assets = await _queryDispatcher.DispatchAsync(new GetAssetsQuery());
var loans = assets.Where(a => a.AssetTypeId == (int)AssetTypes.BtcLoan);
// map into LoanItem { DisplayName, CollateralSats, LoanAmount, Apr, Fees,
//   LoanStartDate, RepaymentDate, LiquidationLtv, CurrencyCode }
```

**Fixed-debt edge case:** loans with `FixedTotalDebt` (HodlHodl-style) have a derived APR — prefill using `BtcLoanDetails.DeriveAprFromFixedDebt(...)` (already in Core) and default to Simple mode.

### Pattern 3: Modal Registration Checklist (all touch points)

Every modal in this app requires the same 6 edits; missing one = runtime failure or dead menu item:

1. `ApplicationModalNames.cs` — add `LoanSimulator = 42` (next free value; 41 is `ReportsCategoryFilterConfig`)
2. `Extensions.cs` — `services.AddTransient<LoanSimulatorViewModel>();`
3. `Extensions.cs` modal factory switch — `ApplicationModalNames.LoanSimulator => new LoanSimulatorView() { DataContext = services.GetRequiredService<LoanSimulatorViewModel>() }`
4. `MainViewModel.cs` — `[RelayCommand] OpenLoanSimulator()` → `_modalLauncher.ShowAsync(ApplicationModalNames.LoanSimulator, Window!)`
5. `MainView.axaml` — `<MenuItem>` under the Tools menu (next to Leverage Simulator, line ~244); optional KeyBinding (F11 is taken by Leverage Simulator — use F12 or none)
6. Localization — `Menu_LoanSimulator` + `LoanSimulator_*` keys in all three `.resx` files **and** `language.Designer.cs`

## Data Flow

### Simulation Flow (no persistence)

```
[User types input] → OnXChanged partial method → Recalculate()
    ↓
Parse texts (culture-tolerant TryParseDecimal — copy from LeverageSimulatorViewModel)
    ↓
BtcLoanSimulationCalculator.Simulate(input)        [Valt.Core, pure]
    ↓
Format results (fiat via CurrencyCode; sats via RatesState BTC price in that currency)
    ↓
HasResults = true → right panel + schedule list update
```

### Prefill Flow

```
[Modal opens] → OnBindParameterAsync()
    ↓
LoadAvailableCurrencies (IConfigurationManager)   ── clone from Leverage Simulator
LoadLoansAsync (IQueryDispatcher → GetAssetsQuery) ── filter AssetTypes.BtcLoan
    ↓
[User selects loan] → OnSelectedLoanChanged → fill text fields → Recalculate()
```

### Sats Conversion

Same formula the Leverage Simulator uses: `sats = fiatValue / btcPriceInCurrency * 100_000_000`, where `btcPriceInCurrency` derives from `RatesState.BitcoinPrice` (USD) × `RatesState.FiatRates[currency]` for non-USD. Null-rate tolerance is required (prices may not be loaded yet) — fall back gracefully and hide sats outputs.

### Schedule Granularity

Daily rows explode for multi-year loans. Emit **monthly anchors + the end date** (or every 30 days), each row = `(Date, AccruedInterest, TotalDebt)`. Cap at ~120 rows; this is display data, not a precision instrument.

## Scaling Considerations

Not applicable in the usual sense (single-user desktop, ephemeral calc). Relevant limits:

| Concern | Approach |
|---------|----------|
| Long loan terms (10y+) | Monthly schedule granularity + row cap; compound via `Math.Pow` in double, cast back to decimal (matches codebase rounding style) |
| Very large sats values | Use `long`/decimal consistently; `BtcValue` conventions already handle this |
| UI recompute churn | Recalculate per keystroke is fine — math is O(schedule rows), no I/O |

## Anti-Patterns

### Anti-Pattern 1: Math in the ViewModel

**What people do:** Implement interest accrual inside `LoanSimulatorViewModel.Recalculate()`.
**Why it's wrong:** Violates the codebase's own rule (Leverage Simulator delegates to `LeveragedPositionDetails`); makes the math untestable without a VM harness and unusable from MCP.
**Do this instead:** Static `BtcLoanSimulationCalculator` in Core; VM only parses/formats.

### Anti-Pattern 2: New CQRS command/query for the simulator

**What people do:** Create `SimulateLoanQuery` in Valt.App "because the App layer is the convention."
**Why it's wrong:** The convention exists to mediate persistence and validation of writes. There is no state here; a query handler would be an empty pass-through. The CLAUDE.md directive ("always use Valt.App for new modules") applies to modules with data, not pure calculators.
**Do this instead:** Reuse `GetAssetsQuery` for prefill; call the Core calculator directly from VM and MCP.

### Anti-Pattern 3: Skipping one localization file or Designer.cs

**What people do:** Add keys only to `language.resx`.
**Why it's wrong:** pt-BR/es builds fall back silently or fail; XAML binds via `{x:Static local:language.X}` which requires the Designer property.
**Do this instead:** All three `.resx` + `language.Designer.cs` in the same change (project rule).

### Anti-Pattern 4: Reusing UI DTOs in the MCP tool

**What people do:** Return `AssetDTO` or VM item types from an MCP tool.
**Why it's wrong:** Project rule: MCP tools define their own DTOs in the tool file.
**Do this instead:** `LoanSimulationResultDto` local to `LoanSimulatorTools.cs`.

## Integration Points

### New vs Modified (explicit)

| Layer | File | New/Modified | Change |
|-------|------|--------------|--------|
| Core | `Modules/Assets/Simulation/*` (4 files) | **New** | Calculator, input/result/schedule records, interest-mode enum |
| App | — | None | Reuse `GetAssetsQuery` + `AssetDTO` as-is |
| UI | `Views/ApplicationModalNames.cs` | Modified | `LoanSimulator = 42` |
| UI | `Extensions.cs` | Modified | Transient registration + factory case |
| UI | `Views/Main/MainViewModel.cs` | Modified | `OpenLoanSimulator` command |
| UI | `Views/Main/MainView.axaml` | Modified | Tools `MenuItem` (+ optional KeyBinding) |
| UI | `Views/Main/Modals/LoanSimulator/*` (3 files) | **New** | View, code-behind, ViewModel |
| UI | `Lang/language.{resx,pt-BR.resx,es.resx,Designer.cs}` | Modified | ~15-20 `LoanSimulator_*` keys + `Menu_LoanSimulator` |
| Infra | `Mcp/Tools/LoanSimulatorTools.cs` | **New** (optional) | `simulate_btc_loan` tool; static calculator → **no `ForwardServicesFromMainApp` change needed** |
| Infra DB | — | None | No migration, no new collections |
| Tests | `Domain/Assets/Simulation/BtcLoanSimulationCalculatorTests.cs` | **New** | NUnit, no DB |
| Tests | `UI/ViewModels/MainViewModelModalCommandTests.cs` | Modified | Launcher assertion for the new command |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| VM ↔ Core calculator | Direct static call | No interface needed; calculator is stateless |
| VM ↔ App (prefill) | `IQueryDispatcher` → `GetAssetsQuery` | Filter `AssetTypes.BtcLoan` in VM, clone Leverage Simulator pattern |
| VM ↔ RatesState | Constructor-injected singleton | For default price + fiat↔sats conversion |
| MCP ↔ Core calculator | Direct static call | No DI forwarding (unlike other tools) — the one place the MCP checklist is trivially satisfied |

## Suggested Build Order (dependency-driven)

1. **Core calculator + enum + records + unit tests** — no dependencies; everything else consumes it. Cover: simple vs compound divergence, zero APR, `end <= start`, fees-only, schedule anchors, liquidation-price-at-end-date, rounding (2dp fiat).
2. **Localization keys** (3 resx + Designer) — must exist before XAML compiles with `{x:Static}` bindings.
3. **Modal plumbing** — enum value, DI transient, factory case. Compiles without the view existing only if factory case added together with the view; do 3+4 in one slice.
4. **View + ViewModel + menu item + MainViewModel command** — one vertical slice (project constraint: VM + XAML together to avoid broken bindings). Clone LeverageSimulatorView layout; add schedule list (ItemsControl/DataGrid) to the right panel.
5. **MainViewModelModalCommandTests addition** — cheap launcher wiring guard.
6. **MCP tool (optional)** — independent; thin wrapper over step 1. Update `.claude/docs/` tool tables if added (v0.6 established docs-drift rules).
7. **E2E verification** — manual: open modal, prefill from a real BTC loan, toggle simple/compound, verify sats values against RatesState price.

## Sources

- Direct codebase inspection: `src/Valt.UI/Views/Main/Modals/LeverageSimulator/` (View + VM), `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs`, `src/Valt.App/Modules/Assets/DTOs/AssetDTO.cs`, `src/Valt.UI/Extensions.cs`, `src/Valt.UI/Views/ApplicationModalNames.cs`, `src/Valt.UI/Views/Main/MainViewModel.cs` / `MainView.axaml`, `src/Valt.Core/Common/FinancialCalculator.cs`, `tests/Valt.Tests/UI/ViewModels/MainViewModelModalCommandTests.cs`
- Project conventions: `AGENTS.md` (CQRS, MCP checklist, localization rule, modal MinWidth/MinHeight rule), `.planning/PROJECT.md` v0.8 requirements

---
*Architecture research for: BTC Loan Simulator (v0.8) in Valt*
*Researched: 2026-08-13*
