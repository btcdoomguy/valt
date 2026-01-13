# Codebase Structure

**Analysis Date:** 2026-01-13

## Directory Layout

```
valt/
├── src/
│   ├── Valt.Core/              # Domain layer (no dependencies)
│   ├── Valt.Infra/             # Infrastructure layer
│   └── Valt.UI/                # Presentation layer
├── tests/
│   └── Valt.Tests/             # Test project
├── Directory.Packages.props    # Central NuGet package management
├── Valt.sln                    # Solution file
└── CLAUDE.md                   # AI assistant instructions
```

## Directory Purposes

**src/Valt.Core/**
- Purpose: Domain layer with business logic and entities
- Contains: Aggregates, Value Objects, Domain Events, Repository interfaces
- Key files:
  - `Kernel/` - Base classes (`Entity.cs`, `AggregateRoot.cs`), ID generation, event system interfaces
  - `Modules/Budget/` - Accounts, Categories, Transactions, FixedExpenses
  - `Modules/AvgPrice/` - Cost basis tracking with calculation strategies
  - `Common/` - Shared value objects (`BtcValue.cs`, `FiatValue.cs`, `FiatCurrency.cs`, `Icon.cs`)

**src/Valt.Infra/**
- Purpose: Infrastructure implementations and external service integrations
- Contains: Repositories, queries, price crawlers, background jobs, settings
- Key files:
  - `DataAccess/` - LiteDB database access (`LocalDatabase.cs`, `PriceDatabase.cs`), migrations
  - `Modules/Budget/` - Repository implementations (`AccountRepository.cs`, `TransactionRepository.cs`)
  - `Modules/Reports/` - Report generators (`MonthlyTotalsReport.cs`, `ExpensesByCategoryReport.cs`)
  - `Crawlers/` - Price data providers (Coinbase, Kraken, Frankfurter, CurrencyApi)
  - `Kernel/BackgroundJobs/` - Job system and implementations
  - `Settings/` - Persistent settings (`CurrencySettings.cs`, `DisplaySettings.cs`)
  - `Extensions.cs` - DI registration with Scrutor assembly scanning

**src/Valt.UI/**
- Purpose: Avalonia desktop application
- Contains: Views, ViewModels, State objects, Controls, Converters
- Key files:
  - `Base/` - ViewModel base classes (`ValtViewModel.cs`, `ValtModalViewModel.cs`, `ValtTabViewModel.cs`)
  - `Views/Main/Tabs/` - Three main tabs (Transactions, Reports, AvgPrice)
  - `Views/Main/Modals/` - 17 modal dialogs
  - `State/` - Observable state objects (`RatesState.cs`, `AccountsTotalState.cs`)
  - `UserControls/` - Reusable controls (`BtcInput.axaml`, `FiatInput.axaml`)
  - `Services/` - Factories (`ModalFactory.cs`, `PageFactory.cs`)
  - `Converters/` - XAML value converters
  - `Lang/` - Localization resources (en-US, pt-BR)
  - `Extensions.cs` - UI-specific DI registration

**tests/Valt.Tests/**
- Purpose: Unit and integration tests
- Contains: Domain tests, repository tests, ViewModel tests, architecture tests
- Key files:
  - `Builders/` - Test data builders (`TransactionBuilder.cs`, `AvgPriceProfileBuilder.cs`)
  - `Domain/` - Domain entity tests
  - `UseCases/` - Query handler tests
  - `UI/` - ViewModel and converter tests
  - `Architecture/` - Layer dependency constraint tests
  - `DatabaseTest.cs` - Base class for tests with in-memory LiteDB
  - `IntegrationTest.cs` - Base class for full DI container tests

## Key File Locations

**Entry Points:**
- `src/Valt.UI/Program.cs` - Application entry, Avalonia initialization
- `src/Valt.UI/App.axaml.cs` - DI composition, background job startup

**Configuration:**
- `Directory.Packages.props` - Central NuGet package versions
- `src/Valt.*/Valt.*.csproj` - Project files with framework targets

**Core Logic:**
- `src/Valt.Core/Modules/Budget/Transactions/Transaction.cs` - Transaction aggregate
- `src/Valt.Core/Modules/AvgPrice/AvgPriceProfile.cs` - Cost basis profile aggregate
- `src/Valt.Infra/Modules/Reports/` - Report generators

**Testing:**
- `tests/Valt.Tests/Builders/` - Test data builders (REQUIRED for all tests)
- `tests/Valt.Tests/DatabaseTest.cs` - In-memory database test base

**Documentation:**
- `CLAUDE.md` - AI assistant instructions for working with codebase

## Naming Conventions

**Files:**
- PascalCase for all C# files (e.g., `TransactionRepository.cs`)
- `.axaml` for Avalonia views (e.g., `MainView.axaml`)
- `I` prefix for interfaces (e.g., `IAccountRepository.cs`)

**Directories:**
- PascalCase for all directories
- Plural for collections (e.g., `Modules/`, `Builders/`)

**Special Patterns:**
- `*Entity.cs` - LiteDB mapped entities (e.g., `TransactionEntity.cs`)
- `*ViewModel.cs` - MVVM ViewModels (e.g., `MainViewModel.cs`)
- `*Builder.cs` - Test data builders (e.g., `TransactionBuilder.cs`)
- `*Tests.cs` - Test classes (e.g., `TransactionTests.cs`)

## Where to Add New Code

**New Feature:**
- Domain model: `src/Valt.Core/Modules/{Module}/`
- Repository: `src/Valt.Infra/Modules/{Module}/`
- UI: `src/Valt.UI/Views/Main/` (tab or modal)
- Tests: `tests/Valt.Tests/Domain/{Module}/`

**New Background Job:**
- Implementation: `src/Valt.Infra/Kernel/BackgroundJobs/Jobs/`
- Auto-registered via Scrutor assembly scanning

**New Modal Dialog:**
- View: `src/Valt.UI/Views/Main/Modals/{ModalName}/`
- Register: `src/Valt.UI/Services/ApplicationModalNames.cs`

**New Value Object:**
- Definition: `src/Valt.Core/Common/`
- Tests: `tests/Valt.Tests/Domain/Common/`

**New Test Builder:**
- Location: `tests/Valt.Tests/Builders/`
- Pattern: Static factory methods with fluent API

## Special Directories

**.planning/**
- Purpose: Project planning documents (generated by GSD)
- Contains: PROJECT.md, STATE.md, ROADMAP.md, codebase analysis
- Committed: Yes

**bin/, obj/**
- Purpose: Build output directories
- Source: Generated by MSBuild
- Committed: No (in .gitignore)

---

*Structure analysis: 2026-01-13*
*Update when directory structure changes*
