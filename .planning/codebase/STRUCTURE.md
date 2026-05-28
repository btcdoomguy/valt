# Codebase Structure

**Analysis Date:** 2026-05-27

## Directory Layout

```
[project-root]/
├── src/
│   ├── Valt.Core/              # Domain layer - entities, value objects, events
│   │   ├── Kernel/             # Base classes (Entity, AggregateRoot, IDs)
│   │   ├── Common/             # Shared value objects (BtcValue, FiatValue, Icon)
│   │   └── Modules/            # Domain modules
│   │       ├── Assets/         # External investments domain
│   │       ├── AvgPrice/       # Cost basis tracking domain
│   │       ├── Budget/         # Accounts, categories, transactions, fixed expenses
│   │       └── Goals/          # Financial goals domain
│   ├── Valt.App/               # Application layer - CQRS commands, queries, DTOs
│   │   ├── Kernel/             # Dispatchers, Result, Validation
│   │   │   ├── Commands/       # ICommand, ICommandHandler, CommandDispatcher
│   │   │   ├── Queries/        # IQuery, IQueryHandler, QueryDispatcher
│   │   │   └── Validation/     # IValidator, ValidationResult
│   │   └── Modules/            # Application modules mirror Core modules
│   │       ├── Assets/
│   │       │   ├── Commands/   # Per-command folders (e.g., CreateBasicAsset/)
│   │       │   ├── Queries/    # Per-query folders (e.g., GetAssets/)
│   │       │   ├── DTOs/       # Command/query DTO records
│   │       │   └── Contracts/  # App-level service contracts
│   │       ├── AvgPrice/
│   │       ├── Budget/
│   │       │   ├── Accounts/   # Nested: Commands/, Queries/, DTOs/, Contracts/
│   │       │   ├── Categories/
│   │       │   ├── FixedExpenses/
│   │       │   └── Transactions/
│   │       └── Goals/
│   ├── Valt.Infra/             # Infrastructure layer - persistence, external services
│   │   ├── DataAccess/         # LiteDB databases, migrations, mappers
│   │   ├── Kernel/             # Background jobs, event publisher, notifications
│   │   ├── Modules/            # Repositories, query implementations, services
│   │   │   ├── Assets/
│   │   │   ├── AvgPrice/
│   │   │   ├── Budget/
│   │   │   ├── Configuration/
│   │   │   ├── Currency/
│   │   │   ├── DataSources/    # Bitcoin/fiat historical data
│   │   │   ├── Goals/
│   │   │   ├── MachineLearning/# Expense forecasting, alerts
│   │   │   └── Reports/        # Financial reports
│   │   ├── Crawlers/           # Price providers
│   │   │   ├── HistoricPriceCrawlers/
│   │   │   │   ├── Bitcoin/    # Kraken provider
│   │   │   │   └── Fiat/       # Frankfurter, CurrencyAPI providers
│   │   │   ├── LivePriceCrawlers/
│   │   │   │   ├── Bitcoin/    # CoinGecko provider
│   │   │   │   └── Fiat/       # Frankfurter, CurrencyAPI providers
│   │   │   └── Indicators/     # Fear & Greed, dominance
│   │   ├── Mcp/                # MCP server and AI tools
│   │   │   ├── Server/         # McpServerService, McpServerState
│   │   │   └── Tools/          # MCP tool classes per domain
│   │   ├── Services/           # CSV import/export, updates
│   │   └── Settings/           # Persistable settings
│   └── Valt.UI/                # Presentation layer - Avalonia desktop app
│       ├── Assets/             # Icons, fonts, images
│       ├── Base/               # ViewModel base classes
│       ├── Converters/         # XAML value converters
│       ├── Handlers/           # UI notification handlers
│       ├── Helpers/            # UI helper classes
│       ├── Lang/               # RESX localization files
│       ├── Services/           # Modal/page factories, theming
│       ├── State/              # Application state objects
│       ├── Styles/             # Avalonia theme styles
│       ├── UserControls/       # Reusable XAML controls
│       └── Views/
│           └── Main/
│               ├── Controls/   # Main window controls (LiveRates, etc.)
│               ├── Modals/     # Modal dialog views and ViewModels
│               └── Tabs/       # Tab page views and ViewModels
├── tests/
│   └── Valt.Tests/             # NUnit test project
│       ├── Application/        # App layer tests (commands, queries)
│       ├── Architecture/       # NetArchTest.Rules architecture tests
│       ├── Builders/           # Test data builders
│       ├── Domain/             # Domain layer unit tests
│       ├── Infra/              # Infrastructure tests
│       ├── Infrastructure/     # Background job, crawler tests
│       ├── Reports/            # Report calculation tests
│       ├── Services/           # Goal calculator tests
│       └── UI/                 # UI converter, ViewModel tests
├── Directory.Packages.props    # Centralized NuGet package versions
└── Valt.sln                    # Solution file (5 projects)
```

