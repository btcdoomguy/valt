---
phase: 51-mcp-localization-documentation-verification
plan: 03
subsystem: mcp
tags: [tests, integration, mcp, goals]
requires:
  - 51-01 (GetGoalContributingTransactions MCP tool in GoalTools)
  - 49-03 (GetGoalContributingTransactionsQuery App query + handler)
provides:
  - End-to-end proof that the MCP tool's DTO is byte-identical to the App query result through real DI
  - Typed Supported=false proof for NetWorthBtc and null not-found convention proof
affects:
  - 51-04/51-05 (docs/verification plans can cite this parity proof for GOL-08)
tech-stack:
  added: []
  patterns:
    - IntegrationTest base + AddValtApp + direct static MCP tool invocation (AssetToolsSoldStateTests precedent)
    - Data-parity assertion against a second dispatch of the same App query, never a hand-built row list
key-files:
  created:
    - tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs
  modified: []
decisions:
  - Skipped RED/GREEN ceremony — production surface (MCP tool) pre-exists from 51-01; committed green as test(51-03), following the 50-03 precedent
metrics:
  duration: 10 min
  completed: 2026-10-07
  tasks: 2
  commits: 1
status: complete
actuals:
  tokens: 9000
  tasks: 2
  commits: 1
---

# Phase 51 Plan 03: MCP Tool Integration Verification (GoalToolsTests) Summary

End-to-end integration proof that `GoalTools.GetGoalContributingTransactions` returns DTO data byte-identical to the App query it wraps, dispatched through the real DI container; NetWorthBtc yields a typed `Supported=false`; unknown goal IDs return null.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | GoalToolsTests fixture — supported-goal data-parity test | 5b478c9 | tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs |
| 2 | Full-suite gate | — (verification only, no file changes) | — |

## What Was Built

`tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs` — three-test fixture on the `IntegrationTest` base:

1. **Supported_Parity_With_AppQuery** — seeds a StackBitcoin monthly goal (refDate 2024-06-15), a fiat account, a BTC account, one Bitcoin income (100k sats, Jun 10) and one Bitcoin expense (25k sats, Jun 15). Invokes the tool statically with the container-resolved `IQueryDispatcher`, asserts `Supported=true`, `GoalType="StackBitcoin"`, `StrategyUnit="Sats"`, 2 rows, `FinalTotal == last row RunningTotal`. Then dispatches `GetGoalContributingTransactionsQuery` directly through the same dispatcher and asserts field-by-field parity per row (Date/Description/Account/Category/FiatAmount/FiatCurrencyCode/SatsAmount/RunningTotal) plus `Contribution == running-total delta`. Parity is against a second dispatch of the App query — never a hand-built expectation list (per plan prohibition).
2. **NetWorthBtc_ReturnsSupportedFalse** — seeds `GoalBuilder.ANetWorthBtcGoal`; asserts `Supported=false`, `GoalType="NetWorthBtc"`, null StrategyUnit/FinalTotal, empty Rows (typed, not an error).
3. **UnknownGoalId_ReturnsNull** — id `000000000000000000000099` returns null exactly.

Cleanup follows the handler-test pattern: delete all goals via container-resolved `IGoalRepository`, wipe transactions/accounts/categories collections via `_localDatabase`.

## Verification Results

- Filtered: `Passed! - Failed: 0, Passed: 3, Total: 3` (GoalToolsTests)
- Full suite: `Passed! - Failed: 0, Passed: 1821, Skipped: 0, Total: 1821`
- Plan's expected total was 1806 (1803 + 3); the pre-existing baseline had grown to 1818 since the 49-03 gate count, so the realized total is 1821 — 0 failures is the binding criterion and it holds.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Missing `using Valt.App;` caused CS1061 on first compile**
- **Found during:** Task 1
- **Issue:** `AddValtApp()` extension not resolved — the using list omitted `Valt.App`
- **Fix:** Added `using Valt.App;` (mirrors AssetToolsSoldStateTests usings)
- **Files modified:** tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs
- **Commit:** 5b478c9

### Process Deviations

**1. TDD RED/GREEN ceremony skipped — production surface pre-exists**
- **Found during:** Task 1 (tdd="true")
- **Issue:** The plan marks Task 1 `tdd="true"`, but the behavior under test (the MCP tool) shipped in 51-01; a RED phase is impossible for behavior reasons — the tests pass green on first compile.
- **Fix:** Followed the documented Phase 50-03 precedent (logged in STATE.md): test-only plan against pre-existing production surface commits green as a single `test(...)` commit.
- **Commit:** 5b478c9

**2. Full-suite total (1821) exceeds plan's expected 1806**
- **Found during:** Task 2
- **Issue:** The plan computed 1803 (49-03 baseline) + 3; 18 additional tests landed via 51-01/51-02 and other commits since that count.
- **Fix:** None needed — the acceptance criterion's binding clause ("0 failed tests") holds. Count discrepancy documented here.

## Self-Check: PASSED

- FOUND: tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs
- FOUND: commit 5b478c9 (test(51-03): add GoalToolsTests MCP integration fixture)

## Known Stubs

None.
