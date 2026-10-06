---
phase: 45
slug: simulator-modal-ui-inputs-results-panel
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-08-14
---

# Phase 45 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit 4.4.0 + NSubstitute 5.3.0 |
| **Config file** | none — configured via project references |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 45-01-01 | 01 | 1 | SIM-01 | T-45-01 / — | VM accepts and parses all loan input fields | unit | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"` | ❌ W0 | ⬜ pending |
| 45-01-02 | 01 | 1 | SIM-02 | — | Recalculate() runs on every observable input change | unit | same filter | ❌ W0 | ⬜ pending |
| 45-01-03 | 01 | 1 | SIM-05 | — | Total to repay + principal/interest/fees breakdown rendered | unit | same filter | ❌ W0 | ⬜ pending |
| 45-01-04 | 01 | 1 | SIM-06 | — | Fiat + sats values computed with current BTC price; fallback when price unavailable | unit | same filter | ❌ W0 | ⬜ pending |
| 45-01-05 | 01 | 1 | SIM-07 | — | Liquidation BTC price derived from total debt / (collateral × LTV) | unit | same filter | ❌ W0 | ⬜ pending |
| 45-01-06 | 01 | 1 | SIM-08 | — | Effective fee-inclusive APR displayed | unit | same filter | ❌ W0 | ⬜ pending |
| 45-01-07 | 01 | 1 | SIM-09 | — | Distance to liquidation vs live BTC price displayed | unit | same filter | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` — stubs for SIM-01/02/05/06/07/08/09
- [ ] `BtcLoanSimulatorViewModel` design-time constructor and live recalc behavior
- [ ] Build verification that new modal registers correctly in `ApplicationModalNames` + `Extensions.cs`

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
