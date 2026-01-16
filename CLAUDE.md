# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Valt is a personal budget management desktop application for bitcoiners, built with .NET 9 and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, and displays values in bitcoin terms. The application includes advanced features like cost basis tracking with multiple calculation methods (Brazilian tax rule, FIFO) and comprehensive reporting.

## Build and Run Commands

```bash
# Build the solution
dotnet build Valt.sln

# Run the application
dotnet run --project src/Valt.UI/Valt.UI.csproj

# Run all tests
dotnet test

# Run a specific test file
dotnet test --filter "FullyQualifiedName~Valt.Tests.Domain.Budget.Transactions.TransactionTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~Valt.Tests.Domain.Budget.Transactions.TransactionTests.MethodName"
```

## Architecture

### Layered Structure

The solution follows clean architecture with three layers:

- **Valt.Core** - Domain layer with entities, value objects, and domain events. No external dependencies except DI abstractions. Contains:
  - `Kernel/` - Base classes (`Entity<T>`, `AggregateRoot<T>`), domain event system, repository interfaces, ID generation
  - `Modules/Budget/` - Domain models for Accounts, Categories, Transactions, FixedExpenses
  - `Modules/AvgPrice/` - Cost basis tracking with multiple calculation strategies
  - `Common/` - Value objects (`BtcValue`, `FiatValue`, `FiatCurrency`, `Icon`, `DateOnlyRange`)

- **Valt.Infra** - Infrastructure layer implementing persistence, external services, and cross-cutting concerns:
  - `DataAccess/` - LiteDB database access (`ILocalDatabase`, `IPriceDatabase`)
  - `Modules/` - Repository implementations, query handlers, and DTOs
  - `Crawlers/` - External price data providers (Kraken, Coinbase, Frankfurter) for live and historical prices
  - `Kernel/BackgroundJobs/` - Background job system for periodic tasks (price updates, auto-sat calculation)
  - `Settings/` - Persistent settings (currency, display preferences)
  - `Modules/Reports/` - Report generators (MonthlyTotals, ExpensesByCategory, AllTimeHigh, Statistics)
  - `Services/Updates/` - GitHub update checker for version management
  - `Services/CsvImport/` - CSV import services (parser, executor, template generator)

- **Valt.UI** - Avalonia desktop application:
  - `Views/Main/Tabs/` - Three main tabs (Transactions, Reports, AvgPrice)
  - `Views/Main/Modals/` - 18 modal dialogs for various operations
  - `Base/` - Base ViewModel classes (`ValtViewModel`, `ValtModalViewModel`, `ValtTabViewModel`, `ValtValidatorViewModel`)
  - `State/` - Application state objects (RatesState, AccountsTotalState, FilterState, LiveRateState)
  - `UserControls/` - Reusable controls (BtcInput, FiatInput, CustomTitleBar, etc.)
  - `Converters/` - XAML value converters

### Key Patterns

- **MVVM**: ViewModels inherit from `ValtViewModel` (using CommunityToolkit.Mvvm) and use compiled bindings
- **Domain Events**: Aggregate roots emit events via `AddEvent()`, published through `IDomainEventPublisher`
- **Dependency Injection**: Services registered in `Extensions.cs` files per project, composed in `App.axaml.cs`
- **Factory Pattern**: `IModalFactory`, `IPageFactory` for creating views with their ViewModels
- **Strategy Pattern**: `IAvgPriceCalculationStrategy` for different cost basis calculation methods
- **Weak Messaging**: `WeakReferenceMessenger` for loosely coupled state updates

### Domain Layer Constraints

- **No attributes in domain classes**: Domain entities, value objects, and goal types must not use serialization attributes (e.g., `[JsonPropertyName]`, `[JsonConstructor]`, `[BsonField]`). The domain layer should remain infrastructure-agnostic.
- **Use DTOs for serialization**: Create separate DTO classes in the Infra layer to handle serialization. Map between domain objects and DTOs in the repository/extension classes.
- Example: `StackBitcoinGoalType` (domain) maps to `StackBitcoinGoalTypeDto` (infra) for JSON serialization.

### Database

- Uses LiteDB (embedded NoSQL database) with password protection
- Two separate databases:
  - **Local database**: accounts, transactions, categories, fixed expenses, avg price profiles, settings
  - **Price database**: historical Bitcoin and fiat prices (shared across instances)
