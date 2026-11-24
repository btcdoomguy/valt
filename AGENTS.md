<!-- gsd-path:begin -->
# AGENTS.md — Operating Rules for the GSD Path Pipeline

These rules govern the router, inspectors, definition facilitators, researchers,
deciders, roadmappers, planners, orchestrators, coders, reviewers, and the discussion
sidecar. Role briefs are bundled with the installed skills. These rules win over role instructions except where the user says
otherwise.

## Authority order

1. The user, in chat.
2. `.project/CHARTER.md` (program flow): program scope, vetoes, and
   corrections bind every milestone. Vetoes may not be researched, planned,
   or built.
3. `.project/intent/INTENT.md`: constraints, vetoes, corrections, and success
   criteria are hard limits. Vetoes may not be researched, planned, or built.
   Only `$gsd-path-define` changes a success criterion, by appending
   `## Corrections`; a task Log or review cannot waive one.
4. `.project/SYNTHESIS.md` (program flow, top level) or
   `.project/research/SYNTHESIS.md` (single milestone): gated decisions are
   settled. Report conflicts; do not override them.
5. The current phase brief or task file.

If two sources disagree, stop and surface the conflict. Never average.

The orchestrator's isolated rerun of a task Verify is that task's evidence.
Wave and ship reviewers read that recorded output plus the isolated diff;
they do not re-run the task command. PLAN.md's project Verify runs once, at
ship, in one sidecar through `workflow_run.py prepare-final`. That runtime owns
execution, output recording, collection, and retry reuse. A complete quick-lane
single full wave with explicit final scope and walkthrough evidence may also
supply final review when the runtime proves unchanged product and contracts.
In that case FINAL.md is a generated view, not a new model assignment.
The project gap is always a view of its recorded command result.
Do not repeat a proven claim or write another narrative of the same evidence.
Verification effort follows uncovered contract claims and actual integration
risk; code line counts and token ratios are observations, never scope targets.
A task Verify must not copy the project command unless an
owned success criterion names it. Any other task Verify must name a path
from that task's `files`. INTENT constraints about not running the
full-repo suite on a tiny edit outrank the phase brief.

## Files are the only memory

- Start from disk, not conversation. If an input is absent from `.project/`,
  report it instead of inventing it.
- A discussion answer with `Status: final` or `NEEDS-USER`, `Follow-up:
  required`, and no later `Disposition X###` receipt is pending. Before phase
  work and again before a phase gate, the router and current phase run the
  active skill's bundled `scripts/discussion_records.py pending --repo
  <absolute-root>` helper; when `.project/discuss/` is absent it returns an
  empty list, so continue. Do not parse IDs, pair records, recover writes, or
  route pending receipts through model reasoning. The named owner either
  updates the target artifact through a legal current-phase gate and uses the
  helper's `dispose` command to append an `applied` disposition, or appends
  `acknowledged-no-change` with evidence. If applying it would rewrite an
  approved earlier-phase contract or the owner cannot legally enter, block the
  current phase with links to ANSWERS.md and the target artifact and ask the
  user; never auto-advance or archive it. Only the user may authorize
  `rejected-by-user`.
- After a phase completes its gate and state update, run the bundled
  `pipeline_state.py status --repo <absolute-root>` with the routed
  `--project-dir`. Its `handoff` is the shared phase handoff for chat and the
  dashboard. Present **Outcome** from `handoff.outcome` plus the completed
  work; **Review** links the phase artifact, or `handoff.review` when absent.
  An active router uses `handoff.router_next` for **Next** and follows the
  returned route. A directly invoked phase uses `handoff.phase_next` and
  stops. A status or plain-prompt reply uses `handoff.next` and stays read-only.
  Convert the `$` invocation prefix to the host's slash form when needed.
  Phase owners still stop for required input and approval before completion;
  a route is not approval. Return to an active caller instead of invoking an
  explicit-only sibling skill. Derive the next phase from this result, not
  from another lane table in prose.
- Phase skills bundle the remaining rules in `references/operating-rules.md`;
  read it before phase work.

## Plain-prompt re-entry

<!-- gsd-path/plain-prompt-reentry/v1 -->

- On a turn that did not explicitly invoke a GSD Path skill, check for
  `.project/STATE.md`. When it exists, first select the compatible interpreter:
  probe `python3 -B -c "import sys; raise SystemExit(sys.version_info < (3, 9))"`,
  then probe `python -B -c "import sys; raise SystemExit(sys.version_info < (3, 9))"`
  only if the first command fails. Run the first successful interpreter
  with `-B .gsd-path/status_runtime.py --repo <absolute-root>`
  before any requested repository mutation. Treat its JSON as the only route
  authority.
  When `completion.status` is `verified` and `git.branch` is an ordinary
  branch (not `gsd-path/M###`), requested product work may proceed normally.
  Archives and pipeline control files remain protected. Otherwise,
  a plain change request does not authorize work outside the pipeline: make no
  changes and report **Outcome**, link the returned `path` under **Review**, and
  under **Next** use `handoff.next`. An explicit request to leave
  or bypass the pipeline is a user ruling;
  route it through the router's undo or abandon flow instead of editing directly.
