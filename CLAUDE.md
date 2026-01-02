# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Valt is a personal budget management desktop application for bitcoiners, built with .NET 9 and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, and displays values in bitcoin terms.

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
  - `Kernel/` - Base classes (`Entity<T>`, `AggregateRoot<T>`), domain event system, repository interfaces
  - `Modules/Budget/` - Domain models for Accounts, Categories, Transactions, FixedExpenses
  - `Common/` - Value objects (`BtcValue`, `FiatValue`, `FiatCurrency`)

- **Valt.Infra** - Infrastructure layer implementing persistence, external services, and cross-cutting concerns:
  - `DataAccess/` - LiteDB database access (`ILocalDatabase`, `IPriceDatabase`)
  - `Modules/` - Repository implementations, query handlers, and DTOs
  - `Crawlers/` - External price data providers (Kraken, Coinbase, Frankfurter)
  - `Kernel/BackgroundJobs/` - Background job system for periodic tasks
  - `Settings/` - Persistent settings (currency, display preferences)
  - `Modules/Reports/` - Report generators (MonthlyTotals, ExpensesByCategory, AllTimeHigh)

- **Valt.UI** - Avalonia desktop application:
  - `Views/Main/` - Main window and tab views (Transactions, Reports)
  - `Views/Main/Modals/` - Modal dialogs (TransactionEditor, ManageAccount, Settings, etc.)
  - `Base/` - Base ViewModel classes (`ValtViewModel`, `ValtModalViewModel`, `ValtTabViewModel`)
  - `State/` - Application state objects (RatesState, AccountsTotalState, FilterState)

### Key Patterns

- **MVVM**: ViewModels inherit from `ValtViewModel` (using CommunityToolkit.Mvvm) and use compiled bindings
- **Domain Events**: Aggregate roots emit events via `AddEvent()`, published through `IDomainEventPublisher`
- **Dependency Injection**: Services registered in `Extensions.cs` files per project, composed in `App.axaml.cs`
- **Factory Pattern**: `IModalFactory`, `IPageFactory` for creating views with their ViewModels

### Database

- Uses LiteDB (embedded NoSQL database)
- Two separate databases: local database (accounts, transactions) and price database (historical prices)
- Entity classes in Infra layer (e.g., `TransactionEntity`) map to/from domain models
- Migrations managed by `MigrationManager` with scripts implementing `IMigrationScript`

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
   - `AvgPriceProfileBuilder.AProfile()` / `.ABrazilianRuleProfile()` - For AvgPriceProfile entities
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

## UI Framework

- Avalonia 11.3 with Fluent theme
- LiveChartsCore.SkiaSharpView for charts in Reports
- AXAML files for views with code-behind `.axaml.cs` files
- Localization via `.resx` files in `Lang/` (en-US, pt-BR)
