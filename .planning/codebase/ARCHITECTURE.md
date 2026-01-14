# Architecture

**Analysis Date:** 2026-01-14

## Pattern Overview

**Overall:** Clean Architecture with Domain-Driven Design (DDD)

**Key Characteristics:**
- Three-layer architecture with strict dependency flow inward
- Rich domain models with aggregate roots and entities
- Event-driven architecture with domain events
- MVVM pattern for UI with compiled bindings
- Local-first with embedded database (no cloud)

## Layers

**Domain Layer (Valt.Core):**
- Purpose: Pure business logic with no external dependencies
- Contains: Entities, value objects, aggregates, domain events, repository interfaces
- Depends on: Nothing (except DI abstractions)
- Used by: Infrastructure and UI layers
- Location: `src/Valt.Core/`

**Infrastructure Layer (Valt.Infra):**
- Purpose: Persistence, external services, background jobs, event handlers
- Contains: Repository implementations, query handlers, crawlers, settings
- Depends on: Domain layer (Valt.Core)
- Used by: UI layer
- Location: `src/Valt.Infra/`

**Presentation Layer (Valt.UI):**
- Purpose: User interface with Avalonia desktop UI
- Contains: ViewModels, Views (AXAML), state objects, converters
- Depends on: Infrastructure and Domain layers
- Used by: End user
- Location: `src/Valt.UI/`

## Data Flow

**Write Operation (Command-style):**

1. User action triggers ViewModel command
2. ViewModel calls repository method (e.g., `IAccountRepository.SaveAccountAsync()`)
3. Repository retrieves aggregate from LiteDB
4. Domain method called on aggregate (e.g., `account.Rename()`)
5. Aggregate raises event via `AddEvent()`
6. Repository persists entity and publishes domain events
7. Event handlers execute (notifications, state updates)
8. Aggregate events cleared

**Read Operation (Query-style):**

1. ViewModel injects query service (e.g., `IAccountQueries`)
2. Query handler reads from LiteDB collections directly
3. Maps entities to DTOs
4. Returns immutable result to ViewModel
5. ViewModel updates observable properties for UI binding

**State Management:**
- File-based: All data in LiteDB files (`valt.db`, `prices.db`)
- In-memory state: `RatesState`, `AccountsTotalState`, `FilterState`, `LiveRateState`
- State updates via `WeakReferenceMessenger` for loose coupling

## Key Abstractions

**Entity:**
- Purpose: Base class for domain objects with ID-based equality
- Location: `src/Valt.Core/Kernel/Entity.cs`
- Pattern: `Entity<T>` with `Id` property and equality semantics

**AggregateRoot:**
- Purpose: Domain aggregate with event collection and versioning
- Location: `src/Valt.Core/Kernel/AggregateRoot.cs`
- Pattern: Inherits `Entity<T>`, manages domain events via `AddEvent()`

**Repository:**
- Purpose: Persistence abstraction for aggregates
- Examples: `IAccountRepository`, `ITransactionRepository`, `ICategoryRepository`
- Pattern: Interface in Core, implementation in Infra with event publishing

**Query Handler:**
- Purpose: Read-only services returning DTOs
- Examples: `IAccountQueries`, `ITransactionQueries`, `ICategoryQueries`
- Location: `src/Valt.Infra/Modules/{Module}/Queries/`

**Value Objects:**
- Purpose: Immutable domain concepts
- Examples: `BtcValue` (satoshis), `FiatValue`, `FiatCurrency`, `Icon`
- Location: `src/Valt.Core/Common/`

**Background Job:**
- Purpose: Periodic tasks for price updates, calculations
- Examples: `LivePricesUpdaterJob`, `BitcoinHistoryUpdaterJob`, `AutoSatAmountJob`
- Location: `src/Valt.Infra/Crawlers/`, `src/Valt.Infra/Modules/`

**ViewModel:**
- Purpose: UI state and command handling
- Base classes: `ValtViewModel`, `ValtModalViewModel`, `ValtTabViewModel`
- Location: `src/Valt.UI/Base/`

## Entry Points

**CLI Entry:**
- Location: `src/Valt.UI/Program.cs`
- Triggers: Application launch
- Responsibilities: STA thread setup, Avalonia app builder configuration

**Application Bootstrap:**
- Location: `src/Valt.UI/App.axaml.cs`
- Triggers: Framework initialization
- Responsibilities: DI container setup, service registration, background job initialization

**Main Window:**
- Location: `src/Valt.UI/Views/Main/MainViewModel.cs`
- Triggers: After database selection
- Responsibilities: Tab navigation, modal dialogs, menu commands

## Error Handling

**Strategy:** Throw domain exceptions, catch at boundaries (ViewModels, jobs)

**Patterns:**
- Domain-specific exceptions: `DomainException`, `EntityNotFoundException`
- Value object validation exceptions: `InvalidBtcValueException`, `InvalidFiatValueException`
- Logging via `ILogger<T>` with Microsoft.Extensions.Logging
- Silent fallbacks in price crawlers (graceful degradation)

## Cross-Cutting Concerns

**Logging:**
- Microsoft.Extensions.Logging with ILogger<T>
- Injected per-class via DI

**Validation:**
- Constructor validation in value objects
- Domain validation in aggregate methods
- MVVM validation in ViewModels with `ValtValidatorViewModel`

**Domain Events:**
- `IDomainEvent` marker interface
- `IDomainEventHandler<TEvent>` for scoped handlers
- `IDomainEventPublisher` publishes after repository save
- Handler registration via Scrutor assembly scanning

**Background Jobs:**
- `BackgroundJobManager` manages job lifecycle
- Job types: `App`, `ValtDatabase`, `PriceDatabase`
- Automatic start on application launch

---

*Architecture analysis: 2026-01-14*
*Update when major patterns change*
