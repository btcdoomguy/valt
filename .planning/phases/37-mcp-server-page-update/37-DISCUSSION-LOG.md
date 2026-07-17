# Phase 37: MCP Server Page Update - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-16
**Phase:** 37-mcp-server-page-update
**Areas discussed:** AssetTools section structure, Parameter documentation depth, IndicatorTools coverage
**Mode:** --auto (all gray areas auto-selected; recommended options chosen without prompting)

---

## AssetTools Section Structure

| Option | Description | Selected |
|--------|-------------|----------|
| Grouped subsections | Split 26 tools into logical groups (core assets, loans/lending, asset groups, loan-state timeline, sold assets) keeping the page's 2-column tables | ✓ |
| One flat table | Single 26-row 2-column table for all AssetTools | |

**User's choice:** Grouped subsections (auto-selected, recommended default)
**Notes:** A 26-row flat table is hard to scan; grouping mirrors the success criteria's own grouping (loan-state tools, sold-asset tools) and preserves the page's established 2-column style.

---

## Parameter Documentation Depth

| Option | Description | Selected |
|--------|-------------|----------|
| Parameter tables for the 7 required tools only | Small `\| Parâmetro \| Descrição \|` tables under the 4 loan-state + 3 sold-asset tools; everything else stays 2-column | ✓ |
| Inline parameters in description column | Append parameter names inside the existing description cells | |
| Parameter tables for all tools on the page | Retrofit all 8 pre-existing categories plus new ones | |

**User's choice:** Parameter tables for the 7 required tools only (auto-selected, recommended default)
**Notes:** Success criteria 2 and 3 explicitly require parameters for the loan-state and sold-asset tools. Retrofitting all categories is scope creep; inline lists would hurt readability.

---

## IndicatorTools Coverage

| Option | Description | Selected |
|--------|-------------|----------|
| Add brief IndicatorTools section | Document the Bitcoin macro-indicators tool; traceability stays on MCP-01/02/03 | ✓ |
| Exclude IndicatorTools | Strictly scope to MCP-01/02/03; leave IndicatorTools undocumented | |

**User's choice:** Add brief IndicatorTools section (auto-selected, recommended default)
**Notes:** The phase goal states "lists the complete current toolset" — IndicatorTools is part of the current toolset. Small cost, goal-aligned.

---

## the agent's Discretion

- Portuguese wording of tool descriptions (consistent with existing page tone and app terminology).
- Exact subsection titles and grouping boundaries inside AssetTools.
- Placement of new sections relative to existing categories.

## Deferred Ideas

- Retrofitting parameter tables onto the 8 pre-existing tool categories — future phase.
- Screenshots/diagrams; translations beyond pt-BR/en-US — per REQUIREMENTS.md v2 items.
