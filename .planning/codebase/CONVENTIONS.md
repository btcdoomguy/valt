# Coding Conventions

**Analysis Date:** 2026-05-27

## Naming Patterns

**Files:**
- PascalCase, matching the primary type name (e.g., `CreateFiatAccountHandler.cs`)
- One type per file, with rare exceptions for tightly coupled DTOs
- Handlers live in folders named after the command/query (e.g., `CreateFiatAccount/CreateFiatAccountHandler.cs`)

**Classes:**
- Commands: `XxxCommand` — `public sealed record` or `public sealed class`
- Command handlers: `XxxHandler` — `internal sealed class`
- Queries: `XxxQuery` — `public sealed record` or `public sealed class`
- Query handlers: `XxxHandler` — `internal sealed class`
- ViewModels: `XxxViewModel` — `public partial class` (when using `[ObservableProperty]` source generators) or `public class`
- Repositories: `XxxRepository` — `public sealed class`
- Domain events: `XxxEvent` — `public sealed record` implementing `IDomainEvent`
- DTOs: `XxxDTO` — `public record` (named tuple style) or `public sealed record`
- Persistence entities: `XxxEntity` — in Infra layer only
- Validators: `XxxValidator` — `internal sealed class`
- Domain event handlers: `XxxHandler` — `internal sealed class`

**Functions/Methods:**
- PascalCase for all methods
- Async methods suffixed with `Async`
- Private helper methods use PascalCase
- Test methods: descriptive sentence style, e.g., `Should_Create_BtcValue_With_Given_Sats` or `HandleAsync_WithInvalidPeriod_ReturnsValidationError`

**Variables:**
- `camelCase` for locals and parameters
- `_camelCase` for private fields
- `PascalCase` for properties
- `PascalCase` for constants with `const` or `readonly`

**Types:**
- Value objects: no mandatory suffix, e.g., `BtcValue`, `FiatValue`, `TransactionName`
- Entity IDs: strong-typed wrapper, e.g., `TransactionId`, `AccountId`
- Interfaces: `IXxx` prefix, e.g., `ITransactionRepository`, `IValidator<T>`

## Code Style

**Formatting:**
- File-scoped namespaces (`namespace Xxx;`)
- 4-space indentation
- Braces on same line (K&R style)
- `editor.formatOnSave: true` in `.editorconfig`
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- C# 14 language version (`<LangVersion>14</LangVersion>`)

**Linting:**
- No StyleCop, ESLint, or Biome detected
- NUnit.Analyzers enabled in test project
- Architecture conventions enforced via NetArchTest.Rules in tests

## Import Organization

**Order:**
1. `System` namespaces
2. Third-party framework namespaces (e.g., `Avalonia`, `CommunityToolkit.Mvvm`, `LiteDB`, `Microsoft.Extensions`)
3. Valt project namespaces, ordered by layer:
   - `Valt.Core.*` (domain)
   - `Valt.App.*` (application)
   - `Valt.Infra.*` (infrastructure)
   - `Valt.UI.*` (presentation)

**No path aliases** — full namespace imports used throughout.

## Error Handling

**Primary Pattern: Result<T>**

Commands return `Result<T>` for railway-oriented error handling:

```csharp
public async Task<Result<CreateFiatAccountResult>> HandleAsync(CreateFiatAccountCommand command, CancellationToken ct = default)
{
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
        return Result<CreateFiatAccountResult>.ValidationFailure(
            new Dictionary<string, string[]>(validation.Errors));
    }
    // ... success path
    return Result<CreateFiatAccountResult>.Success(new CreateFiatAccountResult(account.Id.Value));
}
```

**Error factory methods:**
- `Error.NotFound(entityType, id)` → code `ENTITYTYPE_NOT_FOUND`
- `Error.Validation(message, errors)` → code `VALIDATION_FAILED`
- `Error.Conflict(message)` → code `CONFLICT`
- `Error.Internal(message)` → code `INTERNAL_ERROR`

**Validation Pattern:**
- Implement `IValidator<T>` with `ValidationResult Validate(T instance)`
- Use `ValidationResultBuilder` for collecting multiple errors:

```csharp
var builder = new ValidationResultBuilder();
builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Account name is required.");
builder.AddErrorIf(command.Name.Length > MaxNameLength, nameof(command.Name), $"Name too long.");
return builder.Build();
```

**Guard Clauses:**
- Use `ArgumentNullException.ThrowIfNull(parameter)` for null checks
- Early return on no-op state changes in domain entities

## Logging

**Framework:** `Microsoft.Extensions.Logging`

**Patterns:**
- Injected via constructor: `ILogger<ClassName>`
- Used in ViewModels and background jobs
- No custom logging abstractions — standard MS logging throughout

## Comments

**When to Comment:**
- XML documentation on public APIs, base classes, and interfaces
- Inline comments for complex business logic or non-obvious decisions
- Region blocks (`#region Creation Tests`) used extensively in test files
- Section comments in validators to group related rules

**JSDoc/TSDoc:**
- Not applicable (C# project)
- XML docs (`/// <summary>`) used on public types and members

## Function Design

**Size:**
- Handlers typically 30-80 lines
- Validators typically 20-50 lines
- Domain entity methods are short (5-20 lines)
- Large ViewModels exist (e.g., `TransactionsViewModel` at ~800 lines) — this is accepted for screen controllers

**Parameters:**
- Constructor injection for dependencies
- DTOs/records for multi-parameter method inputs
- `CancellationToken ct = default` on all async handler methods

**Return Values:**
- Commands: `Task<Result<T>>`
- Queries: `Task<T>` or `Task<IReadOnlyList<T>>`
- Domain mutators: `void` (emit events via `AddEvent`)

## Module Design

**Exports:**
- Public contracts (interfaces, DTOs, commands, queries) are `public`
- Implementation details (handlers, validators, event handlers) are `internal`
- Test projects reference all four source projects to access internals

**Barrel Files:**
- No barrel files detected
- Each type imported by its full namespace path

## Domain Layer Constraints

**Critical rules enforced by architecture tests:**
- No serialization attributes in domain classes (no `[JsonPropertyName]`, `[BsonField]`)
- DTOs for serialization live in Infra layer
- Domain layer has zero dependencies on `LiteDB`, `Avalonia`, `Newtonsoft.Json`, or `System.Text.Json.Serialization`

## Threading & Synchronization

**Use `Lock` class (.NET 9+):**
```csharp
// Good
private readonly Lock _lock = new();
lock (_lock) { /* ... */ }

// Avoid
private readonly object _lock = new();
lock (_lock) { /* ... */ }
```

## Special Patterns

**Domain Events:**
- Aggregate roots emit events via `protected void AddEvent(IDomainEvent @event)`
- Events are cleared after publishing
- Version is auto-incremented on first event per change

**MVVM Source Generators:**
- `[ObservableProperty]` generates properties from fields
- `[RelayCommand]` generates `ICommand` implementations
- ViewModels using source generators must be `partial`

**Factory Pattern:**
- Domain entities have `New(...)` factory methods for creation
- `Create(...)` methods for reconstruction from persistence
- `IdGenerator.Generate()` or `new EntityId()` for ID creation

---

*Convention analysis: 2026-05-27*