- After any other plain-prompt turn with owned state, rerun the same read-only
  status command immediately before the final response and append the same
  **Outcome** / **Review** / **Next** handoff with the same shared handoff rule.
  Keep an informational turn read-only from start to finish: do not use repository
  mutation tools or run commands that can create or alter files. A GSD Path skill
  already supplies this handoff, so emit it once. With no STATE.md, respond
  normally and do not initialize the pipeline. A missing runtime or invalid
  status blocks mutation and routes to `$gsd-path-forensics`.

## Evidence and honesty

- Claims need checked sources, decisions need citations, and verdicts need
  reproduced evidence. Never report a command as passing unless it ran.
- Report partial or failed results plainly. Silent partial success is the
  worst outcome.
- Preserve uncertainty as `NEEDS-USER`, `RESEARCH`, or a confidence level.
  Questions never silently disappear between phases.
- Record user corrections and vetoes verbatim.

## Gates

- Same-wave dependencies execute in dependency order; a task dispatches as
  soon as its dependencies land, never idling behind unrelated
  in-flight tasks. Only ready tasks run.
- Parallel dispatch rounds use distinct linked worktrees, each created at the
  clean primary HEAD recorded as its task base at dispatch. A serial dispatch
  round (one ready task) works and lands on the bound branch in the primary
  worktree. Task and reviewer Verify run against that recorded base plus only
  the task patch; evidence from a shared worktree or combined branch tip does
  not count.
- A wave advances only after every task and the wave review pass.
- Final review blocks on any `not-met`, `unverifiable`, or blocked gap verdict.
- STATE.md becomes `shipped` only when every final verdict and the project
  verify pass; the router reports shipped only after the integration
  validator (`validate-integrated`) passes.
- Shipping archives the milestone: artifacts move to
  `.project/archive/<NNN>-<slug>/` with a manifest, and the ship phase
  records the ship commit (STATE.md, final reviews, archive) as its single
  commit on the bound branch. Archives are read-only —
  no agent may modify or delete them — and a new milestone may not begin
  while an un-archived shipped milestone's artifacts sit in the active paths.
- STATE.archive is the crash-recovery transaction id. It is persisted before
  moves and never recomputed. Keep review active until the prepared archive,
  canonical contents, carry-forward, and manifest pass the bundled precommit
  validator; report shipped only after the exact `.project/`-only ship commit
  passes the postcommit validator and `validate-integrated` proves the
  integration landing, its tag, and its ancestry on origin/main —
  while integration is pending the router routes back to ship instead of
  reporting shipped or starting the next milestone.

## Asking the user

- Ask through an interactive user-input tool when the runtime provides one;
  otherwise ask concise numbered questions in chat and stop for the reply.
- Before any approval, ruling, `NEEDS-USER` question, blocked escalation, or
  phase-completion handoff, present three things in order: **Outcome** — what
  was produced or learned; **Review** — a Markdown link to the primary
  canonical artifact using its resolved absolute path; **Next** — the one
  question or action now required. Any text the user is expected to send back
  verbatim — a ruling, an approval command, a reply — goes in its own fenced
  code block, never a blockquote or inline prose, so it pastes cleanly. If the
  host cannot render local links, print
  the resolved absolute path immediately after the link. On an output failure,
  link the malformed artifact when it exists; otherwise link STATE.md or the
  canonical log that proves the failure and say the expected artifact is
  missing. Never ask for approval before its reviewable artifact exists on
  disk, and never ask a bare "approve?" without its outcome and link.
- Every choice offered to the user names a recommended option — listed first
  and marked `(recommended)` — with a one-line reason grounded in evidence,
  intent, or the codebase, followed by the real alternatives. A pure values
  call with no evidence either way carries no recommendation; say so
  explicitly instead of inventing one.
