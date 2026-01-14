# Coding Conventions

**Analysis Date:** 2026-01-14

## Naming Patterns

**Files:**
- PascalCase for all C# files: `Transaction.cs`, `AccountRepository.cs`
- Entity suffix for database entities: `TransactionEntity.cs`, `AccountEntity.cs`
- Exception suffix for exceptions: `InvalidBtcValueException.cs`, `AccountHasTransactionsException.cs`
- Event suffix for domain events: `TransactionCreatedEvent.cs`, `AccountUpdatedEvent.cs`

**Functions:**
- PascalCase for all methods (C# convention)
- Factory methods: `New()` for new aggregates, `Create()` for reconstruction from DB
- Async suffix for async methods: `SaveAccountAsync()`, `GetAccountsAsync()`
- Boolean methods: `Has*`, `Is*`, `Can*`: `HasAutoSatAmount`, `IsVisible`

**Variables:**
- camelCase with underscore prefix for private fields: `_id`, `_name`, `_localDatabase`
- PascalCase for public properties: `Id`, `Name`, `Amount`
- UPPER_SNAKE_CASE not used (C# convention)

**Types:**
- PascalCase for interfaces with `I` prefix: `IAccountRepository`, `ICsvImportParser`
- PascalCase for classes: `Transaction`, `BtcValue`, `AccountRepository`
- Record types for value objects: `public record BtcValue`, `public record FiatValue`
- Strongly-typed IDs: `AccountId`, `TransactionId`, `CategoryId`

## Code Style

**Formatting:**
- 4-space indentation (C# standard)
- File-scoped namespaces: `namespace Valt.Core.Modules.Budget.Accounts;`
- Nullable reference types enabled: `<Nullable>enable</Nullable>`
- Implicit usings enabled: `<ImplicitUsings>enable</ImplicitUsings>`

**Code Organization:**
- `#region` / `#endregion` for logical grouping in large files
- Example: `#region Validation Tests`, `#region Step 1 - File Selection`

**Visibility:**
- `internal` for infrastructure implementations: `internal class TransactionRepository`
- `[assembly: InternalsVisibleTo("Valt.Tests")]` in `src/Valt.Core/Extensions.cs`
- `private` fields with underscore prefix

## Import Organization

**Order:**
1. System namespaces (`System`, `System.Collections`, etc.)
2. External packages (`CommunityToolkit.Mvvm`, `LiteDB`, etc.)
3. Internal namespaces (`Valt.Core`, `Valt.Infra`, etc.)

**Grouping:**
- No blank lines between groups (C# convention)
- Type imports via `using static` when needed

## Error Handling

**Patterns:**
- Domain-specific exceptions in `src/Valt.Core/`
- Throw in domain methods, catch at boundaries (ViewModels, repositories)
- Async methods use try/catch, no `.catch()` chains

**Error Types:**
- `DomainException` base class for domain errors
- `EntityNotFoundException` for missing aggregates
- Value object validation: `InvalidBtcValueException`, `InvalidFiatValueException`
- Business rule violations: `AccountHasTransactionsException`, `EmptyCategoryNameException`

**Exception Throwing:**
```csharp
if (string.IsNullOrWhiteSpace(name))
    throw new EmptyCategoryNameException();
```

## Logging

**Framework:**
- Microsoft.Extensions.Logging with `ILogger<T>`
- Injected per-class via constructor

**Patterns:**
- `_logger.LogError(ex, "Error message")` for exceptions
- `_logger.LogWarning("Warning message")` for non-critical issues
- Silent fallbacks in price crawlers with logging

## Comments

**When to Comment:**
- TODO comments for planned work: `//TODO: move to a specific app layer`
- Explain business rules and non-obvious behavior
- Document public APIs with XML doc comments

**XML Doc Comments:**
```csharp
/// <summary>
/// Builder for creating Transaction test data.
/// </summary>
public class TransactionBuilder { }
```

**TODO Comments:**
- Format: `//TODO: description`
- No username prefix (use git blame)

## Function Design

**Size:**
- Extract helpers for complex logic
- Use `#region` for grouping in large files

**Parameters:**
- Constructor injection for dependencies
- Use object parameters for complex options
- Strongly-typed IDs: `TransactionId`, not `string`

**Return Values:**
- Nullable for queries that may not find results
- `Task<T?>` for async operations
- Early returns for guard clauses

## Module Design

**Exports:**
- Public interfaces in `Contracts/` subdirectory
- Internal implementations in module root
- `Extensions.cs` for DI registration

**Domain Module Pattern:**
```
Valt.Core/Modules/{ModuleName}/
├── {Feature}/              # Aggregate and related entities
├── {Feature}/Contracts/    # Repository interfaces
├── {Feature}/Events/       # Domain events
└── Extensions.cs           # Module DI registration
```

**Infrastructure Module Pattern:**
```
Valt.Infra/Modules/{ModuleName}/
├── {Feature}/
│   ├── {Feature}Repository.cs
│   ├── {Feature}Entity.cs
│   ├── Queries/            # Query handlers and DTOs
│   ├── Services/           # Business services
│   └── Handlers/           # Event handlers
└── Extensions.cs           # Module DI registration
```

## MVVM Patterns

**ViewModel Attributes:**
- `[ObservableProperty]` for bindable properties
- `[RelayCommand]` for commands
- `[NotifyPropertyChangedFor]` for dependent properties

**Example:**
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CanGoNext))]
private WizardStep _currentStep;

[RelayCommand]
private async Task GoNext() { }
```

**Base Classes:**
- `ValtViewModel` - Standard ViewModel
- `ValtModalViewModel` - Modal dialogs with close action
- `ValtTabViewModel` - Main tabs with tab name
- `ValtValidatorViewModel` - ViewModels with validation

---

*Convention analysis: 2026-01-14*
*Update when patterns change*
