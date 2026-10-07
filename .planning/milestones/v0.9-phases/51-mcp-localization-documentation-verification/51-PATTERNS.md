# Phase 51: MCP, Localization, Documentation & Verification - Pattern Map

**Mapped:** 2026-10-07
**Files analyzed:** 6
**Analogs found:** 6 / 6

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.Infra/Mcp/Tools/GoalTools.cs` (modify: add `GetGoalContributingTransactions`) | service (MCP tool) | request-response (query) | Existing `GetGoal`/`GetGoals` tools in same file (`GoalTools.cs` lines 22-46) | exact |
| `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs` (new) | test | request-response (end-to-end DI) | `tests/Valt.Tests/Infra/Mcp/Tools/AssetToolsSoldStateTests.cs` | exact |
| `src/Valt.UI/Lang/language.pt-BR.resx` (modify: add 12 keys) | config (localization) | transform | Existing sibling keys in same file: `Goals_Title` (line 2003), `LoanStateHistory_Close` (line 2920), `Transactions.Columns.Category` (line 225) | exact |
| `src/Valt.UI/Lang/language.es.resx` (modify: add 12 keys) | config (localization) | transform | Same sibling keys: `Goals_Title` (line 2018), `LoanStateHistory_Close` (line 2920), `Transactions.Columns.Category` (line 312) | exact |
| `src/Valt.Infra/Mcp/Server/McpServerService.cs` (verify-only: `ForwardServicesFromMainApp`) | config (DI) | N/A | Same method, lines 249-281 | exact |
| `.claude/docs/goals.md` (modify: append Goal Summary section) | documentation | N/A | Same file's existing section structure (433 lines, headings listed below) | exact |

**No change needed:** `language.Designer.cs` — the 12 static properties already exist (verified lines 4056-4082+). `language.resx` — English source already present (lines 2031-2066).

## Pattern Assignments

### `src/Valt.Infra/Mcp/Tools/GoalTools.cs` (service, request-response)

**Analog:** Same file, `GetGoals`/`GetGoal` query tools (lines 22-46). Read-only tools dispatch a query and return the result directly; no `McpDataChangedNotification` publish (only mutating tools publish).

**Imports pattern** (lines 1-12) — add one using line:
```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
// ... existing ...
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions; // NEW
using Valt.App.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;
```

**Read-only query tool pattern** (lines 40-46) — copy exactly, adapt to the summary query:
```csharp
[McpServerTool, Description("Get a single financial goal by its ID")]
public static async Task<GoalDTO?> GetGoal(
    IQueryDispatcher dispatcher,
    [Description("The goal ID")] string goalId)
{
    return await dispatcher.DispatchAsync(new GetGoalQuery { GoalId = goalId });
}
```

**New tool signature** (pattern-consistent shape):
```csharp
[McpServerTool, Description("Get the transactions contributing to a goal's progress, with running total")]
public static async Task<GoalContributingTransactionsMcpResult?> GetGoalContributingTransactions(
    IQueryDispatcher dispatcher,
    [Description("ID of the goal (from GetGoals)")] string goalId)
```

**Core pattern to implement** — the App query returns `Result<GoalContributingTransactionsResult>` where the result is a discriminated union (`Supported(Rows)` / `NotSupported(GoalTypeNames)`). Handler at `src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsHandler.cs`:
```csharp
public async Task<Result<GoalContributingTransactionsResult>> HandleAsync(...)
{
    var result = await _goalQueries.GetContributingTransactionsAsync(query.GoalId);
    return result is null
        ? Result<GoalContributingTransactionsResult>.NotFound("Goal", query.GoalId)
        : Result<GoalContributingTransactionsResult>.Success(result);
}
```

**Error handling pattern** — query tools return `null` on not-found (see `GetGoal`); for the new tool, map: not-found → `null`; `Supported` → flat MCP DTO `{ Supported: true, GoalType, StrategyUnit, FinalTotal, Rows[] }`; `NotSupported` → `{ Supported: false, GoalType = NetWorthBtc, ... }` (typed, not an error, per CONTEXT.md). MCP-owned DTO must be declared in the tool file (per AGENTS.md — not reusing UI/App DTOs).

**Row DTO fields to carry** (from `src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs`): `Date`, `Description`, `AccountName`, `CategoryName` (nullable), `FiatAmount` (magnitude, non-negative, `FiatValue.Value`), `FiatCurrencyCode`, `SatsAmount` (`BtcValue.Sats`), `RunningTotal` (decimal; final row equals goal's calculated field).

**DI verification:** `IQueryDispatcher` is already forwarded in `ForwardServicesFromMainApp()` (McpServerService.cs line 256) — **no change expected** to `McpServerService.cs`.

---

### `tests/Valt.Tests/Infra/Mcp/Tools/GoalToolsTests.cs` (test, request-response)

**Analog:** `tests/Valt.Tests/Infra/Mcp/Tools/AssetToolsSoldStateTests.cs` (106 lines, full file read).

**Class + set-up pattern** (lines 1-46):
```csharp
namespace Valt.Tests.Infrastructure.Mcp.Tools;