- After a user decision, confirm what changed, link the updated artifact again,
  and state whether the active router continues automatically or which exact
  explicit skill the user should invoke next.

## Escalation

Stop and ask the user only when:

- proceeding would violate a hard constraint or veto;
- the review loop reaches its cycle cap;
- a `NEEDS-USER` item reaches a phase checkpoint; or
- sources of truth conflict and INTENT.md does not make the resolution clear.

Handle everything else autonomously and record it in the task log or
STATE.md.

## Style

- Write fixed-format data for the next agent, without pleasantries.
- Write short, plain-English outcomes for the user.
- Keep agent final messages to paths, statuses, verdicts, and evidence.

## Distribution layout

This section describes the GSD Path source checkout. These source paths and
the sync command do not apply to an application that only installs GSD Path.
In a consuming project, resolve roles, templates, and helpers from the active
installed skill's absolute paths; the application need not contain this layout.

| Path | Purpose |
|------|---------|
| `plugin.json` | Agent Plugins manifest (`agent-plugins.org` 1.0.0 schema) |
| `skills/` | Skills and aliases declared in `scripts/skill-resources.json` |
| `skills/gsd-path/templates/` | Required artifact formats |
| `skills/gsd-path/references/` | Agent role and dispatch contracts |
| `WORKFLOW.md` | Phase-by-phase SOP |

Edit canonical resources only: `skills/gsd-path/` templates and references,
each per-skill `SKILL.md`, `scripts/`, and
`platforms/shared-agents/dispatch.md`. Every per-skill `references/`,
`templates/`, and `scripts/` copy is generated — run `python3
scripts/sync_skill_resources.py` after editing a canonical source. Sync
overwrites divergent generated copies and warns when a divergent copy is
newer than its canonical source (the wrong-direction-edit signature); treat
that warning as a lost edit and re-apply it to the canonical path.
<!-- gsd-path:end -->

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Valt is a personal budget management desktop application for bitcoiners, built with .NET 9 and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, and displays values in bitcoin terms.

## Module Documentation

For detailed documentation on specific modules, see:
- **[Budget Module](.claude/docs/budget.md)** - Accounts, transactions, categories
- **[Reports Module](.claude/docs/reports.md)** - Financial analysis, charts, dashboards
- **[AvgPrice Module](.claude/docs/avgprice.md)** - Cost basis tracking (BrazilianRule, FIFO)
- **[Goals Module](.claude/docs/goals.md)** - Financial goal tracking with auto-progress
- **[Fixed Expenses Module](.claude/docs/fixedexpenses.md)** - Recurring expense management
- **[Assets Module](.claude/docs/assets.md)** - External investments (stocks, ETFs, crypto, real estate, leveraged positions)

## Build and Run Commands

```bash
dotnet build Valt.sln                    # Build solution
dotnet run --project src/Valt.UI/Valt.UI.csproj  # Run application
dotnet test                              # Run all tests
dotnet test --filter "FullyQualifiedName~Valt.Tests.Domain.Budget.Transactions.TransactionTests"  # Specific test file
```

## Architecture

### Layered Structure

- **Valt.Core** - Domain layer: entities, value objects, domain events. No external dependencies.
  - `Kernel/` - Base classes (`Entity<T>`, `AggregateRoot<T>`), events, ID generation
  - `Modules/Budget/` - Accounts, Categories, Transactions, FixedExpenses
  - `Modules/AvgPrice/` - Cost basis tracking
  - `Modules/Goals/` - Financial goals
  - `Modules/Assets/` - External investments
  - `Common/` - Value objects (`BtcValue`, `FiatValue`, `FiatCurrency`, `Icon`)

