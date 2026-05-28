<!-- refreshed: 2026-05-27 -->
# Architecture

**Analysis Date:** 2026-05-27

## System Overview

Valt is a personal budget management desktop application built on a strictly layered architecture with CQRS, Domain-Driven Design patterns, and MVVM UI. The codebase is organized into four projects following dependency rules enforced by NetArchTest.Rules architecture tests.

```text
┌─────────────────────────────────────────────────────────────┐
│                      UI Layer (Valt.UI)                      │
│  Avalonia 12 + CommunityToolkit.Mvvm + LiveCharts           │
│  `src/Valt.UI/`                                              │
├──────────────────┬──────────────────┬───────────────────────┤
│   Views/Tabs     │   Views/Modals   │   State & Services    │
│  `Views/Main/    │  `Views/Main/    │  `State/`, `Services/`│
│   Tabs/`         │   Modals/`       │                       │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure (Valt.Infra)                │
│  LiteDB persistence, price crawlers, MCP server, reports    │
│  `src/Valt.Infra/`                                           │
├──────────────────┬──────────────────┬───────────────────────┤
│  Repositories    │  Price Crawlers  │  Background Jobs      │
│  `Modules/*/      │  `Crawlers/`     │  `Kernel/Background`  │
│   Repositories`  │                  │                       │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application (Valt.App)                     │
│  CQRS commands, queries, validators, DTOs                   │
│  `src/Valt.App/`                                             │
├──────────────────┬──────────────────┬───────────────────────┤
│   Commands       │   Queries        │   Validation          │
│  `Modules/*/      │  `Modules/*/      │  `Kernel/Validation/` │
│   Commands/`     │   Queries/`      │                       │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                     Domain (Valt.Core)                       │
│  Entities, value objects, domain events, contracts          │
│  `src/Valt.Core/`                                            │
├──────────────────┬──────────────────┬───────────────────────┤
│   AggregateRoots │   Value Objects  │   Domain Events       │
│  `Kernel/`        │  `Common/`       │  `Modules/*/Events/`  │
└─────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| `App.axaml.cs` | Application bootstrap, DI container setup, background job lifecycle | `src/Valt.UI/App.axaml.cs` |
| `MainViewModel` | Main window orchestration, tab switching, database open/close | `src/Valt.UI/Views/Main/MainViewModel.cs` |
| `CommandDispatcher` | Resolves and invokes command handlers via reflection cache | `src/Valt.App/Kernel/Commands/CommandDispatcher.cs` |
| `QueryDispatcher` | Resolves and invokes query handlers via reflection cache | `src/Valt.App/Kernel/Queries/QueryDispatcher.cs` |
| `Result<T>` | Railway-oriented error handling for command results | `src/Valt.App/Kernel/Result.cs` |
| `AggregateRoot<T>` | Base for all aggregates with domain event collection | `src/Valt.Core/Kernel/AggregateRoot.cs` |
| `Entity<T>` | Base entity with identity-based equality | `src/Valt.Core/Kernel/Entity.cs` |
| `DomainEventPublisher` | Publishes domain events to scoped handlers | `src/Valt.Infra/Kernel/EventSystem/DomainEventPublisher.cs` |
| `NotificationPublisher` | Publishes infra notifications to scoped handlers | `src/Valt.Infra/Kernel/Notifications/NotificationPublisher.cs` |
| `LocalDatabase` | LiteDB access with password protection and thread-safe locking | `src/Valt.Infra/DataAccess/LocalDatabase.cs` |
| `BackgroundJobManager` | Manages periodic background jobs with channels and retry logic | `src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs` |
| `McpServerService` | Embedded Kestrel MCP server for AI assistant integration | `src/Valt.Infra/Mcp/Server/McpServerService.cs` |

## Pattern Overview

**Overall:** Layered Architecture + CQRS + Domain-Driven Design + MVVM

**Key Characteristics:**
- **Dependency direction is strictly inward**: UI -> Infra -> App -> Core (enforced by architecture tests)
- **CQRS separates reads from writes**: Commands mutate state and return `Result<T>`; queries return read-only DTOs
- **Domain events decouple side effects**: Aggregate roots emit events; Infra handlers react to them
- **Auto-registration via Scrutor**: Handlers, validators, jobs, and event handlers are discovered by assembly scanning
- **Repository abstraction**: Domain defines contracts (`IAccountRepository`); Infra implements them (`AccountRepository`)
- **MVVM with source generators**: ViewModels use `CommunityToolkit.Mvvm` source generators for `[ObservableProperty]` and `[RelayCommand]`

## Layers

### Domain Layer (Valt.Core)
- **Purpose:** Entities, value objects, domain events, and repository contracts. No external dependencies.
- **Location:** `src/Valt.Core/`
- **Contains:**
  - `Kernel/Entity<T>`, `AggregateRoot<T>`, `EntityId`, `IdGenerator`
  - `Common/` value objects: `BtcValue`, `FiatValue`, `FiatCurrency`, `Icon`
  - Module-specific entities under `Modules/{Module}/`
  - Domain events under `Modules/{Module}/Events/`
  - Repository contracts under `Modules/{Module}/Contracts/`
- **Depends on:** `Microsoft.Extensions.DependencyInjection.Abstractions` only
- **Used by:** App, Infra, UI (via DTOs)

### Application Layer (Valt.App)
- **Purpose:** CQRS commands, queries, validators, and DTOs. Orchestrates domain logic.
- **Location:** `src/Valt.App/`
- **Contains:**
  - `Kernel/Commands/`: `ICommand<T>`, `ICommandHandler<T, TResult>`, `CommandDispatcher`
  - `Kernel/Queries/`: `IQuery<T>`, `IQueryHandler<T, TResult>`, `QueryDispatcher`
  - `Kernel/Validation/`: `IValidator<T>`, `ValidationResult`
  - `Kernel/Result.cs`: Railway-oriented `Result<T>` with `Bind`, `Map`, `Match`
  - Module folders: `Modules/{Module}/Commands/`, `Queries/`, `DTOs/`, `Contracts/`
- **Depends on:** Valt.Core only
- **Used by:** Infra, UI

### Infrastructure Layer (Valt.Infra)
- **Purpose:** Persistence, external services, price crawlers, background jobs, MCP server
- **Location:** `src/Valt.Infra/`
- **Contains:**
  - `DataAccess/`: `LocalDatabase`, `PriceDatabase`, LiteDB mappers, migrations
  - `Modules/{Module}/`: Repository implementations, query implementations, services
  - `Crawlers/`: Live and historic price providers (Bitcoin, Fiat)
  - `Kernel/BackgroundJobs/`: `BackgroundJobManager`, job definitions
  - `Kernel/EventSystem/`: `DomainEventPublisher`
  - `Kernel/Notifications/`: `NotificationPublisher`, notification handlers
  - `Mcp/`: MCP server and tools for AI assistant integration
  - `Services/`: CSV import/export, update checker
- **Depends on:** Valt.Core, Valt.App, LiteDB, AspNetCore, YahooFinanceApi, CsvHelper
- **Used by:** UI

### Presentation Layer (Valt.UI)
- **Purpose:** Avalonia desktop UI with MVVM
- **Location:** `src/Valt.UI/`
- **Contains:**
  - `Views/Main/Tabs/`: Tab pages (Transactions, Reports, AvgPrice, Assets)
  - `Views/Main/Modals/`: Modal dialogs
  - `Base/`: `ValtViewModel`, `ValtTabViewModel`, `ValtModalViewModel`
  - `State/`: `RatesState`, `AccountsTotalState`, `FilterState`, `SecureModeState`
  - `Services/`: `IModalFactory`, `IPageFactory`, theming, font scaling
  - `Handlers/`: UI notification handlers bridging Infra notifications to `WeakReferenceMessenger`
- **Depends on:** Valt.App, Valt.Infra, Avalonia, CommunityToolkit.Mvvm, LiveCharts
- **Used by:** Entry point only

## Data Flow

### Primary Command Path (Write Operation)

1. **UI triggers command** — ViewModel calls `_commandDispatcher.DispatchAsync(new MyCommand { ... })` (`src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs:564`)
2. **Dispatcher resolves handler** — `CommandDispatcher` looks up `ICommandHandler<MyCommand, TResult>` from DI cache (`src/Valt.App/Kernel/Commands/CommandDispatcher.cs:20`)
3. **Handler executes business logic** — Loads aggregates via repositories, invokes domain methods (`src/Valt.App/Modules/Budget/Accounts/Commands/CreateFiatAccount/`)
4. **Domain events emitted** — `AggregateRoot.AddEvent(new AccountUpdatedEvent(this))` (`src/Valt.Core/Modules/Budget/Accounts/Account.cs:40`)
5. **Repository persists** — `AccountRepository.SaveAccountAsync(account)` stores entity and publishes events (`src/Valt.Infra/Modules/Budget/Accounts/AccountRepository.cs:29`)
6. **Domain events published** — `DomainEventPublisher` resolves scoped handlers and invokes them (`src/Valt.Infra/Kernel/EventSystem/DomainEventPublisher.cs:23`)
7. **UI refreshed** — Notification handlers or `WeakReferenceMessenger` update UI state (`src/Valt.UI/Handlers/LivePriceUpdateUIHandler.cs`)

### Primary Query Path (Read Operation)

1. **UI requests data** — ViewModel calls `_queryDispatcher.DispatchAsync(new GetAccountsQuery(...))` (`src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs:446`)
2. **Dispatcher resolves handler** — `QueryDispatcher` looks up `IQueryHandler<GetAccountsQuery, TResult>` (`src/Valt.App/Kernel/Queries/QueryDispatcher.cs:20`)
3. **Handler reads from queries** — Query handlers in Infra read from LiteDB and project DTOs (`src/Valt.Infra/Modules/Budget/Accounts/Queries/AccountQueries.cs`)
4. **DTOs returned** — Flat DTO records returned to UI (`src/Valt.App/Modules/Budget/Accounts/DTOs/AccountDTO.cs`)

### Background Job Flow

1. **Jobs registered** via Scrutor scan in `AddValtInfrastructure()` (`src/Valt.Infra/Extensions.cs:163`)
2. **Started by type** — `BackgroundJobManager.StartAllJobsAsync(BackgroundJobTypes.App)` (`src/Valt.UI/App.axaml.cs:106`)
3. **Periodic execution** — Each job runs on a timer feeding a bounded channel (`src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs:211`)
4. **Retry logic** — Up to 3 retries with 100ms delay on failure (`src/Valt.Infra/Kernel/BackgroundJobs/JobInfo.cs:279`)

## Key Abstractions

### Result<T>
- **Purpose:** Railway-oriented error handling for commands
- **Location:** `src/Valt.App/Kernel/Result.cs`
- **Pattern:** Discriminated union with `Success`/`Failure` branches; supports `Bind`, `Map`, `Match`
- **Usage:** All command handlers return `Task<Result<TResult>>`

### ICommand<T> / IQuery<T>
- **Purpose:** Marker interfaces for CQRS messages
- **Location:** `src/Valt.App/Kernel/Commands/ICommand.cs`, `src/Valt.App/Kernel/Queries/IQuery.cs`
- **Pattern:** Records implement these; dispatchers resolve handlers by generic type

### AggregateRoot<T>
- **Purpose:** Base for all domain aggregates with event sourcing capabilities
- **Location:** `src/Valt.Core/Kernel/AggregateRoot.cs`
- **Pattern:** `AddEvent()` collects events; repositories call `ClearEvents()` after publishing
- **Examples:** `Account`, `Transaction`, `Goal`, `Asset`

### IRepository (marker)
- **Purpose:** Marker interface for repository implementations
- **Location:** `src/Valt.Core/Kernel/Abstractions/IRepository.cs`
- **Pattern:** Domain-specific contracts (e.g., `IAccountRepository`) extend this marker

### ValtViewModel
- **Purpose:** Base for all UI ViewModels
- **Location:** `src/Valt.UI/Base/ValtViewModel.cs`
- **Pattern:** Extends `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`; provides `GetUserControlOwnerWindow` delegate

## Entry Points

### Application Entry Point
- **Location:** `src/Valt.UI/Program.cs`
- **Triggers:** OS launches the executable
- **Responsibilities:** Initialize crash reporting, build Avalonia app, start desktop lifetime

### DI Bootstrap
- **Location:** `src/Valt.UI/App.axaml.cs`
- **Triggers:** Avalonia framework initialization
- **Responsibilities:**
  - Build `ServiceCollection` and register all layers via `.AddValtCore()`, `.AddValtApp()`, `.AddValtInfrastructure()`, `.AddValtUI()`
  - Set context scope for service provider resolution
  - Initialize settings, theme, font scale
  - Create `MainWindow` with `MainViewModel`
  - Start background jobs (if not in design mode)

### Database Open Flow
- **Location:** `src/Valt.UI/Views/Main/MainViewModel.cs:528` (`OpenInitialSelectionModal`)
- **Triggers:** User selects/creates a database file from initial modal
- **Responsibilities:**
  - Open local database (password-protected LiteDB)
  - Run migrations via `MigrationManager`
  - Initialize price database
  - Start background jobs
  - Set initial tab and refresh

### MCP Server Entry Point
- **Location:** `src/Valt.Infra/Mcp/Server/McpServerService.cs`
- **Triggers:** User toggles MCP server from settings or UI
- **Responsibilities:** Start embedded Kestrel on configurable port (default 5200), forward DI services to MCP tools

## Architectural Constraints

- **Threading:** Single UI thread (Avalonia). Background jobs run on thread pool. Database access uses `SemaphoreSlim` locks for MCP thread safety (`src/Valt.Infra/DataAccess/LocalDatabase.cs:329`).
- **Global state:** `App.ServiceProvider` static property holds the root DI container (`src/Valt.UI/App.axaml.cs:28`). `IdGenerator` uses a static `IIdProvider` configured at startup.
- **Scoped services:** Domain event and notification handlers run in a DI scope (`CreateScope()`) to support MCP multi-threading.
- **Circular imports:** None detected. Layer dependency tests enforce acyclic references.
- **Design mode:** ViewModels have parameterless constructors for Avalonia designer; `DesignTimeModalFactory` and `DesignTimePageFactory` provide design-time stubs.

## Anti-Patterns

### Direct Repository Access from ViewModels

**What happens:** Some legacy ViewModels may directly reference Infra repositories or query classes instead of using dispatchers.
**Why it's wrong:** Breaks layer boundaries and makes testing harder. UI should only reference App layer abstractions.
**Do this instead:** Always inject `ICommandDispatcher` and `IQueryDispatcher` into ViewModels. Dispatch commands and queries as shown in `TransactionsViewModel` (`src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs`).

### Domain Classes with Serialization Attributes

**What happens:** Adding `[JsonPropertyName]` or `[BsonField]` to domain entities.
**Why it's wrong:** Domain layer must be persistence-ignorant. Architecture tests enforce no `System.Text.Json.Serialization` or `LiteDB` references in Core.
**Do this instead:** Create separate entity/DTO classes in Infra layer (e.g., `AccountEntity` in `src/Valt.Infra/Modules/Budget/Accounts/AccountEntity.cs`) and use mapping methods like `AsDomainObject()` and `AsEntity()`.

### Infra Layer Using WeakReferenceMessenger

**What happens:** Infrastructure code referencing `CommunityToolkit.Mvvm.Messaging`.
**Why it's wrong:** Infra should not depend on UI-layer messaging. Architecture test `Infra_Should_Not_Reference_WeakReferenceMessenger` enforces this.
**Do this instead:** Use `INotificationPublisher` / `INotification` for Infra-to-UI communication. Bridge notifications to `WeakReferenceMessenger` in UI handlers (`src/Valt.UI/Handlers/`).

## Error Handling

**Strategy:** Railway-oriented programming with `Result<T>` for commands; exceptions for unrecoverable errors.

**Patterns:**
- Commands return `Result<T>` with `IsSuccess`/`IsFailure` flags. ViewModels check `result.IsFailure` and show error messages.
- Domain exceptions (`DomainException`, `EntityNotFoundException`) thrown for invariant violations.
- Background jobs catch exceptions, retry up to 3 times, and set `BackgroundJobState.Error`.

## Cross-Cutting Concerns

**Logging:** `Microsoft.Extensions.Logging` throughout. Background jobs use a custom `JobLoggerProvider` that captures logs into `JobLogPool` for UI display.

**Validation:** Command validators implement `IValidator<TCommand>`. Validators are auto-registered and invoked by handlers before domain logic.

**Authentication:** Password-based LiteDB encryption. Secure mode hides sensitive values in UI. No external auth provider.

**Localization:** RESX files (`Lang/language.resx`, `language.pt-BR.resx`, `language.es.resx`) with designer-generated C# properties.

---

*Architecture analysis: 2026-05-27*