## Directory Purposes

**`src/Valt.Core/Kernel/`:**
- Purpose: Base abstractions for the domain layer
- Contains: `Entity<T>`, `AggregateRoot<T>`, `EntityId`, `IdGenerator`, `IClock`
- Key files: `src/Valt.Core/Kernel/Entity.cs`, `src/Valt.Core/Kernel/AggregateRoot.cs`

**`src/Valt.Core/Modules/{Module}/`:**
- Purpose: Domain entities and events per module
- Contains: Entity classes, domain events, repository contracts, exceptions
- Key files: `src/Valt.Core/Modules/Budget/Accounts/Account.cs`, `src/Valt.Core/Modules/Budget/Accounts/Contracts/IAccountRepository.cs`

**`src/Valt.App/Modules/{Module}/Commands/`:**
- Purpose: CQRS command definitions, handlers, and validators
- Contains: One folder per command with `{Command}.cs`, `{Command}Handler.cs`, `{Command}Validator.cs`
- Key files: `src/Valt.App/Modules/Budget/Accounts/Commands/CreateFiatAccount/CreateFiatAccountCommand.cs`

**`src/Valt.App/Modules/{Module}/Queries/`:**
- Purpose: CQRS query definitions and handlers
- Contains: One folder per query with `{Query}.cs`, `{Query}Handler.cs`
- Key files: `src/Valt.App/Modules/Budget/Accounts/Queries/GetAccounts/GetAccountsQuery.cs`

**`src/Valt.App/Modules/{Module}/DTOs/`:**
- Purpose: Data transfer objects for commands and queries
- Contains: Record classes with `required` init properties
- Key files: `src/Valt.App/Modules/Budget/Accounts/DTOs/AccountDTO.cs`

**`src/Valt.Infra/DataAccess/`:**
- Purpose: LiteDB database access and migrations
- Contains: `LocalDatabase`, `PriceDatabase`, `BsonMapper` configuration, migration scripts
- Key files: `src/Valt.Infra/DataAccess/LocalDatabase.cs`, `src/Valt.Infra/DataAccess/Migrations/MigrationManager.cs`

**`src/Valt.Infra/Modules/{Module}/`:**
- Purpose: Repository implementations, query implementations, domain services
- Contains: `*Repository.cs`, `*Queries.cs`, `*Services.cs`, event handlers
- Key files: `src/Valt.Infra/Modules/Budget/Accounts/AccountRepository.cs`

**`src/Valt.UI/Views/Main/Tabs/`:**
- Purpose: Main application tab pages
- Contains: One folder per tab with `*View.axaml`, `*ViewModel.cs`, and `Models/` subfolder
- Key files: `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs`

**`src/Valt.UI/Views/Main/Modals/`:**
- Purpose: Modal dialogs
- Contains: One folder per modal with `*View.axaml`, `*ViewModel.cs`
- Key files: `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs`

**`tests/Valt.Tests/Builders/`:**
- Purpose: Fluent test data builders
- Contains: Builder classes for all domain entities
- Key files: `tests/Valt.Tests/Builders/TransactionBuilder.cs`, `tests/Valt.Tests/Builders/FiatAccountBuilder.cs`

**`tests/Valt.Tests/Architecture/`:**
- Purpose: Architecture constraint tests using NetArchTest.Rules
- Contains: Layer dependency tests, naming convention tests
- Key files: `tests/Valt.Tests/Architecture/LayerDependencyTests.cs`

