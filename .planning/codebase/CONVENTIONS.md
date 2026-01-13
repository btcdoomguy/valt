# Coding Conventions

**Analysis Date:** 2026-01-13

## Naming Patterns

**Files:**
- PascalCase for all C# files (e.g., `Transaction.cs`, `AccountRepository.cs`)
- `I` prefix for interfaces (e.g., `IAccountRepository.cs`, `ILocalDatabase.cs`)
- `*Entity.cs` for LiteDB entities (e.g., `TransactionEntity.cs`)
- `*ViewModel.cs` for ViewModels (e.g., `MainViewModel.cs`)
- `*Tests.cs` for test classes (e.g., `TransactionTests.cs`)
- `*Builder.cs` for test data builders (e.g., `TransactionBuilder.cs`)

**Classes:**
- PascalCase for all types
- `{Entity}Id` for strong-typed IDs (e.g., `TransactionId`, `AccountId`)
- `{Entity}Name` for name value objects (e.g., `AccountName`, `CategoryName`)

**Functions:**
- PascalCase for public methods (e.g., `GetAccountByIdAsync`, `SaveAsync`)
- Async suffix for async methods (e.g., `GetTransactionByIdAsync`)
- Action verbs: `Add`, `Change`, `Remove`, `Get`, `Save`, `Update`

**Variables:**
- camelCase for local variables and parameters
- `_underscorePrefix` for private fields (e.g., `_clock`, `_database`, `_events`)

**Types:**
- `I` prefix for interfaces (e.g., `IRepository`, `IDomainEvent`)
- Records for value objects (e.g., `public record BtcValue`)

## Code Style

**Formatting:**
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- File-scoped namespaces (e.g., `namespace Valt.Core.Modules.Budget;`)
- Expression-bodied members for simple properties

**Language Features:**
- C# 14 with .NET 10
- Collection expressions (e.g., `_events = []`)
- Target-typed new expressions
- Pattern matching in switch expressions

## Import Organization

**Order:**
1. System namespaces
2. Microsoft namespaces
3. Third-party packages
4. Internal namespaces (Valt.*)

**Patterns:**
- File-scoped usings
- No explicit grouping required (implicit usings handle System.*)

## Error Handling

**Patterns:**
- Guard clauses in domain entities (early validation)
- Exceptions for domain invariant violations
- Graceful degradation in crawlers (log and return empty)
- Try/catch at service boundaries

**Domain Validation:**
```csharp
public void Rename(AccountName name)
{
    if (Name == name)
        return;
    Name = name;
    AddEvent(new AccountUpdatedEvent(this));
}
```

## Domain Event Pattern

**Publishing:**
- Aggregates emit events via `AddEvent()` method (`src/Valt.Core/Kernel/AggregateRoot.cs`)
- Events collected in `Events` property
- Cleared after publishing via `ClearEvents()`

**Example:**
```csharp
public static Transaction New(...)
{
    var transaction = new Transaction(...);
    transaction.AddEvent(new TransactionCreatedEvent(transaction));
    return transaction;
}
```

## Factory Methods

**Static factory pattern for entity creation:**
- `New(...)` - Create new instance with new ID
- `Create(...)` - Reconstitute from persistence (existing ID)

**Example from `src/Valt.Core/Modules/Budget/Categories/Category.cs`:**
```csharp
public static Category New(CategoryName name, Icon icon, CategoryId? parentId = null)
public static Category Create(CategoryId id, CategoryName name, Icon icon, CategoryId? parentId = null)
```

## MVVM Patterns

**ViewModel Base:**
- Inherit from `ValtViewModel` (extends `ObservableObject`)
- Use `[ObservableProperty]` for bindable properties
- Use `[RelayCommand]` for commands

**Example:**
```csharp
public partial class MyViewModel : ValtViewModel
{
    [ObservableProperty]
    private string _name = string.Empty;

    [RelayCommand]
    private async Task SaveAsync() { ... }
}
```

## Comments

**When to Comment:**
- TODO comments for known issues: `//TODO: description`
- XML documentation on public interfaces
- Explain "why" not "what" for complex logic

**Documentation:**
- XML docs required on public API interfaces
- Builder classes include summary documentation
- Test classes include purpose documentation

## Module Design

**Domain Modules:**
- Each module has own subdirectory: `Modules/{ModuleName}/`
- Contains: Entity, Events, Contracts (interfaces)
- Repository interface in `Contracts/` subdirectory

**Infrastructure Modules:**
- Mirror domain structure: `Modules/{ModuleName}/`
- Contains: Repository implementation, Entity (LiteDB), Queries, Handlers

**Exports:**
- No barrel files (index.cs)
- Direct class references via namespace imports

---

*Convention analysis: 2026-01-13*
*Update when patterns change*