- Entity classes in Infra layer (e.g., `TransactionEntity`) map to/from domain models
- Migrations managed by `MigrationManager` with scripts implementing `IMigrationScript`
- Account caching via `AccountCacheService` for performance optimization

## Domain Modules

### Budget Module

#### Accounts
- **FiatAccount**: Fiat currency accounts with initial amount and currency
- **BtcAccount**: Bitcoin accounts storing value in satoshis
- Both support: name, icon, visibility, display order

#### Transactions
Six transaction detail types for different scenarios:
- `FiatDetails` - Single fiat account credit/debit
- `BitcoinDetails` - Single bitcoin account credit/debit
- `FiatToFiatDetails` - Transfer between fiat accounts
- `BitcoinToBitcoinDetails` - Transfer between bitcoin accounts
- `FiatToBitcoinDetails` - Exchange fiat to bitcoin
- `BitcoinToFiatDetails` - Exchange bitcoin to fiat

**AutoSatAmount Feature**: Eligible fiat transactions can automatically calculate their satoshi equivalent based on historical BTC prices. States: `Pending`, `Processed`, `Manual`, `Missing`.

#### Categories
Hierarchical categories with optional parent ID for nesting. Support custom icons and colors.

#### Fixed Expenses
Recurring expenses with:
- Multiple periods: Monthly, Weekly, Biweekly, Yearly
- Ranges with different amounts over time
- Can be linked to specific accounts or currencies
- Record tracking for expense history

### AvgPrice Module (Cost Basis Tracking)

Tracks cost basis for bitcoin holdings with two calculation methods:
- **BrazilianRule**: Brazilian tax law cost basis calculation
- **FIFO**: First-In-First-Out accounting

Key entities:
- **AvgPriceProfile**: Aggregate root containing lines and calculation settings
- **AvgPriceLine**: Individual buy/sell/setup entries with date, quantity, amount

## Infrastructure Services

### Background Jobs

| Job | Interval | Purpose |
|-----|----------|---------|
| `LivePricesUpdaterJob` | 30s | Fetches current BTC/fiat prices |
| `BitcoinHistoryUpdaterJob` | 120s | Updates historical BTC prices |
| `FiatHistoryUpdaterJob` | 120s | Updates historical fiat rates |
| `AutoSatAmountJob` | 120s | Calculates sat amounts for eligible transactions |
| `AccountTotalsJob` | 5s | Refreshes account cache on date changes |

### Price Providers
- **Coinbase**: Live BTC prices
- **Kraken**: Historical BTC OHLC data
- **Frankfurter**: Fiat currency rates (live and historical)

### Reports
- `MonthlyTotalsReport` - Monthly income/expense tracking
- `ExpensesByCategoryReport` - Breakdown by category with percentages
- `AllTimeHighReport` - Peak portfolio value tracking
- `StatisticsReport` - Median expenses, wealth coverage calculation

### CSV Import Services

The CSV Import feature provides bulk transaction import from CSV files:

**Services:**
- `ICsvImportParser` / `CsvImportParser` - Parses CSV files with strict validation using CsvHelper library
- `ICsvTemplateGenerator` / `CsvTemplateGenerator` - Generates sample CSV templates demonstrating all transaction types
- `ICsvImportExecutor` / `CsvImportExecutor` - Executes the import: creates accounts, categories, and transactions

**CSV Format:**
- Required columns: `date` (YYYY-MM-DD), `description`, `amount`, `account`, `category`
- Optional columns: `to_account`, `to_amount` (for transfers)
- Account naming: `AccountName [CurrencyCode]` for fiat (e.g., "Checking [USD]"), `AccountName [btc]` for bitcoin
- Amount conventions: negative = debit, positive = credit

**DTOs:**
- `CsvImportRow` - Parsed CSV row with date, description, amount, account, category, optional transfer fields
- `CsvImportResult` - Parsing result with success status, rows, and errors (supports partial success)
- `CsvImportExecutionResult` - Execution outcome with counts of created entities
- `CsvImportProgress` - Progress tracking (current row, total, action, percentage)
- `CsvAccountMapping` / `CsvCategoryMapping` - Mapping configurations for accounts and categories

**Key Features:**
- Smart account type detection from bracket suffixes ([USD], [btc], etc.)
- Automatic transaction detail type inference based on source/destination account types
- Background job suspension during import to prevent interference
- Real-time progress reporting via `IProgress<CsvImportProgress>`

### Version Checking
`GitHubUpdateChecker` checks for new releases from the GitHub repository and supports in-app updates.

