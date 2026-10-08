---
phase: 42
slug: loans-leverage-reports-ui
# status lifecycle: draft (seeded by plan-phase) → validated (set by validate-phase §6)
# audit-milestone §5.5 distinguishes NOT-VALIDATED (draft) from PARTIAL (validated + nyquist_compliant: false) (#2117)
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-08-11
---

# Phase 42 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit 4 + NSubstitute |
| **Config file** | `tests/Valt.Tests/Valt.Tests.csproj` (no custom `.runsettings`) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~LoanReports"` |
| **Full suite command** | `dotnet test Valt.sln` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~LoanReports"`
- **After every plan wave:** Run `dotnet test Valt.sln`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 42-01-01 | 01 | 1 | LON-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~LoanReportsQueriesTests"` | ❌ W0 | ⬜ pending |
| 42-01-02 | 01 | 1 | LON-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~LoanReportsQueriesTests"` | ❌ W0 | ⬜ pending |
| 42-01-03 | 01 | 1 | LON-02 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~LoanReportsQueriesTests"` | ❌ W0 | ⬜ pending |
| 42-02-01 | 02 | 2 | LON-01/LON-02 | — | N/A | UI/VM | `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` | ❌ new | ⬜ pending |
| 42-02-02 | 02 | 2 | LON-01/LON-02 | — | N/A | UI | manual smoke | — | ⬜ pending |
| 42-03-01 | 03 | 3 | LON-01/LON-02 | — | N/A | integration | `dotnet test Valt.sln` | ✅ existing | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Valt.Tests/Modules/LoanReports/LoanReportsQueriesTests.cs` — stubs/tests for LON-01/LON-02
- [ ] `tests/Valt.Tests/UI/Reports/ReportsViewModelTests.cs` — VM wiring tests
- [ ] Existing NUnit infrastructure covers all phase requirements.

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Loan reports section renders correctly in Reports tab with real data | LON-01/LON-02 | Requires visual confirmation of charts and empty states | Open app, navigate to Reports, ensure "Loans & Leverage" expander shows stacked-bar cost chart and line distance chart when active BTC-backed loans exist; confirm empty-state hint appears when no active loans. |

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
