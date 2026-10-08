---
phase: 51-mcp-localization-documentation-verification
reviewed: 2026-10-07T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - src/Valt.Infra/Mcp/Tools/GoalTools.cs
  - src/Valt.UI/Lang/language.pt-BR.resx
  - src/Valt.UI/Lang/language.es.resx
  - tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs
findings:
  critical: 0
  warning: 3
  info: 0
  total: 3
status: fixed
fixed_at: 2026-10-07T00:00:00Z
fix_summary: |
  WR-01 (commit 5e7719b): GetGoalContributingTransactions now returns a typed
  result with Supported=false and a new nullable Error field when the App query
  fails, instead of null; failure-path test added (substituted IQueryDispatcher).
  WR-02 (commit d8db8b9): GetStrategyUnit switched from magic type ids to named
  GoalTypeNames enum cases; fallback comment corrected.
  WR-03 (commit 4986b6a): parity test now pins semantic expected values
  (contributions +100,000/−25,000 sats, running totals 100,000/75,000,
  FinalTotal 75,000) in addition to MCP↔App parity.
  Verification: dotnet build Valt.sln — 0 errors; dotnet test — 1822 passed,
  0 failed (main checkout).
---

# Phase 51: Code Review Report

**Reviewed:** 2026-10-07
**Depth:** standard
**Files Reviewed:** 4 (+ `.claude/docs/goals.md` read for cross-checking claims)
**Status:** issues_found

## Summary

Reviewed the `GetGoalContributingTransactions` MCP tool + MCP DTOs, the 12 new GoalSummary
localization keys in pt-BR and es, the goals.md documentation update, and the new
`GoalToolsTests` fixture. Build succeeds with 0 warnings/errors; all 3 new tests pass
(`dotnet test --filter FullyQualifiedName~GoalToolsTests`: 3/3 passed).

Cross-module facts were verified against the codebase: `GoalTypeNames` enum values match the
`GetStrategyUnit` mapping (0=StackBitcoin, 2=Dca, 4=IncomeBtc, 6=BitcoinHodl, 8=SavingsRate);
the App-layer `GoalContributingTransactionsResult` discriminated union and
`ContributingTransactionRow` DTO fields match the MCP mapping; en resx keys and
`language.Designer.cs` properties predate this phase (added in commit 661ff6d, phase 50) so the
"update ALL THREE language files + Designer" convention is satisfied in aggregate; the tool is
read-only and publishes no `McpDataChangedNotification`, per convention; MCP DTOs are defined in
the tool file, per convention.

Three warnings found: one error-handling gap in the tool, one fragile magic-number mapping, and
one test-reliability gap. No security issues (no injection surface — goalId flows through the
CQRS dispatcher to LiteDB id lookups; no secrets, no eval, no deserialization of untrusted data).

## Warnings

### WR-01: App-query failure silently returns null, conflating "not found" with real errors

**File:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs:60-66`
**Issue:** When the goal exists (the preceding `GetGoalQuery` succeeded) but the
`GetGoalContributingTransactionsQuery` dispatch fails (`result.IsFailure`), the tool returns
`null` — the same signal used for "goal not found". An MCP client (an AI assistant) cannot
distinguish "this goal id doesn't exist" from "the query failed internally" (e.g., malformed
stored data, transient repository error). This also undercuts the phase's own documented design
philosophy: `NotSupported` was made a *typed* fallback specifically so it wouldn't be mistaken
for an error, yet genuine errors are flattened into an untyped null. The doc comment on the tool
("Returns null when the goal is not found") does not document the second, error-path null.

**Fix:** Return a typed failure indication instead of null for the error path, e.g.:

```csharp
var result = await dispatcher.DispatchAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });
if (result.IsFailure)
    return new GoalContributingTransactionsMcpResult
    {
        Supported = false,
        GoalType = goalType,
        StrategyUnit = null,
        FinalTotal = null,
        Rows = [],
        Error = result.Error?.Message   // add nullable Error field to distinguish from NotSupported
    };