## Key File Locations

**Entry Points:**
- `src/Valt.UI/Program.cs`: Application entry point
- `src/Valt.UI/App.axaml.cs`: DI bootstrap and Avalonia initialization

**Configuration:**
- `Directory.Packages.props`: Central package management (all NuGet versions)
- `src/Valt.UI/Valt.UI.csproj`: Main executable project with Avalonia resources
- `src/Valt.UI/app.manifest`: Windows application manifest

**Core Logic:**
- `src/Valt.App/Kernel/Result.cs`: Railway-oriented result type
- `src/Valt.App/Kernel/Commands/CommandDispatcher.cs`: Command routing
- `src/Valt.App/Kernel/Queries/QueryDispatcher.cs`: Query routing
- `src/Valt.Core/Kernel/Entity.cs`: Base entity class
- `src/Valt.Core/Kernel/AggregateRoot.cs`: Base aggregate root with events

**Testing:**
- `tests/Valt.Tests/IntegrationTest.cs`: Base class for integration tests with full DI
- `tests/Valt.Tests/DatabaseTest.cs`: Base class for in-memory LiteDB tests
- `tests/Valt.Tests/Architecture/LayerDependencyTests.cs`: Architecture constraint enforcement

## Naming Conventions

**Files:**
- Commands: `{Action}{Entity}Command.cs` (e.g., `CreateFiatAccountCommand.cs`)
- Command Handlers: `{Action}{Entity}Handler.cs` (e.g., `CreateFiatAccountHandler.cs`)
- Command Validators: `{Action}{Entity}Validator.cs` (e.g., `CreateFiatAccountValidator.cs`)
- Queries: `{Action}{Entity}Query.cs` (e.g., `GetAccountsQuery.cs`)
- Query Handlers: `{Action}{Entity}Handler.cs` (e.g., `GetAccountsHandler.cs`)
- DTOs: `{Entity}DTO.cs` (e.g., `AccountDTO.cs`)
- Repositories: `{Entity}Repository.cs` (e.g., `AccountRepository.cs`)
- Repository Contracts: `I{Entity}Repository.cs` (e.g., `IAccountRepository.cs`)
- ViewModels: `{Name}ViewModel.cs` (e.g., `TransactionsViewModel.cs`)
- Views: `{Name}View.axaml` + `{Name}View.axaml.cs` (e.g., `TransactionsView.axaml`)
- Domain Events: `{Entity}{Action}Event.cs` (e.g., `AccountCreatedEvent.cs`)
- Event Handlers: `{EventName}Handler.cs` (e.g., `AccountCreatedEventHandler.cs`)
- Entity DTOs (Infra): `{Entity}Entity.cs` (e.g., `AccountEntity.cs`)

**Directories:**
- Command folders: `src/Valt.App/Modules/{Module}/Commands/{CommandName}/`
- Query folders: `src/Valt.App/Modules/{Module}/Queries/{QueryName}/`
- Module folders mirror across Core/App/Infra: `Modules/{Module}/`

**Types:**
- Domain entities: PascalCase, extend `AggregateRoot<T>` or `Entity<T>`
- Value objects: PascalCase, typically records or immutable classes
- Commands/Queries: `public record {Name} : ICommand<TResult>` or `IQuery<TResult>`
- DTOs: `public sealed record {Name}DTO` with `required` init properties
- ViewModels: `public partial class {Name}ViewModel : ValtViewModel` (partial for source generators)

## Where to Add New Code

**New Domain Entity:**
- Entity class: `src/Valt.Core/Modules/{Module}/{Entity}.cs`
- Domain events: `src/Valt.Core/Modules/{Module}/Events/{Entity}{Action}Event.cs`
- Repository contract: `src/Valt.Core/Modules/{Module}/Contracts/I{Entity}Repository.cs`
- Tests: `tests/Valt.Tests/Domain/{Module}/{Entity}Tests.cs`

