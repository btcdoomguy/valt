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

- Tests use NUnit with NSubstitute for mocking
- `DatabaseTest` base class provides in-memory LiteDB setup
- `IntegrationTest` base class for tests requiring full DI container
- Architecture tests using NetArchTest.Rules verify layer dependencies
- Test builders in `tests/Valt.Tests/Builders/` for creating test data

## UI Framework

- Avalonia 11.3 with Fluent theme
- LiveChartsCore.SkiaSharpView for charts in Reports
- AXAML files for views with code-behind `.axaml.cs` files
- Localization via `.resx` files in `Lang/` (en-US, pt-BR)
