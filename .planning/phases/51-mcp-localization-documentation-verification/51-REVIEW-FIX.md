---
phase: 51-mcp-localization-documentation-verification
fixed_at: 2026-10-07T00:00:00Z
review_path: .planning/phases/51-mcp-localization-documentation-verification/51-REVIEW.md
iteration: 1
findings_in_scope: 3
fixed: 3
skipped: 0
status: all_fixed
---

# Phase 51: Code Review Fix Report

**Fixed at:** 2026-10-07
**Source review:** `.planning/phases/51-mcp-localization-documentation-verification/51-REVIEW.md`
**Iteration:** 1

**Summary:**
- Findings in scope: 3 (WR-01, WR-02, WR-03)
- Fixed: 3
- Skipped: 0

**Verification:** `dotnet build Valt.sln` — 0 errors (106 pre-existing warnings, unchanged).
`dotnet test` — 1822 passed, 0 failed, full suite green. Gates ran in the main checkout
(workflow.use_worktrees not disabled; edits committed directly on `vm-next2`).

## Fixed Issues

### WR-01: App-query failure silently returns null, conflating "not found" with real errors

**Files modified:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs`, `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs`
**Commit:** 5e7719b
**Applied fix:** Followed the review's primary recommendation (typed failure result). When
`GetGoalContributingTransactionsQuery` dispatch fails after the goal was found, the tool now
returns `GoalContributingTransactionsMcpResult { Supported = false, Error = <message> }`
instead of `null`; goal-not-found remains the only `null` path. Added a nullable `Error`
property to the MCP result record with XML docs distinguishing it from typed NotSupported.
Hoisted the `goalType` computation above the failure branch so the error result still
carries the goal type. Extended the tool's XML doc to state that null covers only not-found.
Added `QueryFailure_ReturnsTypedErrorInsteadOfNull` test using a substituted
`IQueryDispatcher` (goal lookup succeeds, contributing-transactions query returns
`Result.Failure`) asserting `Supported == false`, `Error == "query exploded"`, and empty rows.

### WR-02: `GetStrategyUnit` uses magic numbers and creates a second source of truth

**Files modified:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs`
**Commit:** d8db8b9
**Applied fix:** Replaced the integer-literal switch with a switch on named
`GoalTypeNames` enum members exactly as suggested. Also corrected the `_ => "Fiat"`
fallback comment (removed the unreachable NetWorthBtc, noted it takes the NotSupported
path above). Checked for a shared unit-derivation in the App layer first: the App-layer
`GoalTypeOutputDTO` hierarchy does not expose a strategy unit, so the named-enum switch in
the tool file is the correct fix (no UI-layer reference from Infra, no layer inversion).

### WR-03: Parity test recomputes the implementation formula instead of asserting semantics

**Files modified:** `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs`
**Commit:** 4986b6a
**Applied fix:** Added an explicit semantic-assertion block to
`Supported_Parity_With_AppQuery` pinning the seeded scenario's fully-determined values:
row 0 contribution +100,000 / running 100,000; row 1 contribution −25,000 / running
75,000; `FinalTotal == 75_000m` — in addition to the existing MCP↔App parity loop.
Verified the expected values against `StackBitcoinProgressCalculator` accumulation
semantics (BTC income adds, BTC expense subtracts) before pinning them; the full suite
passes with the assertions in place.

## Skipped Issues

None — all findings were fixed.

---

_Fixed: 2026-10-07_
_Fixer: the agent (gsd-code-fixer)_
_Iteration: 1_