[TestFixture]
public class AssetToolsSoldStateTests : IntegrationTest
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;

    [OneTimeSetUp]
    public void AddApplicationLayer()
    {
        _serviceCollection.AddValtApp();
        RebuildServiceProvider();
    }

    [SetUp]
    public new async Task SetUp()
    {
        _commandDispatcher = _serviceProvider.GetRequiredService<ICommandDispatcher>();
        _queryDispatcher = _serviceProvider.GetRequiredService<IQueryDispatcher>();
        // ... cleanup of seeded entities via repository ...
    }
```

**Direct tool invocation pattern** (lines 55-61) — tools are static; dispatchers resolved from the DI container are passed in:
```csharp
var result = await AssetTools.MarkAssetAsSold(
    _commandDispatcher, _notificationPublisher, asset.Id.Value, today);
Assert.That(result, Does.Not.StartWith("Error:"));
var soldAssets = await AssetTools.ListSoldAssets(_queryDispatcher);
```

**Coverage required by CONTEXT.md success criterion 4:**
1. Supported goal (e.g. StackBitcoin or SpendingLimit) → seed goal + contributing transactions via `GoalBuilder` and `TransactionBuilder`, call `GoalTools.GetGoalContributingTransactions(_queryDispatcher, goalId)`, assert rows match the App query result dispatched directly via `_queryDispatcher.DispatchAsync(new GetGoalContributingTransactionsQuery { GoalId = ... })` (data-parity check).
2. NetWorthBtc goal (`GoalBuilder.ANetWorthBtcGoal()`) → assert `Supported == false`, `GoalType == NetWorthBtc`. Precedent assertion shape from `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` lines 391-407:
```csharp
var notSupported = (GoalContributingTransactionsResult.NotSupported)result.Value!;
Assert.That(notSupported.Type, Is.EqualTo(GoalTypeNames.NetWorthBtc));
```

**Test data builders** (per AGENTS.md): `GoalBuilder.AStackBitcoinGoal(...)`, `GoalBuilder.ANetWorthBtcGoal()`, `TransactionBuilder.ATransaction()`, `FiatAccountBuilder.AnAccount()`. `IdGenerator.Configure(new LiteDbIdProvider())` happens in `IntegrationTest` ctor — no extra `OneTimeSetUp` needed.

---

### `src/Valt.UI/Lang/language.pt-BR.resx` (config, transform)

**Analog:** Existing sibling keys in the same file. Exact XML shape (4-line blocks, `xml:space="preserve"`):
```xml
    <data name="Goals_Title" xml:space="preserve">
        <value>Metas</value>
    </data>
