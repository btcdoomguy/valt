---
phase: 18
slug: manage-asset-builder
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
---

# Phase 18 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit |
| **Config file** | none |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | 01 | 1 | VM-SVC-02 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` — unit tests for create/edit/load paths
- [ ] `src/Valt.UI/Services/IAssetFormBuilder.cs` — interface and records
- [ ] `src/Valt.UI/Services/AssetFormBuilder.cs` — implementation
- [ ] `src/Valt.UI/Services/Exceptions/AssetFormBuildException.cs` — exception type
- [ ] `src/Valt.UI/Extensions.cs` — DI registration

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
