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
  - `Common/` - Value objects (`BtcValue`, `FiatValue`, `FiatCurrency`, `Icon`)

- **Valt.Infra** - Infrastructure layer: persistence, external services
  - `DataAccess/` - LiteDB database access
  - `Modules/` - Repositories, queries, DTOs, reports
  - `Crawlers/` - Price providers (Kraken, Coinbase, Frankfurter)
  - `Kernel/BackgroundJobs/` - Periodic tasks
  - `Services/` - Updates, CSV import

- **Valt.UI** - Avalonia desktop application
  - `Views/Main/Tabs/` - Transactions, Reports, AvgPrice tabs
  - `Views/Main/Modals/` - Modal dialogs
  - `Base/` - ViewModel base classes
  - `State/` - Application state (RatesState, AccountsTotalState, FilterState)

### Key Patterns

- **MVVM**: ViewModels inherit from `ValtViewModel` (CommunityToolkit.Mvvm)
- **Domain Events**: Aggregate roots emit events via `AddEvent()`, published through `IDomainEventPublisher`
- **Factory Pattern**: `IModalFactory`, `IPageFactory` for view creation
- **Strategy Pattern**: `IAvgPriceCalculationStrategy`, `IGoalProgressCalculator`
- **Weak Messaging**: `WeakReferenceMessenger` for loosely coupled updates

### Domain Layer Constraints

- **No attributes in domain classes**: No `[JsonPropertyName]`, `[BsonField]`, etc.
- **Use DTOs for serialization**: Create separate DTO classes in Infra layer
- Example: `StackBitcoinGoalType` (domain) maps to `StackBitcoinGoalTypeDto` (infra)

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
│   └── Common/          # Value objects
├── Valt.Infra/          # Infrastructure layer
│   ├── DataAccess/      # LiteDB, migrations
│   ├── Modules/         # Repositories, queries, reports
│   ├── Crawlers/        # Price providers
│   └── Services/        # Updates, CSV import
└── Valt.UI/             # Presentation layer
    ├── Views/Main/      # Tabs and modals
    ├── State/           # Application state
    └── Lang/            # Localization
```