## UI Components

### Main Tabs
1. **Transactions**: Account list, transaction list, fixed expenses entry, wealth summary
2. **Reports**: Charts (monthly totals, expenses by category), all-time high dashboard
3. **Average Price**: Cost basis profiles, buy/sell/setup line management

### Modal Dialogs (18)
- Database: `InitialSelection`, `CreateDatabase`, `InputPassword`, `ChangePassword`
- Accounts: `ManageAccount`, `IconSelector`
- Transactions: `TransactionEditor`, `MathExpression`, `ChangeCategoryTransactions`
- Categories: `ManageCategories`
- Fixed Expenses: `ManageFixedExpenses`, `FixedExpenseEditor`, `FixedExpenseHistory`
- AvgPrice: `ManageAvgPriceProfiles`, `AvgPriceLineEditor`
- Import: `ImportWizard` - Multi-step CSV import wizard
- System: `Settings`, `StatusDisplay`, `About`

### Import Wizard

A 5-step wizard for importing transactions from CSV files:

1. **File Selection** - Select CSV file, download template, view parsing results/errors
2. **Account Mapping** - Map CSV account names to existing or new accounts (auto-detects BTC vs fiat)
3. **Category Preview** - Map CSV categories to existing or create new ones
4. **Summary** - Review import counts before execution
5. **Progress** - Real-time progress tracking during import

**Key Components:**
- `ImportWizardViewModel` - Main wizard ViewModel with step navigation and import execution
- `ImportWizardView.axaml` - Fluent-styled modal with step indicator and navigation buttons
- `AccountMappingItem` / `CategoryMappingItem` - UI models for mapping configuration
- `StepConverters` - XAML converters for step indicator styling

### Custom Controls
- `BtcInput` - Bitcoin/satoshi input with format toggle
- `FiatInput` - Currency input with validation
- `DateCalendarSelector` - Date picker with calendar
- `ColorPalettePicker` - Color selection
- `CustomTitleBar` - Windows custom title bar
- `UpdateIndicator` - Version update notification

### State Objects
- `RatesState` - Global exchange rates (BTC/fiat)
- `AccountsTotalState` - Aggregate wealth calculation
- `FilterState` - Date range filtering for reports
- `LiveRateState` - Live price with historical comparison

## Testing

### Test Framework & Tools
- Tests use NUnit with NSubstitute for mocking
- `DatabaseTest` base class provides in-memory LiteDB setup
- `IntegrationTest` base class for tests requiring full DI container
- Architecture tests using NetArchTest.Rules verify layer dependencies

### Test Guidelines

**IMPORTANT: Always use Builder classes when creating test data.**

1. **Use Builders for Test Data**: All domain objects in tests should be created using Builder classes from `tests/Valt.Tests/Builders/`. This ensures:
   - Tests remain resilient to constructor changes
   - Consistent test data creation patterns
   - Better readability with fluent API

2. **Builder Pattern Examples**:
   ```csharp
   // Good - Using builders with static factory methods
   var line = AvgPriceLineBuilder.ABuyLine()
       .WithDate(new DateOnly(2024, 1, 1))
       .WithQuantity(1m)
       .WithAmount(FiatValue.New(50000m))
       .Build();

   var account = FiatAccountBuilder.AnAccount()
       .WithName("My Account")
       .WithFiatCurrency(FiatCurrency.Brl)
       .Build();

   var category = CategoryBuilder.ACategory()
       .WithName("Groceries")
       .Build();

   // Avoid - Direct construction (brittle to changes)
   var line = AvgPriceLine.Create(id, date, order, type, btc, fiat, comment, totals);
   ```

3. **Test Structure**: Follow Arrange-Act-Assert pattern with clear comments:
   ```csharp
   [Test]
   public void Should_Calculate_Total_After_Buy()
   {
       // Arrange: Set up initial state
       var profile = AvgPriceProfileBuilder.AProfile().Build();

       // Act: Perform the action being tested
       profile.AddLine(...);

       // Assert: Verify expected outcomes
       Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(1));
   }
   ```

4. **Test Naming**: Use descriptive names that explain the scenario:
   - `Should_[ExpectedBehavior]_When_[Condition]`
   - `Should_[ExpectedBehavior]_For_[Scenario]`

5. **IdGenerator Setup**: For tests that create domain objects with IDs, configure the IdGenerator in `[OneTimeSetUp]`:
   ```csharp
   [OneTimeSetUp]
   public void OneTimeSetUp()
   {
       IdGenerator.Configure(new LiteDbIdProvider());
   }
   ```