```

Or at minimum, extend the `[Description]`/XML doc to state that null covers both not-found and
internal failure.

### WR-02: `GetStrategyUnit` uses magic numbers and creates a second source of truth for the unit mapping

**File:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs:119-127`
**Issue:** The switch hardcodes integer type ids (`0 or 4 or 6 => "Sats"`, `2 => "Count"`,
`8 => "Percentage"`) even though `Valt.Core.Modules.Goals` is already imported and the
`GoalTypeNames` enum is used one line earlier (`((GoalTypeNames)goal.GoalType.TypeId).ToString()`).
If the enum is ever reordered or a value is inserted, this mapping silently breaks (it compiles
cleanly and returns the wrong unit). This is also a *second* copy of the same mapping that
already exists in the UI layer (`GoalEntryViewModel.SummaryStrategyUnit`, a DTO-type-based
switch, `GoalEntryViewModel.cs:204-211`) — two hand-maintained copies of one semantic, which
the code comment itself acknowledges ("mirrors the SummaryStrategyUnit semantics"). The `_`
fallback comment also misleadingly lists NetWorthBtc as reaching the Fiat branch, which is
unreachable (NetWorthBtc always takes the `NotSupported` path above).

**Fix:** Use enum members instead of literals:

```csharp
private static string GetStrategyUnit(int typeId) => (GoalTypeNames)typeId switch
{
    GoalTypeNames.StackBitcoin or GoalTypeNames.IncomeBtc or GoalTypeNames.BitcoinHodl => "Sats",
    GoalTypeNames.Dca => "Count",
    GoalTypeNames.SavingsRate => "Percentage",
    _ => "Fiat"
};
```

(Longer-term, consider moving the unit onto the goal-type DTO or the App query result so the
UI and MCP read it from one source.)

### WR-03: Parity test recomputes the implementation formula instead of asserting semantics — can pass with a systematically wrong result

**File:** `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs:107-135`
**Issue:** The test's parity loop recomputes the contribution formula in the test body
(`expectedContribution = i == 0 ? app.RunningTotal : app.RunningTotal - previousRunningTotal`)
— byte-for-byte the same formula as the tool under test (`GoalTools.cs:88-91`). This verifies
the tool copies the App query's rows, but nothing verifies the *values* are right: if the App
query's `RunningTotal` accumulation had a sign or ordering bug, both sides would agree and the
test would pass. The seeded scenario is semantically rich — a 100,000-sat BTC income on 06-10
and a 25,000-sat BTC spend on 06-15 under a StackBitcoin goal — so the expected values are
fully determined (row 1 contribution +100,000 / running 100,000; row 2 contribution −25,000 /
running 75,000; FinalTotal = 75,000 sats), yet no assertion checks any of them, and
`FinalTotal` is only asserted as equal to the last row's own `RunningTotal` (a tautology given
`FinalTotal = rows[^1].RunningTotal` in the tool). Per project test guidelines ("evaluate to
create new tests if needed"), the fixture should pin at least one semantic expectation.

**Fix:** Add explicit value assertions to `Supported_Parity_With_AppQuery`:

```csharp
Assert.That(mcpResult.Rows[0].Contribution, Is.EqualTo(100_000m));
Assert.That(mcpResult.Rows[1].Contribution, Is.EqualTo(-25_000m));
Assert.That(mcpResult.FinalTotal, Is.EqualTo(75_000m));
Assert.That(mcpResult.Rows[1].RunningTotal, Is.EqualTo(75_000m));
```

(Adjust expected values to the strategy's actual accumulation semantics if the BTC-expense
handling differs — that determination is precisely the missing test.)

## Notes (no action required)

- **Localization parity is verified, but only manually.** Commit 788d29f message claims a
  "tri-locale parity gate", and the 12 keys (`Goals_ViewSummary` + 11 `GoalSummary_*`) are
  present and identical in name across `language.resx`, `language.pt-BR.resx`, and
  `language.es.resx`, with matching `language.Designer.cs` properties. However, no automated
  test enforces key parity between resx files, so the next key addition can silently drift
  locales. Optional: a small test asserting identical key sets across the three resx files.
- **Test namespace/folder mismatch:** `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs`
  declares namespace `Valt.Tests.Infrastructure.Mcp.Tools` (`Infra` vs `Infrastructure`).
  Cosmetic; pre-existing convention varies in the repo.
- **Using-directive ordering:** `using Valt.Core.Modules.Goals;` is spliced between `Valt.App`
  usings (`GoalTools.cs:11` region) rather than sorted. Cosmetic.
- **`new async Task SetUp()` hides the base `[SetUp]`** (`IntegrationTest.SetUp` is non-virtual
  and a no-op), so NUnit may run both — harmless today, but brittle if the base method gains a
  body. Prefer making the base method `virtual` and using `override`.
- **docs accuracy:** the goals.md Goal Summary section was spot-checked against the code
  (query contract, NotSupported semantics, unit-per-type table, file layout) — all claims
  verified accurate.

---

_Reviewed: 2026-10-07_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
