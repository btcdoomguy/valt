---
phase: 51-mcp-localization-documentation-verification
plan: 01
subsystem: mcp
tags: [mcp, goals, gsd-mcp-tool, read-only-query]
dependency-graph:
  requires: [phase-49-goal-summary-query, phase-50-goal-summary-ui]
  provides: [GoalTools.GetGoalContributingTransactions, GoalContributingTransactionsMcpResult, GoalContributingTransactionMcpRow]
  affects: [src/Valt.Infra/Mcp/Tools/GoalTools.cs]
tech-stack:
  added: []
  patterns: [mcp-read-only-tool, mcp-owned-dto-in-tool-file, typed-not-supported-result]
key-files:
  created: []
  modified: [src/Valt.Infra/Mcp/Tools/GoalTools.cs]
decisions:
  - "NotSupported maps GoalType from the goal's actual TypeId enum name (equals NetWorthBtc today) instead of a hardcoded string — satisfies the CONTEXT contract and stays correct if future non-transaction goal types appear"
  - "FinalTotal guarded against empty Rows (null instead of rows[^1] throw) — defensive deviation from plan item 5"
metrics:
  duration: 15m
  completed: 2026-10-07
  tasks: 2
  commits: 1
actuals:
  tokens: 1753
  tasks: 2
  commits: 1
status: complete
---

# Phase 51 Plan 01: GetGoalContributingTransactions MCP Tool Summary

Added the `GetGoalContributingTransactions` MCP tool to `GoalTools` — the GOL-08 tracer slice making the goal summary feature AI-accessible end-to-end: MCP surface → App query → Infra calculators → MCP-owned flat DTO, flowing through the already-forwarded `IQueryDispatcher` with zero DI container changes.

## What Was Built

**Task 1 (tracer):** `GetGoalContributingTransactions(IQueryDispatcher dispatcher, string goalId)` on `GoalTools`:
- Not-found goal → `null` (exact `GetGoal` convention; no throw, no error string).
- App query failure (NotFound result) → `null`.
- `NotSupported` (NetWorthBtc) → typed result `{ Supported = false, GoalType = "NetWorthBtc", StrategyUnit = null, FinalTotal = null, Rows = [] }` — never an error string (CONTEXT-locked contract, assumption A1).
- `Supported` → flat MCP DTO with rows carrying Date, Description, Account, Category (null when uncategorized), FiatAmount (magnitude), FiatCurrencyCode, SatsAmount (magnitude), Contribution (derived running-total delta: first row = its RunningTotal, subsequent = delta), RunningTotal; `FinalTotal = rows[^1].RunningTotal`.
- `GoalType` = `(GoalTypeNames)goal.GoalType.TypeId` enum name; `StrategyUnit` via private static TypeId helper replicating `GoalEntryViewModel.SummaryStrategyUnit` (0/4/6→Sats, 2→Count, 8→Percentage, else Fiat) without referencing the UI layer.

**Task 2 (verify-only):** Confirmed both `ICommandDispatcher` (line 255) and `IQueryDispatcher` (line 256) are forwarded in `ForwardServicesFromMainApp()`; `git diff` on `McpServerService.cs` is empty — no modification made, per plan and AGENTS.md MCP checklist item 3.

## Verification

- `dotnet build Valt.sln` → 0 errors (110 pre-existing warnings, out of scope).
- `grep -c "GetGoalContributingTransactions" GoalTools.cs` → 3 (≥2 required).
- `McpDataChangedNotification` count unchanged (11 → 11): read-only tool publishes nothing.
- Both MCP DTOs declared exactly once each in the tool file (AGENTS.md MCP-owned DTO rule honored).
- Tracer feedback gate (auto mode): verify re-run passed end-to-end before expansion/verification tasks.

## Deviations from Plan

### Auto-fixed Issues

None.

### Documented Adjustments (non-behavioral)

1. **FinalTotal empty-rows guard** — Plan item 5 prescribed `FinalTotal = rows[^1].RunningTotal` unconditionally; implemented as `rows.Count > 0 ? rows[^1].RunningTotal : null` to avoid `IndexOutOfRangeException` on a supported goal with zero contributing transactions (e.g., a fresh goal with no period transactions). The happy path is identical to the plan.

## Threat Model Compliance

- T-51-01 (Tampering): goalId flows only into parameterized queries via the App handler; not-found/invalid ids return null. ✓
- T-51-02 (Information Disclosure): accepted — read-only data already exposed via existing tools. ✓
- T-51-03 (DoS): accepted — row set bounded by period transaction count. ✓

## Known Stubs

None.

## Self-Check: PASSED

- `src/Valt.Infra/Mcp/Tools/GoalTools.cs` — FOUND (modified, +135 lines)
- Commit `393a7fe` — FOUND on `vm-next2`
- `src/Valt.Infra/Mcp/Server/McpServerService.cs` — confirmed unchanged (Task 2 acceptance)
