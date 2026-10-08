# Phase 43: MCP, Localization, Documentation & Verification - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-12
**Phase:** 43-MCP, Localization, Documentation & Verification
**Areas discussed:** MCP tool coverage

---

## MCP tool coverage

| Option | Description | Selected |
|--------|-------------|----------|
| One tool per category | Add four coarse tools mapped to the new App modules: GetSpendingAnalytics, GetBtcDenominatedMetrics, GetWealthPerformanceMetrics, GetLoanReports. | ✓ |
| One tool per panel | Add ~10 fine-grained tools mapped to each UI panel. | |
| Hybrid approach | Add both coarse category tools and a few panel-specific tools. | |

**User's choice:** One tool per category.
**Notes:** User preferred a small MCP surface that matches the existing code module boundaries rather than mirroring every UI panel.

---

| Option | Description | Selected |
|--------|-------------|----------|
| Uniform parameters | Every tool takes startDate, endDate, currencyCode, accountIds, categoryIds. | |
| Tailored parameters | Each tool only exposes filters that make sense for that metric. | ✓ |

**User's choice:** Tailored parameters.
**Notes:** Each tool should expose only the inputs meaningful for its metric category (e.g., category filters only for spending/BTC metrics, not for wealth/loans).

---

| Option | Description | Selected |
|--------|-------------|----------|
| Nested combined DTO | Each category tool returns one result object with nested sections for each panel. | ✓ |
| Flat key-value DTO | Return a flat list keyed by metric name with simple values. | |
| Dedicated MCP DTOs | Follow existing ReportTools pattern of dedicated DTOs in ReportTools.cs. | |

**User's choice:** Nested combined DTO.
**Notes:** DTOs should be nested by panel/metric within the category result, not flat or one-DTO-per-tool.

---

| Option | Description | Selected |
|--------|-------------|----------|
| Extend ReportTools.cs | Keep all report tools in one place. | ✓ |
| New V07ReportTools.cs | Create a new partial class for the new v0.7 tools. | |
| One file per tool | Create one file per tool containing its tool and DTOs. | |

**User's choice:** Extend ReportTools.cs.
**Notes:** User explicitly chose to extend the existing ReportTools.cs file rather than split tools into new files.

---

## the agent's Discretion

The following areas were presented but not selected for discussion; the planner/executor may decide within the phase boundary:
- Localization handoff (how pt-BR/es translations are produced and quality-checked).
- Documentation depth (how detailed the .claude/docs/reports.md update should be).
- End-to-end verification strategy (exact mix of automated and manual verification).

## Deferred Ideas

None — discussion stayed within the Phase 43 boundary.
