---
phase: 49
slug: goal-contributing-transactions-query-backend
# status lifecycle: draft (seeded by plan-phase) → validated (set by validate-phase §6)
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-10-06
---

# Phase 49 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit 4.x + NSubstitute 5.x (existing) |
| **Config file** | none — test project conventions (`tests/Valt.Tests`) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~120 seconds (full suite) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 49-XX   | TBD  | TBD  | GOL-05      | —          | N/A             | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests.RowShape"` | ❌ W0 | ⬜ pending |
| 49-XX   | TBD  | TBD  | GOL-06      | —          | N/A             | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~Reconciles"` | ❌ W0 | ⬜ pending |
| 49-XX   | TBD  | TBD  | GOL-07      | —          | N/A             | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~AddRemove"` | ❌ W0 | ⬜ pending |
| 49-XX   | TBD  | TBD  | GOL-07      | —          | N/A             | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~NotSupported"` | ❌ W0 | ⬜ pending |

*Task IDs to be finalized by the planner. Source: 49-RESEARCH.md §Validation Architecture (test matrix per goal type: SpendingLimit, ReduceExpenseCategory, IncomeFiat, SaveFiat, SavingsRate, StackBitcoin, IncomeBtc, Dca, BitcoinHodl, NetWorthBtc).*

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Valt.Tests/.../GetGoalContributingTransactionsHandlerTests.cs` — test class stubs for GOL-05/06/07 (per-strategy reconciliation, add/remove, NotSupported, GOAL_NOT_FOUND, empty-set)
- [ ] Test builders already exist (`TransactionBuilder`, `FiatAccountBuilder`, `BtcAccountBuilder`, `GoalBuilder`, `PriceDataBuilder.SeedRange`) — no new framework installs

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|----------------|
| — | — | — | All phase behaviors have automated verification |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
