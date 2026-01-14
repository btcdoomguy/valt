# Codebase Structure

**Analysis Date:** 2026-01-14

## Directory Layout

```
valt/
├── src/                    # Source code
│   ├── Valt.Core/         # Domain layer (entities, value objects)
│   ├── Valt.Infra/        # Infrastructure layer (persistence, services)
│   └── Valt.UI/           # Presentation layer (Avalonia UI)
├── tests/                  # Test projects
│   └── Valt.Tests/        # All tests (unit, integration, architecture)
├── .planning/             # Project planning documents
└── Valt.sln               # Solution file
```

## Directory Purposes

**src/Valt.Core/**
- Purpose: Pure domain logic with no external dependencies
- Contains: Entities, value objects, aggregates, domain events, contracts
- Key subdirectories:
  - `Kernel/` - Base classes (`Entity.cs`, `AggregateRoot.cs`), event system, ID generation
  - `Modules/Budget/` - Accounts, Transactions, Categories, FixedExpenses
  - `Modules/AvgPrice/` - Cost basis tracking with calculation strategies
  - `Common/` - Value objects (`BtcValue`, `FiatValue`, `FiatCurrency`, `Icon`)

**src/Valt.Infra/**
- Purpose: Infrastructure implementations (persistence, external services)
- Contains: Repository implementations, query handlers, crawlers, background jobs
- Key subdirectories:
  - `DataAccess/` - `LocalDatabase.cs`, `PriceDatabase.cs`, migrations
  - `Modules/Budget/` - Repository implementations and query handlers
  - `Modules/Reports/` - Report generators (MonthlyTotals, ExpensesByCategory, etc.)
  - `Crawlers/` - Price providers (Coinbase, Kraken, Frankfurter)
  - `Kernel/BackgroundJobs/` - Job manager and job implementations
  - `Services/CsvImport/` - CSV import parser, executor, template generator
  - `Services/CsvExport/` - CSV export service
  - `Settings/` - Persistent settings (CurrencySettings, DisplaySettings)

**src/Valt.UI/**
- Purpose: Avalonia desktop application
- Contains: ViewModels, Views, state objects, converters, services
- Key subdirectories:
  - `Base/` - ViewModel base classes (`ValtViewModel`, `ValtModalViewModel`, `ValtTabViewModel`)
  - `Views/Main/Tabs/` - Three main tabs (Transactions, Reports, AvgPrice)
  - `Views/Main/Modals/` - 18 modal dialogs (TransactionEditor, ImportWizard, Settings, etc.)
  - `State/` - Application state objects (RatesState, FilterState, LiveRateState)
  - `Services/` - Factories (`IModalFactory`, `IPageFactory`), LocalStorage
  - `Converters/` - XAML value converters
  - `UserControls/` - Reusable controls (BtcInput, FiatInput, etc.)
  - `Lang/` - Localization resources (en-US, pt-BR)
  - `Assets/` - Fonts, images

**tests/Valt.Tests/**
- Purpose: Comprehensive test suite
- Contains: Unit tests, integration tests, architecture tests
- Key subdirectories:
  - `Builders/` - Test data factories (TransactionBuilder, AccountBuilder, etc.)
  - `Domain/` - Domain model tests
  - `UseCases/` - Query handler tests
  - `UI/` - ViewModel and converter tests
  - `Reports/` - Report generator tests
  - `CsvImport/` - CSV import feature tests
  - `Architecture/` - Layer dependency tests

## Key File Locations

**Entry Points:**
- `src/Valt.UI/Program.cs` - Application entry point
- `src/Valt.UI/App.axaml.cs` - Avalonia app configuration and DI setup

**Configuration:**
- `Valt.sln` - Solution file
- `Directory.Packages.props` - Centralized NuGet package versions
- `.editorconfig` - Editor formatting rules

**DI Registration:**
- `src/Valt.Core/Extensions.cs` - Core services
- `src/Valt.Infra/Extensions.cs` - Infrastructure services (line ~60-170)
- `src/Valt.UI/Extensions.cs` - UI services and factories

**Core Logic:**
- `src/Valt.Infra/DataAccess/LocalDatabase.cs` - Database connection management
- `src/Valt.Infra/DataAccess/PriceDatabase.cs` - Price data storage
- `src/Valt.UI/Views/Main/MainViewModel.cs` - Main application logic

**Testing:**
- `tests/Valt.Tests/DatabaseTest.cs` - Base class for unit tests with database
- `tests/Valt.Tests/IntegrationTest.cs` - Base class for integration tests
- `tests/Valt.Tests/Builders/` - Test data builders

## Naming Conventions

**Files:**
- PascalCase for C# files: `Transaction.cs`, `AccountRepository.cs`
- Entity suffix for LiteDB entities: `TransactionEntity.cs`, `AccountEntity.cs`
- ViewModel suffix for ViewModels: `TransactionEditorViewModel.cs`
- View suffix for AXAML: `TransactionEditorView.axaml`

**Directories:**
- PascalCase for all directories
- Plural for collections: `Modules/`, `Crawlers/`, `Builders/`

**Special Patterns:**
- `I{Name}.cs` for interfaces: `IAccountRepository.cs`, `ICsvImportParser.cs`
- `{Name}Tests.cs` for test files: `TransactionTests.cs`, `CategoryNameTests.cs`
- `{Name}Builder.cs` for test builders: `TransactionBuilder.cs`

## Where to Add New Code

**New Feature:**
- Domain code: `src/Valt.Core/Modules/{ModuleName}/`
- Infrastructure: `src/Valt.Infra/Modules/{ModuleName}/`
- UI: `src/Valt.UI/Views/Main/` (tab or modal)
- Tests: `tests/Valt.Tests/Domain/{ModuleName}/`

**New Repository:**
- Interface: `src/Valt.Core/Modules/{Module}/Contracts/I{Name}Repository.cs`
- Implementation: `src/Valt.Infra/Modules/{Module}/{Name}Repository.cs`
- Entity: `src/Valt.Infra/Modules/{Module}/{Name}Entity.cs`

**New Modal:**
- ViewModel: `src/Valt.UI/Views/Main/Modals/{ModalName}/{ModalName}ViewModel.cs`
- View: `src/Valt.UI/Views/Main/Modals/{ModalName}/{ModalName}View.axaml`
- Register in: `src/Valt.UI/Extensions.cs` (modal factory)

**New Background Job:**
- Implementation: `src/Valt.Infra/Kernel/BackgroundJobs/` or relevant `Modules/` subdirectory
- Interface: `IBackgroundJob` with `RunAsync()` method
- Auto-registered via Scrutor assembly scanning

**Utilities:**
- Domain utilities: `src/Valt.Core/Common/`
- Infrastructure utilities: `src/Valt.Infra/Kernel/Extensions/`
- UI utilities: `src/Valt.UI/Helpers/`

## Special Directories

**.planning/**
- Purpose: Project planning and codebase documentation
- Source: Generated by GSD workflow
- Committed: Yes

**src/Valt.UI/Lang/**
- Purpose: Localization resources
- Contains: `.resx` files for en-US (default) and pt-BR
- Committed: Yes

**src/Valt.UI/Assets/**
- Purpose: Static resources (fonts, images)
- Contains: Custom fonts (Geist, GeistMono, Phosphor, MaterialDesign icons)
- Committed: Yes

---

*Structure analysis: 2026-01-14*
*Update when directory structure changes*