6. **Available Builders** (in `tests/Valt.Tests/Builders/`):
   - `TransactionBuilder.ATransaction()` - For Transaction entities
   - `CategoryBuilder.ACategory()` - For Category entities
   - `FiatAccountBuilder.AnAccount()` - For Fiat Account entities
   - `BtcAccountBuilder.AnAccount()` - For Bitcoin Account entities
   - `FixedExpenseBuilder.AFixedExpense()` / `.AFixedExpenseWithAccount()` / `.AFixedExpenseWithCurrency()` - For FixedExpense entities
   - `AvgPriceLineBuilder.ABuyLine()` / `.ASellLine()` / `.ASetupLine()` - For AvgPriceLine entities
   - `AvgPriceProfileBuilder.AProfile()` / `.ABrazilianRuleProfile()` / `.AFifoProfile()` - For AvgPriceProfile entities
   - `FakeClock` - For time-dependent tests

7. **Test Organization**: Use `#region` blocks to group related tests:
   ```csharp
   #region Creation Tests
   [Test]
   public void Should_Create_...() { }
   #endregion

   #region Validation Tests
   [Test]
   public void Should_Throw_...() { }
   #endregion
   ```

### Test Categories
Tests are organized by layer:
- `Domain/` - Domain model tests (Accounts, Categories, Transactions, FixedExpenses, AvgPrice, Common value objects)
- `UseCases/` - Query handler tests
- `UI/` - ViewModel and converter tests
- `Reports/` - Report generator tests
- `Jobs/` - Background job tests
- `Architecture/` - Architecture constraint tests
- `HistoricPriceCrawlers/`, `LivePriceCrawlers/` - External service integration tests
- `CsvImport/` - CSV import parser, executor, and template generator tests

## UI Framework

- Avalonia 11.3 with Fluent theme
- LiveChartsCore.SkiaSharpView for charts in Reports
- AXAML files for views with code-behind `.axaml.cs` files
- Localization via `.resx` files in `Lang/` (en-US, pt-BR, es)
- Custom fonts: Geist (sans), GeistMono (mono), Phosphor (icons), MaterialDesign (icons)

### Localization Requirements

**IMPORTANT: When adding or modifying localization strings, you MUST update ALL THREE language files:**

1. `language.resx` - English (en-US) - Primary/default language
2. `language.pt-BR.resx` - Portuguese (Brazilian)
3. `language.es.resx` - Spanish

Also update `language.Designer.cs` with the new property for each new string.

Steps for adding a new localized string:
1. Add the `<data>` entry to `language.resx` (English)
2. Add the `<data>` entry to `language.pt-BR.resx` (Portuguese translation)
3. Add the `<data>` entry to `language.es.resx` (Spanish translation)
4. Add the static property to `language.Designer.cs`

## Key Value Objects

| Type | Description |
|------|-------------|
| `BtcValue` | Bitcoin amount stored in satoshis (long), with Btc property for conversion |
| `FiatValue` | Decimal fiat amount rounded to 2 decimals |
| `FiatCurrency` | Currency definition (32 supported: USD, BRL, EUR, etc.) |
| `Icon` | Serializable icon with name, unicode, and color |
| `DateOnlyRange` | Date range abstraction |

## File Organization

```
src/
├── Valt.Core/           # Domain layer
│   ├── Kernel/          # Base classes, events, ID generation
│   ├── Modules/
│   │   ├── Budget/      # Accounts, Categories, Transactions, FixedExpenses
│   │   └── AvgPrice/    # Cost basis tracking
│   └── Common/          # Shared value objects
├── Valt.Infra/          # Infrastructure layer
│   ├── DataAccess/      # LiteDB access, migrations
│   ├── Modules/         # Repositories, queries, reports
│   ├── Crawlers/        # Price data providers
│   ├── Kernel/          # Background jobs, events, scopes
│   ├── Settings/        # Persistent settings
│   └── Services/        # Updates, CsvImport (parser, executor, templates)
└── Valt.UI/             # Presentation layer
    ├── Base/            # ViewModel base classes
    ├── Views/Main/      # Tabs and modals
    ├── State/           # Application state
    ├── UserControls/    # Reusable controls
    ├── Converters/      # XAML converters
    ├── Services/        # Factories, helpers
    ├── Lang/            # Localization resources
    └── Styles/          # Theme customization
```