**New Command (Write Operation):**
- Command record: `src/Valt.App/Modules/{Module}/Commands/{CommandName}/{CommandName}Command.cs`
- Handler: `src/Valt.App/Modules/{Module}/Commands/{CommandName}/{CommandName}Handler.cs`
- Validator: `src/Valt.App/Modules/{Module}/Commands/{CommandName}/{CommandName}Validator.cs`
- DTO (if needed): `src/Valt.App/Modules/{Module}/DTOs/{Name}DTO.cs`
- Tests: `tests/Valt.Tests/Application/{Module}/Commands/{CommandName}Tests.cs`

**New Query (Read Operation):**
- Query record: `src/Valt.App/Modules/{Module}/Queries/{QueryName}/{QueryName}Query.cs`
- Handler: `src/Valt.App/Modules/{Module}/Queries/{QueryName}/{QueryName}Handler.cs`
- Query implementation (if Infra needed): `src/Valt.Infra/Modules/{Module}/Queries/{Name}.cs`
- Tests: `tests/Valt.Tests/Application/{Module}/Queries/{QueryName}Tests.cs`

**New Repository:**
- Implementation: `src/Valt.Infra/Modules/{Module}/{Entity}Repository.cs`
- Entity mapping: `src/Valt.Infra/Modules/{Module}/{Entity}Entity.cs` (with `AsDomainObject()` / `AsEntity()` methods)
- Registration: `src/Valt.Infra/Extensions.cs` in `AddRepositories()`
- Tests: `tests/Valt.Tests/Domain/{Module}/{Entity}RepositoryTests.cs`

**New UI Tab:**
- ViewModel: `src/Valt.UI/Views/Main/Tabs/{Name}/{Name}ViewModel.cs` (inherit `ValtTabViewModel`)
- View: `src/Valt.UI/Views/Main/Tabs/{Name}/{Name}View.axaml`
- Register in `src/Valt.UI/Extensions.cs` in `AddValtUI()`
- Add to `MainViewTabNames` enum: `src/Valt.UI/Views/MainViewTabNames.cs`
- Add tab button in `MainView.axaml`

**New Modal Dialog:**
- ViewModel: `src/Valt.UI/Views/Main/Modals/{Name}/{Name}ViewModel.cs` (inherit `ValtModalViewModel`)
- View: `src/Valt.UI/Views/Main/Modals/{Name}/{Name}View.axaml`
- Register in `src/Valt.UI/Extensions.cs` in `AddValtUI()`
- Add to `ApplicationModalNames` enum: `src/Valt.UI/Views/ApplicationModalNames.cs`

**New Background Job:**
- Job class: `src/Valt.Infra/Kernel/BackgroundJobs/{Name}Job.cs` (implement `IBackgroundJob`)
- Auto-registered via Scrutor scan in `AddValtInfrastructure()`
- Add system name to `BackgroundJobSystemNames` enum
- Tests: `tests/Valt.Tests/Infrastructure/BackgroundJobs/{Name}JobTests.cs`

**New MCP Tool:**
- Tool class: `src/Valt.Infra/Mcp/Tools/{Domain}/{Name}Tools.cs`
- Decorate class with `[McpServerToolType]`, methods with `[McpServerTool]` and `[Description]`
- Forward required services in `McpServerService.ForwardServicesFromMainApp()`

**New Localization String:**
- Add to ALL three RESX files:
  1. `src/Valt.UI/Lang/language.resx` (English)
  2. `src/Valt.UI/Lang/language.pt-BR.resx` (Portuguese)
  3. `src/Valt.UI/Lang/language.es.resx` (Spanish)
- Add static property to `language.Designer.cs`

## Special Directories

**`src/Valt.UI/Assets/Fonts/`:**
- Purpose: Custom fonts (Geist, GeistMono, Phosphor, MaterialDesign)
- Generated: No
- Committed: Yes
- Note: `MaterialSymbolsOutlined-map.json` contains icon name-to-unicode mappings

**`src/Valt.Infra/DataAccess/Migrations/Scripts/`:**
- Purpose: Database migration scripts implementing `IMigrationScript`
- Generated: No
- Committed: Yes
- Naming: `Migration_{NNN}_{Description}.cs`

**`tests/Valt.Tests/Builders/`:**
- Purpose: Fluent test data builders for all domain types
- Generated: No
- Committed: Yes
- Pattern: Static factory methods like `.AnAccount()`, `.ATransaction()`

---

*Structure analysis: 2026-05-27*
