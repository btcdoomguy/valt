---
phase: 38
slug: navigation-new-pages-and-quality-assurance
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-16
---

# Phase 38 — Validation Strategy

> Per-phase validation contract for the navigation, new-page, MCP drift-fix, and QA sweep on the public docs site.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | MkDocs (Material for MkDocs + static-i18n) — docs site build |
| **Config file** | `../valt-docs/mkdocs.yml` |
| **Quick run command** | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` |
| **Full suite command** | Same as quick run (docs-only phase) + scripted nav/mirror audits from RESEARCH.md |
| **Estimated runtime** | ~20 seconds |

> **Environment trap (verified across phases 37–38):** the build only works via the valt-docs `.venv`. pipx `mkdocs` and system `python3 -m mkdocs` both fail.
> **Gate blind spots (verified empirically in research):** `--strict` aborts on broken internal links but does NOT fail on orphan pages or missing `.en.md` mirrors — NAV-01 and QA-01 require the scripted audits from RESEARCH.md in addition to the build.

---

## Sampling Rate

- **After every task commit:** Run `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict`
- **After every plan wave:** Run the strict build + the scripted nav-coverage and mirror-parity audits
- **Before `/gsd-verify-work`:** Full build green + audits green + `38-QA-CHECKLIST.md` committed
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 38-01-01 | 01 | 1 | NAV-03 | — | N/A | docs build + grep checks | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 38-01-02 | 01 | 1 | NAV-01, NAV-02 | — | N/A | nav audit script (RESEARCH.md) | scripted nav-coverage audit | ✅ | ⬜ pending |
| 38-02-01 | 02 | 1 | QA-01 | — | N/A | docs build + drift greps | `grep -c` phantom/real tool names per RESEARCH.md tables | ✅ | ⬜ pending |
| 38-02-02 | 02 | 1 | QA-02 | — | N/A | docs build (strict gate) | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |
| 38-03-01 | 03 | 2 | QA-03 | — | N/A | checklist artifact exists + applied | `test -f .planning/phases/38-navigation-new-pages-and-quality-assurance/38-QA-CHECKLIST.md` | ❌ W0 | ⬜ pending |
| 38-03-02 | 03 | 2 | QA-01 | — | N/A | mirror-parity audit script | scripted `.en.md` parity audit (RESEARCH.md) | ✅ | ⬜ pending |
| 38-03-03 | 03 | 2 | NAV-01, QA-02 | — | N/A | final strict build + REQUIREMENTS/ROADMAP flip | `cd ../valt-docs && . .venv/bin/activate && mkdocs build --strict` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Confirm the valt-docs `.venv` build is green at baseline (re-verified during research).
- [ ] `38-QA-CHECKLIST.md` — created by P03 per RESEARCH.md definition (12-check A/C/T across 18 files).
- [ ] No new test framework needed; existing docs build + research-provided audit scripts cover the phase.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rendered navigation + new Settings page appearance | NAV-01, NAV-02, NAV-03 | Visual nav rendering and i18n title switching cannot be grep-verified | Run `mkdocs serve` in valt-docs, confirm Configurações/Settings appears in nav in both languages and the page renders |
| Tone/readability pass on the QA checklist | QA-03 | Human judgment on tone | Review the committed `38-QA-CHECKLIST.md` results |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
