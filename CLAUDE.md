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
| `GoalProgressUpdaterJob` | 5s | Recalculates stale goal progress |

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
