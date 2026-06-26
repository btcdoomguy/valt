---
phase: 12
slug: remove-blocking-calls
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-19
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit 4.x |
| **Config file** | `tests/Valt.Tests/Valt.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~BlockingCallTests|ReportsViewModelTests|FireAndForgetTaskRunnerTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick command
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 12-01-01 | 01 | 1 | ASYNC-02 | — | N/A | unit + regex | `dotnet test` + grep | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` — stubs for ASYNC-02
- [x] `tests/Valt.Tests/Architecture/BlockingCallTests.cs` — architecture regex test for `.GetAwaiter().GetResult()` in ViewModels

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Reports tab initializes and refreshes correctly | ASYNC-02 | UI interaction requires Avalonia runtime | Launch app, open Reports tab, verify indicators load without deadlock |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved

---

## Final Verification Results

- `dotnet build Valt.sln`: ✅ succeeded (0 errors)
- `dotnet test`: ✅ passed — 1509 tests, 0 failed, 0 skipped
- Blocking-pattern grep over `src/Valt.UI/**/*.cs`: ✅ 0 matches
- Targeted filter (`BlockingCallTests|ReportsViewModelTests|FireAndForgetTaskRunnerTests`): ✅ 4/4 passed