```

**Insertion point:** append after the `Goals_YearlyIndicator` block (line 2033), mirroring the English file's `<!-- Goal Summary Modal -->` comment position (en lines 2030-2066).

**Established terminology to reuse** (verbatim, per UI-SPEC translation rules 1 & 4):
| English key | pt-BR established translation | Source key in file |
|---|---|---|
| Close | `Fechar` | `LoanStateHistory_Close` (line 2920) |
| Category | `Categoria` | `Transactions.Columns.Category` (line 226) |
| Account (header) | `Conta` (singular grid usage; `Conta de Origem` for origin-account header) | lines 724-727, 234-235 |
| Period | `Período` | `FixedExpenses.Columns.Period` (line 388) |
| Total | `Total` | `Total` (line 1040) |
| Sats / Fiat | Keep untranslated: `Sats` | `Transactions.Columns.Sats` (line 238) — "Fiat" verify against file usage |

**Rules:** ≤1.5× English char count; preserve trailing period on `GoalSummary_EmptyMessage`; neutral product register.

---

### `src/Valt.UI/Lang/language.es.resx` (config, transform)

**Analog:** Same sibling keys. Established terminology:
| English key | es established translation | Source key in file |
|---|---|---|
| Close | `Cerrar` | `LoanStateHistory_Close` (line 2920) |
| Category | `Categoría` | `Transactions.Columns.Category` (line 313) |
| Account (header) | `Cuenta` / `Cuenta origen` | lines 315-316 |
| Period | `Frecuencia` (existing `FixedExpenses.Columns.Period` translation — note: differs semantically from "Period"; evaluate against English "Period" label per rule 1; consider `Período` if used elsewhere) | line 415 |
| Total | `Total` | `Total` (line 1070) |
| Sats / Fiat | Keep untranslated: `Sats` | `Transactions.Columns.Sats` (line 322) |

**Insertion point:** after `Goals_YearlyIndicator` (line 2048), mirroring en file block order.

---

### `src/Valt.Infra/Mcp/Server/McpServerService.cs` (verify-only)

**Analog:** `ForwardServicesFromMainApp()` lines 249-281. `IQueryDispatcher` is forwarded at line 256:
```csharp
// Command/Query dispatchers (needed by all CRUD tools)
services.AddSingleton(_appServices.GetRequiredService<ICommandDispatcher>());
services.AddSingleton(_appServices.GetRequiredService<IQueryDispatcher>());
```
**Expected action:** no modification — the new tool flows through the already-forwarded dispatcher. Plan should include a verification step only. Modify only if the tool needs a service not in this list.

---

### `.claude/docs/goals.md` (documentation)

**Analog:** Same file. Existing section outline (headings at lines 1-433):
- Domain Layer / Infrastructure Layer / UI Layer / Key Patterns / Testing / Data Flow Example / File Structure
- `### GoalTransactionReader` section (line 138) is the closest analog for documenting a read path.

**Pattern to follow:** append a new subsection documenting (per CONTEXT.md GOL-10): the View Summary flow (UI: GoalsPanelViewModel → `GetGoalContributingTransactionsQuery` → modal), the App-layer query + per-strategy derivation (`IGoalQueries.GetContributingTransactionsAsync`, per-strategy calculators, `Supported`/`NotSupported` semantics), and the new MCP tool `GetGoalContributingTransactions`. Keep the doc's existing terse bullet style and cross-reference file paths.

## Shared Patterns

### MCP tool conventions (apply to GoalTools.cs change + test)
**Source:** `src/Valt.Infra/Mcp/Tools/GoalTools.cs` + AGENTS.md MCP section
- `[McpServerToolType]` on class, `[McpServerTool, Description("...")]` on static methods
- Dispatchers injected as method parameters (`IQueryDispatcher`, `ICommandDispatcher`)
- Read-only tools: no `McpDataChangedNotification`; mutating tools publish it (lines 73, 102, …)
- MCP-owned DTOs declared in the tool file

### Result-type query handling
**Source:** `GetGoalContributingTransactionsHandler.cs`
**Apply to:** MCP tool mapping
```csharp
Result<GoalContributingTransactionsResult>.NotFound("Goal", query.GoalId)
// vs
Result<GoalContributingTransactionsResult>.Success(result)
```

### Integration test base
**Source:** `tests/Valt.Tests/IntegrationTest.cs`
**Apply to:** new GoalToolsTests
- Inherit `IntegrationTest`; `[OneTimeSetUp] AddApplicationLayer()` calls `_serviceCollection.AddValtApp()` + `RebuildServiceProvider()`
- In-memory LiteDB via `OpenInMemoryDatabase(MemoryStream)`
- Builders for all test data (`GoalBuilder`, `TransactionBuilder`, `FiatAccountBuilder`)

### Localization key discipline
**Source:** UI-SPEC §Translation tone rules + AGENTS.md
**Apply to:** both resx files
- 4-line `<data name="..." xml:space="preserve"><value>...</value></data>` blocks
- Terminology consistency with sibling keys (tables above)
- All keys translated in BOTH pt-BR and es — key sets must mirror the en file exactly
- No Designer.cs changes (properties already exist)

## No Analog Found

None — all six files have exact in-codebase analogs.

## Metadata

**Analog search scope:** `src/Valt.Infra/Mcp/`, `src/Valt.App/Modules/Goals/`, `src/Valt.UI/Lang/`, `tests/Valt.Tests/Infra/Mcp/Tools/`, `tests/Valt.Tests/Application/Goals/`, `.claude/docs/goals.md`
**Files scanned:** 14 (GoalTools.cs, McpServerService.cs, IntegrationTest.cs, AssetToolsSoldStateTests.cs, language.resx, language.pt-BR.resx, language.es.resx, language.Designer.cs, GetGoalContributingTransactions handler/query, GoalContributingTransactionsResult.cs, ContributingTransactionRow.cs, handler tests, goals.md)
**Pattern extraction date:** 2026-10-07
