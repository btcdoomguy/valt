# Phase 51: MCP, Localization, Documentation & Verification - Context

**Gathered:** 2026-10-06
**Status:** Ready for planning

<domain>
## Phase Boundary

The goal summary feature is AI-accessible, fully localized, documented, and verified end-to-end. This phase ships: the MCP tool (GOL-08), pt-BR + es translations of all Phase 50 strings (GOL-09), `.claude/docs/goals.md` update (GOL-10), and end-to-end verification. Feature code shipped in Phases 49 (backend) and 50 (UI), both COMPLETE and verified.

New strings to localize (12 keys in `language.resx`, en-only today): `Goals_ViewSummary`, `GoalSummary_Title`, `GoalSummary_PeriodLabel`, `GoalSummary_TotalLabel`, `GoalSummary_ColumnAccount`, `GoalSummary_ColumnCategory`, `GoalSummary_ColumnFiat`, `GoalSummary_ColumnSats`, `GoalSummary_ColumnRunningTotal`, `GoalSummary_EmptyTitle`, `GoalSummary_EmptyMessage`, `GoalSummary_Close`.

</domain>

<decisions>
## Implementation Decisions

### MCP Tool Contract
- Single tool `GetGoalContributingTransactions(goalId)` on `GoalTools` (pattern: existing `GetGoal`/`GetGoals` tools), goalId parameter described as "ID of the goal (from GetGoals)".
- MCP-owned flat result DTO: `{ Supported, GoalType, StrategyUnit, FinalTotal, Rows[] }` where each row carries date, description, account, category, fiatAmount, fiatCurrencyCode, satsAmount, contribution, runningTotal. `Supported=false` for NetWorthBtc (typed, not an error).
- Services forwarded in `McpServerService.ForwardServicesFromMainApp()` per the AGENTS.md MCP impact checklist (only if new services are needed — the query flows through existing forwarded dispatchers; verify).

### Verification & Docs Depth
- MCP tool test: NUnit integration test via the `IntegrationTest` base dispatching the tool end-to-end (existing GoalTools test precedent), covering Supported rows + NetWorthBtc `Supported=false`.
- Docs: update in-repo `.claude/docs/goals.md` with the View Summary flow, the App-layer query + per-strategy derivation, and the new MCP tool (GOL-10). Public `valt-docs` site update is deferred — note it in the plan's deferred items.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Valt.Infra/Mcp/Tools/GoalTools.cs` — existing tools with `[McpServerToolType]`/`[McpServerTool]` patterns, injecting `IQueryDispatcher`.
- `src/Valt.Infra/Mcp/Server/McpServerService.cs` — `ForwardServicesFromMainApp()`.
- Localization: `src/Valt.UI/Lang/language.resx` (12 new en keys from Phase 50), `language.pt-BR.resx`, `language.es.resx`, `language.Designer.cs`.
- `.claude/docs/goals.md` — module documentation to extend.
- Prior analog phases: 31, 38, 43, 48 used the same MCP + localization + docs + verification shape.

### Established Patterns
- MCP tool DTOs are defined in the tool file (not reusing UI DTOs) per AGENTS.md.
- Localization: Designer.cs static properties per key; translations mirror the en key set exactly.
- Tests: `IntegrationTest` base for MCP tools; `DatabaseTest` for handler-level.

### Integration Points
- `GoalTools.cs` — new tool method.
- `ForwardServicesFromMainApp()` — only if needed.
- `language.pt-BR.resx` / `language.es.resx` / `language.Designer.cs`.
- `.claude/docs/goals.md`.

</code_context>

<specifics>
## Specific Ideas

- Success criterion 4 requires end-to-end verification: grid contents + final running total must match goal progress, and the MCP tool must return identical data — the verifier should compare MCP DTO rows against the App query rows for the same goal.

</specifics>

<deferred>
## Deferred Ideas

- Public `valt-docs` site update for the Goal Summary feature (separate repo/docs effort).

</deferred>