- **Valt.App** - Application layer: CQRS commands, queries, and business orchestration. See [CQRS Architecture](#cqrs-architecture).
  - `Kernel/` - Command/Query dispatchers, validation, Result type
  - `Modules/Budget/` - Account, Category, Transaction, FixedExpense commands and queries
  - `Modules/AvgPrice/` - Cost basis profile commands and queries
  - `Modules/Goals/` - Goal commands and queries
  - `Modules/Assets/` - Asset commands and queries

- **Valt.Infra** - Infrastructure layer: persistence, external services
  - `DataAccess/` - LiteDB database access
  - `Modules/` - Repositories, queries, reports
  - `Crawlers/` - Price providers (Kraken, Coinbase, Frankfurter)
  - `Kernel/BackgroundJobs/` - Periodic tasks
  - `Services/` - Updates, CSV import
  - `Mcp/` - MCP server and tools for AI assistant integration

- **Valt.UI** - Avalonia desktop application
  - `Views/Main/Tabs/` - Transactions, Reports, AvgPrice tabs
  - `Views/Main/Modals/` - Modal dialogs
  - `Base/` - ViewModel base classes
  - `State/` - Application state (RatesState, AccountsTotalState, FilterState)

### Key Patterns

- **MVVM**: ViewModels inherit from `ValtViewModel` (CommunityToolkit.Mvvm)
- **CQRS**: Commands and Queries through `ICommandDispatcher` and `IQueryDispatcher`
- **Domain Events**: Aggregate roots emit events via `AddEvent()`, published through `IDomainEventPublisher`
- **Factory Pattern**: `IModalFactory`, `IPageFactory` for view creation
- **Strategy Pattern**: `IAvgPriceCalculationStrategy`, `IGoalProgressCalculator`
- **Weak Messaging**: `WeakReferenceMessenger` for loosely coupled updates

### CQRS Architecture

**IMPORTANT: Always use the Valt.App layer for new modules and features.** The App layer implements CQRS (Command Query Responsibility Segregation) pattern to separate read and write operations.

#### Commands (Write Operations)

Commands modify data and are located in `Valt.App/Modules/{Module}/Commands/`:

```csharp
// Command definition
public sealed class CreateAssetCommand : ICommand<string>  // Returns asset ID
{
    public required string Name { get; init; }
    public required string CurrencyCode { get; init; }
    // ... other properties
}

// Validator
internal sealed class CreateAssetValidator : IValidator<CreateAssetCommand>
{
    public ValidationResult Validate(CreateAssetCommand command)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(command.Name))
            errors.Add(new("Name", "Name is required"));
        return new ValidationResult(errors.Count == 0, errors);
    }
}

// Handler
internal sealed class CreateAssetHandler : ICommandHandler<CreateAssetCommand, string>
{
    public async Task<Result<string>> HandleAsync(CreateAssetCommand command, CancellationToken ct)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<string>.Failure(new Error("VALIDATION_FAILED", "...", validation.Errors));

        // Domain logic here
        return Result<string>.Success(asset.Id.Value);
    }
}
```

For commands that don't return a value, use `ICommand<Unit>` and `ICommandHandler<TCommand, Unit>`.

#### Queries (Read Operations)

Queries read data and are located in `Valt.App/Modules/{Module}/Queries/`:

```csharp
// Query definition
public sealed class GetAssetsQuery : IQuery<IReadOnlyList<AssetDTO>> { }

// Handler
internal sealed class GetAssetsHandler : IQueryHandler<GetAssetsQuery, IReadOnlyList<AssetDTO>>
{
    public async Task<IReadOnlyList<AssetDTO>> HandleAsync(GetAssetsQuery query, CancellationToken ct)
    {
        return await _assetQueries.GetAllAsync();
    }
}
```

#### DTOs

DTOs for commands and queries are defined in `Valt.App/Modules/{Module}/DTOs/`. Use records with required init properties:

```csharp
public sealed record AssetDTO
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    // ... other properties
}
```

#### Using Dispatchers in ViewModels

ViewModels should use `ICommandDispatcher` and `IQueryDispatcher` instead of direct repository access:

```csharp
public class AssetsViewModel
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;

    public async Task LoadAsync()
    {
        var assets = await _queryDispatcher.DispatchAsync(new GetAssetsQuery());
    }

    public async Task DeleteAsync(string id)
    {
        var result = await _commandDispatcher.DispatchAsync(new DeleteAssetCommand { AssetId = id });
        if (result.IsFailure)
        {
            await ShowError(result.Error!.Message);
            return;
        }
        await LoadAsync();
    }
}
```

#### Result Type

Commands return `Result<T>` for railway-oriented error handling:

```csharp
var result = await _commandDispatcher.DispatchAsync(command);
if (result.IsFailure)
{
    // Handle error: result.Error contains code and message
    return;
}
// Use result.Value
```

#### Registration

Commands and queries are auto-registered via `Valt.App.Extensions.AddApplication()` which scans for implementations.

### Important: Cross-Layer Impact

**Always consider the impact of changes across all layers:**

- **MCP Layer**: When adding/modifying features, check if MCP tools need updates. See [MCP Server](#mcp-server) section for the checklist.
- **UI Layer**: New domain features may need corresponding ViewModels, Views, and localization strings.
- **Infra Layer**: Domain changes may require repository/query updates and DTO mappings.

### Domain Layer Constraints

- **No attributes in domain classes**: No `[JsonPropertyName]`, `[BsonField]`, etc.
- **Use DTOs for serialization**: Create separate DTO classes in Infra layer
- Example: `StackBitcoinGoalType` (domain) maps to `StackBitcoinGoalTypeDto` (infra)

### C# Coding Conventions

- **Use `Lock` class for synchronization**: Always use the strongly-typed `System.Threading.Lock` class instead of `lock(object)`. The `Lock` class (introduced in .NET 9) provides better performance and type safety.

```csharp
// Good
private readonly Lock _lock = new();
lock (_lock) { /* ... */ }

// Avoid
private readonly object _lock = new();
lock (_lock) { /* ... */ }
```

### Database

- LiteDB (embedded NoSQL) with password protection
- **Local database**: accounts, transactions, categories, fixed expenses, goals, avg price profiles
- **Price database**: historical BTC and fiat prices (shared)
- Migrations via `MigrationManager` with `IMigrationScript` implementations

## Background Jobs

| Job | Interval | Purpose |
|-----|----------|---------|
| `LivePricesUpdaterJob` | 30s | Fetches current BTC/fiat prices |
| `BitcoinHistoryUpdaterJob` | 120s | Updates historical BTC prices |
| `FiatHistoryUpdaterJob` | 120s | Updates historical fiat rates |
| `AutoSatAmountJob` | 120s | Calculates sat amounts for eligible transactions |
| `AccountTotalsJob` | 5s | Refreshes account cache |
| `GoalProgressUpdaterJob` | 120s | Recalculates stale goal progress |

## MCP Server

Valt includes an embedded MCP (Model Context Protocol) server that exposes application functionality to AI assistants like Claude. The server runs on a configurable port (default 5200) and provides tools for querying and manipulating budget data.

### Structure

```
src/Valt.Infra/Mcp/
├── Server/
│   ├── McpServerService.cs    # Server lifecycle management
│   └── McpServerState.cs      # Server state tracking
└── Tools/
    ├── Budget/
    │   ├── AccountTools.cs      # Account CRUD operations
    │   ├── TransactionTools.cs  # Transaction CRUD operations
    │   ├── CategoryTools.cs     # Category CRUD operations
    │   └── FixedExpenseTools.cs # Fixed expense operations
    ├── AssetTools.cs            # Asset management operations
    ├── AvgPriceTools.cs         # Cost basis profile operations
    ├── GoalTools.cs             # Financial goal operations
    ├── ReportTools.cs           # Financial reports
    └── CurrencyTools.cs         # Currency conversion and rates
```

### Creating MCP Tools

Tools are static methods decorated with `[McpServerTool]` in classes decorated with `[McpServerToolType]`:

```csharp
[McpServerToolType]
public class MyTools
{
    [McpServerTool, Description("Description shown to AI")]
    public static async Task<ResultDto> MyTool(
        IMyService service,  // Injected from DI
        [Description("Parameter description")] string param)
    {
        // Implementation
        return new ResultDto { ... };
    }
}
```

### Service Forwarding

Services needed by MCP tools must be forwarded from the main app's DI container in `McpServerService.ForwardServicesFromMainApp()`:

```csharp
services.AddSingleton(_appServices.GetRequiredService<IMyService>());
```

### MCP Impact Checklist

**IMPORTANT: When adding or modifying features, always consider MCP impact:**

1. **New query/command handlers**: Should this be exposed via MCP? Add corresponding tool if useful for AI assistants.
2. **New parameters on existing operations**: Update the corresponding MCP tool to expose the new parameter.
3. **New services**: If the service is needed by MCP tools, add it to `ForwardServicesFromMainApp()`.
4. **Breaking changes**: If modifying queries/commands used by MCP tools, update the tools accordingly.
5. **New DTOs**: MCP tools use their own DTOs (defined in the tool file) - don't reuse UI DTOs directly.

## Testing

### Framework & Tools
- NUnit with NSubstitute for mocking
- `DatabaseTest` base class for in-memory LiteDB
- `IntegrationTest` base class for full DI container
- NetArchTest.Rules for architecture verification

### Test Guidelines

**IMPORTANT: When implementing or modifying features, always consider the impact on the existing test methods and also always evaluate to create new tests if needed**

**Always use Builder classes for test data:**

```csharp
// Good
var account = FiatAccountBuilder.AnAccount()
    .WithName("Checking")
    .WithFiatCurrency(FiatCurrency.Usd)
    .Build();

var transaction = TransactionBuilder.ATransaction()
    .WithDate(new DateOnly(2024, 1, 1))
    .Build();

// Avoid direct construction
```

**Available Builders** (`tests/Valt.Tests/Builders/`):
- `TransactionBuilder`, `CategoryBuilder`
- `FiatAccountBuilder`, `BtcAccountBuilder`
- `FixedExpenseBuilder` (`.AFixedExpense()`, `.AFixedExpenseWithAccount()`, `.AFixedExpenseWithCurrency()`)
- `AvgPriceLineBuilder` (`.ABuyLine()`, `.ASellLine()`, `.ASetupLine()`)
- `AvgPriceProfileBuilder` (`.AProfile()`, `.ABrazilianRuleProfile()`, `.AFifoProfile()`)
- `GoalBuilder` (`.AGoal()`, `.AStackBitcoinGoal()`, `.AMonthlyGoal()`)
- `FakeClock`

**IdGenerator Setup** for tests creating domain objects:
```csharp
[OneTimeSetUp]
public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());
```

## UI Framework

- Avalonia 11.3 with Fluent theme
- LiveChartsCore.SkiaSharpView for charts
- Custom fonts: Geist, GeistMono, Phosphor, MaterialDesign

### Icons

**Always use the icon mapping file to find Material Design icons:**

The file `src/Valt.UI/Assets/Fonts/MaterialSymbolsOutlined-map.json` contains the mapping of icon names to their unicode values. When you need to use an icon:

1. Search the JSON file for the desired icon by name (e.g., "settings", "home", "check")
2. Use the `unicode` value with the `&#x` prefix in XAML or `\x` prefix in C#

```json
// Example entry in the map file
{ "name": "settings", "unicode": "E8B8", "category": "action" }
```

```xml
<!-- XAML usage -->
<TextBlock FontFamily="{DynamicResource MaterialDesign}" Text="&#xE8B8;" />
```

```csharp
// C# usage
public string SettingsIcon => "\xE8B8";
```

### Modal Windows

**Always set MinWidth and MinHeight for modal windows** to prevent content from being cut off on small screen resolutions:

```xml
<Window ...
        d:DesignWidth="600"
        d:DesignHeight="400"
        MinWidth="600"
        MinHeight="400"
        MaxWidth="600"
        MaxHeight="400"
        ... >
```

- Use `d:DesignWidth` and `d:DesignHeight` values as the minimum dimensions
- Modal views are located in `Views/Main/Modals/`
- All modals use `SystemDecorations="None"` with custom title bar (`CustomTitleBar`)

### Localization

**Update ALL THREE language files when adding strings:**
1. `language.resx` - English (en-US)
2. `language.pt-BR.resx` - Portuguese
3. `language.es.resx` - Spanish
4. `language.Designer.cs` - Add static property

## Key Value Objects

| Type | Description |
|------|-------------|
| `BtcValue` | Bitcoin in satoshis (long), with Btc decimal property |
| `FiatValue` | Decimal rounded to 2 decimals |
| `FiatCurrency` | 32 supported currencies |
| `Icon` | Name, unicode, color |

## File Organization

```
src/
├── Valt.Core/           # Domain layer
│   ├── Kernel/          # Base classes, events
│   ├── Modules/Budget/  # Accounts, Categories, Transactions, FixedExpenses
│   ├── Modules/AvgPrice/# Cost basis tracking
│   ├── Modules/Goals/   # Financial goals
│   ├── Modules/Assets/  # External investments
│   └── Common/          # Value objects
├── Valt.App/            # Application layer (CQRS)
│   ├── Kernel/          # Command/Query dispatchers, validation, Result type
│   ├── Modules/Budget/  # Account, Category, Transaction commands/queries
│   ├── Modules/AvgPrice/# AvgPrice commands/queries
│   ├── Modules/Goals/   # Goal commands/queries
│   └── Modules/Assets/  # Asset commands/queries
├── Valt.Infra/          # Infrastructure layer
│   ├── DataAccess/      # LiteDB, migrations
│   ├── Modules/         # Repositories, queries, reports
│   ├── Crawlers/        # Price providers
│   ├── Mcp/             # MCP server and tools
│   └── Services/        # Updates, CSV import
└── Valt.UI/             # Presentation layer
    ├── Views/Main/      # Tabs and modals
    ├── State/           # Application state
    └── Lang/            # Localization
```
