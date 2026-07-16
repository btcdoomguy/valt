---
phase: 37
slug: mcp-server-page-update
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-16
---

# Phase 37 — Validation Strategy

> Per-phase validation contract for the documentation-only update to the MCP Server public docs page.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | MkDocs (Material for MkDocs) — docs site build |
| **Config file** | `../valt-docs/mkdocs.yml` |
| **Quick run command** | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` |
| **Full suite command** | Same as quick run (docs-only phase) |
| **Estimated runtime** | ~15 seconds |

> **Environment trap (verified in research):** the build only works via the valt-docs `.venv` (mkdocs 1.6.1 + material 9.7.1 + static-i18n 1.3.0). pipx `mkdocs` and system `python3 -m mkdocs` both fail.

---

## Sampling Rate

- **After every task commit:** Run `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict`
- **After every plan wave:** Run `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict`
- **Before `/gsd-verify-work`:** Full build must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 37-01-01 | 01 | 1 | MCP-01 | — | N/A | docs build + grep checks | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 37-01-02 | 01 | 1 | MCP-02 | — | N/A | docs build + grep checks | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 37-01-03 | 01 | 1 | MCP-03 | — | N/A | docs build + grep checks | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 37-02-01 | 02 | 2 | MCP-01 | — | N/A | docs build + parity diff | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 37-02-02 | 02 | 2 | MCP-02 | — | N/A | docs build (strict gate) | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 37-02-03 | 02 | 2 | MCP-03 | — | N/A | REQUIREMENTS.md status check | `grep -c '\- \[x\] \*\*MCP-0' .planning/REQUIREMENTS.md` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Confirm the valt-docs `.venv` exists and `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` is green at baseline (verified during research).
- [ ] Confirm both `../valt-docs/docs/funcionalidades/mcp-server.md` and `../valt-docs/docs/funcionalidades/mcp-server.en.md` exist.
- [ ] No new test framework needed; existing docs build covers the phase.

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rendered page readability of the new AssetTools parameter tables | MCP-02, MCP-03 | Visual rendering of nested tables/admonitions cannot be verified by grep | Run `mkdocs serve` in valt-docs, open the MCP Server page (pt + en), confirm the 7 parameter tables render correctly |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
