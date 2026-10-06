---
status: issues
files_reviewed: 6
critical: 0
warning: 2
info: 1
total: 3
---

# Code Review: Phase 44

## Summary

The new BTC loan simulation calculator (`BtcLoanSimulationCalculator`) and its supporting records/enum are a focused, self-contained domain addition. The core simple/compound interest math, schedule generation, and APR/leverage outputs appear sound for the happy path, and the test suite covers the main scenarios. However, the public API accepts inputs that produce nonsensical financial outputs and does not fully validate the simulation boundary. These are correctness/robustness issues that should be tightened before the code is consumed by the App/Infra/UI layers.

## Findings

### WR-01: Missing validation for `LiquidationLtv`

**File:** `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs:60-73`
**Classification:** WARNING

`Validate(...)` checks `Apr`, `Fees`, `CollateralSats`, and `PrincipalAmount`, but never validates `LiquidationLtv`. A caller can pass a negative LTV, causing `CalculateLiquidationPrice` to produce a negative price (line 175). A zero LTV is also silently swallowed and returns 0, which masks the fact that the input is invalid.

**Fix:**

```csharp
if (input.LiquidationLtv <= 0)
    throw new ArgumentException("Liquidation LTV must be greater than zero", nameof(input.LiquidationLtv));
```

Add corresponding tests for negative and zero `LiquidationLtv`.

---

### WR-02: Inverted date ranges produce a "valid" zero-day result

**File:** `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs:16-29`
**Classification:** WARNING

When `EndDate` is before `StartDate`, the method returns a happy-path result with zero interest and a single schedule row at `StartDate`. This is the same result as a same-day loan, which makes it easy for a caller to accidentally simulate an inverted range and not notice. The existing test explicitly locks in this behavior, but the public API would be safer by rejecting an impossible duration.

**Fix:** Either treat `EndDate < StartDate` as an error:

```csharp
if (input.EndDate < input.StartDate)
    throw new ArgumentException("End date must be on or after the start date", nameof(input.EndDate));
```

or, if a zero-day result is intentional for `EndDate == StartDate`, keep the `days == 0` branch but reject the negative-duration case. Update the test that asserts the current behavior accordingly.

---

### IN-01: Magic number for sats-per-BTC

**File:** `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs:171`
**Classification:** INFO

The literal `100_000_000m` is used to convert satoshis to BTC. The codebase already has `BtcValue` representing the satoshi/BTC relationship, and the domain would be clearer and less error-prone if this constant were named.

**Fix:** Introduce a named constant or reuse the existing conversion mechanism:

```csharp
private const decimal SatsPerBtc = 100_000_000m;

var collateralBtc = input.CollateralSats / SatsPerBtc;
```

---

_Reviewed: 2026-08-14T00:00:00Z_
_Reviewer: gsd-code-reviewer_
_Depth: standard_
