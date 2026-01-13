# Architecture

**Analysis Date:** 2026-01-13

## Pattern Overview

**Overall:** Clean Architecture Desktop Application (3-Layer)

**Key Characteristics:**
- Layered architecture with strict dependency direction (UI → Infra → Core)
- Domain-Driven Design with Aggregates, Events, and Value Objects
- MVVM pattern in UI with CommunityToolkit.Mvvm
- Event-driven communication between layers
- Background jobs for async operations

## Layers

**Core (Domain Layer):**
- Purpose: Domain models, business logic, rules independent of infrastructure
- Contains: Entities, Aggregates, Value Objects, Domain Events, Repository interfaces
- Location: `src/Valt.Core/`
- Depends on: Nothing (no external dependencies except DI abstractions)
- Used by: Infrastructure layer

**Infrastructure (Application/Persistence Layer):**
- Purpose: Repository implementations, data access, external services, background jobs
- Contains: LiteDB persistence, price crawlers, reports, event handlers
- Location: `src/Valt.Infra/`
- Depends on: Core layer (implements Core interfaces)
- Used by: UI layer

**UI (Presentation Layer):**
- Purpose: Avalonia desktop application with MVVM
- Contains: Views, ViewModels, State objects, User controls, Converters
- Location: `src/Valt.UI/`
- Depends on: Infrastructure layer (for services), Core layer (for domain types)
- Used by: End user

## Data Flow

**Transaction Creation Flow:**

1. User opens TransactionEditorModal via `IModalFactory` (`src/Valt.UI/Services/ModalFactory.cs`)
2. User enters data, submits form
3. ViewModel calls `Transaction.New()` to create domain aggregate (`src/Valt.Core/Modules/Budget/Transactions/Transaction.cs`)
4. Transaction emits `TransactionCreatedEvent` via `AddEvent()`
5. Repository saves entity and publishes events (`src/Valt.Infra/Modules/Budget/Transactions/TransactionRepository.cs`)
6. `IDomainEventPublisher` dispatches to handlers (`src/Valt.Infra/Kernel/EventSystem/DomainEventPublisher.cs`)
7. Background job `AutoSatAmountJob` calculates satoshi equivalent (`src/Valt.Infra/Kernel/BackgroundJobs/Jobs/AutoSatAmountJob.cs`)
8. UI updates via `WeakReferenceMessenger` notifications

**State Management:**
- Observable state objects: `RatesState`, `AccountsTotalState`, `FilterState`, `LiveRateState` (`src/Valt.UI/State/`)
- Weak reference messaging for loose coupling between ViewModels
- Background jobs refresh state at fixed intervals

## Key Abstractions

**AggregateRoot:**
- Purpose: Base class for domain aggregates with event collection
- Location: `src/Valt.Core/Kernel/AggregateRoot.cs`
- Examples: `Transaction`, `Account`, `Category`, `AvgPriceProfile`
- Pattern: Emit events via `AddEvent()`, collected in `Events` property

**Repository:**
- Purpose: Persistence abstraction for domain aggregates
- Interfaces: `IAccountRepository`, `ITransactionRepository`, `ICategoryRepository` (`src/Valt.Core/Modules/*/Contracts/`)
- Implementations: `src/Valt.Infra/Modules/*/` with `*Repository.cs`
- Pattern: Repository publishes domain events after save operations

**Background Job:**
- Purpose: Periodic async tasks (price updates, calculations)
- Interface: `IBackgroundJob` (`src/Valt.Infra/Kernel/BackgroundJobs/IBackgroundJob.cs`)
- Manager: `BackgroundJobManager` (`src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs`)
- Examples: `LivePricesUpdaterJob` (30s), `AutoSatAmountJob` (120s), `AccountTotalsJob` (5s)

**ViewModel:**
- Purpose: UI state and command handling
- Base: `ValtViewModel` extends `ObservableObject` (`src/Valt.UI/Base/ValtViewModel.cs`)
- Variants: `ValtModalViewModel`, `ValtTabViewModel`, `ValtValidatorViewModel`
- Pattern: Properties with `[ObservableProperty]`, commands with `[RelayCommand]`

## Entry Points

**Application Entry:**
- Location: `src/Valt.UI/Program.cs`
- Triggers: User launches desktop application
- Responsibilities: Initialize Avalonia framework, crash reporting

**Framework Initialization:**
- Location: `src/Valt.UI/App.axaml.cs`
- Triggers: After Avalonia framework ready
- Responsibilities: DI composition (`AddValtCore()`, `AddValtInfrastructure()`, `AddValtUI()`), start background jobs, show MainView

**DI Registration:**
- Core: `src/Valt.Core/Extensions.cs`
- Infrastructure: `src/Valt.Infra/Extensions.cs` (uses Scrutor assembly scanning)
- UI: `src/Valt.UI/Extensions.cs`

## Error Handling

**Strategy:** Exception bubbling with logging, graceful degradation for external services

**Patterns:**
- Domain validation throws exceptions with descriptive messages
- Price crawlers catch and log exceptions, return empty data (graceful degradation)
- UI displays errors via toast notifications
- Background jobs log errors and continue execution

## Cross-Cutting Concerns

**Logging:**
- Microsoft.Extensions.Logging with Console provider
- Configured at Information level in `App.axaml.cs`
- Each background job has dedicated logger

**Domain Events:**
- Published via `IDomainEventPublisher` (`src/Valt.Infra/Kernel/EventSystem/`)
- Handlers implement `IDomainEventHandler<T>`
- Auto-registered via Scrutor assembly scanning

**Validation:**
- Fluent validation in ViewModels via `ValtValidatorViewModel`
- Domain validation in entity constructors and factory methods

**Notifications:**
- UI updates via `INotificationPublisher` (`src/Valt.Infra/Kernel/Notifications/`)
- WeakReferenceMessenger for loose coupling

---

*Architecture analysis: 2026-01-13*
*Update when major patterns change*
