# Testing Patterns

**Analysis Date:** 2026-01-13

## Test Framework

**Runner:**
- NUnit 4.4.0
- Config: `tests/Valt.Tests/Valt.Tests.csproj`

**Assertion Library:**
- NUnit built-in `Assert.That()` syntax
- Matchers: `Is.EqualTo`, `Is.Empty`, `Is.InstanceOf`

**Mocking:**
- NSubstitute 5.3.0
- Pattern: `Substitute.For<IInterface>()`

**Run Commands:**
```bash
dotnet test                                          # Run all tests
dotnet test --filter "FullyQualifiedName~Valt.Tests.Domain"  # Tests in Domain folder
dotnet test --filter "FullyQualifiedName~TransactionTests"   # Specific test class
```

## Test File Organization

**Location:**
- All tests in `tests/Valt.Tests/`
- Organized by layer: `Domain/`, `UseCases/`, `UI/`, `Reports/`, `Jobs/`, `Architecture/`

**Naming:**
- Test classes: `{Subject}Tests.cs` (e.g., `TransactionTests.cs`)
- Test methods: `Should_[ExpectedBehavior]_When_[Condition]`

**Structure:**
```
tests/Valt.Tests/
├── Builders/           # Test data builders (REQUIRED)
├── Domain/             # Domain entity tests
│   ├── Budget/
│   │   ├── Accounts/
│   │   ├── Categories/
│   │   ├── Transactions/
│   │   └── FixedExpenses/
│   ├── AvgPrice/
│   └── Common/
├── UseCases/           # Query handler tests
├── UI/                 # ViewModel and converter tests
├── Reports/            # Report generator tests
├── Jobs/               # Background job tests
├── Architecture/       # Layer dependency tests
├── DatabaseTest.cs     # Base class for DB tests
└── IntegrationTest.cs  # Base class for full DI tests
```

## Test Base Classes

**DatabaseTest (`tests/Valt.Tests/DatabaseTest.cs`):**
- Purpose: Unit tests with in-memory LiteDB
- Provides: `_localDatabase`, `_priceDatabase`, `_domainEventPublisher`
- Setup: `[OneTimeSetUp]` creates DB, `[SetUp]` refreshes state
- Use for: Repository tests, domain tests with persistence

**IntegrationTest (`tests/Valt.Tests/IntegrationTest.cs`):**
- Purpose: Full DI container tests
- Provides: `_serviceProvider` with all services
- Supports: `ReplaceService<T>()` for substituting services
- Use for: End-to-end tests, event publishing verification

## Builder Pattern (REQUIRED)

**CRITICAL: Always use Builder classes for test data creation.**

**Location:** `tests/Valt.Tests/Builders/`

**Available Builders:**
- `TransactionBuilder.ATransaction()` - Transaction entities
- `CategoryBuilder.ACategory()` - Category entities
- `FiatAccountBuilder.AnAccount()` - Fiat Account entities
- `BtcAccountBuilder.AnAccount()` - Bitcoin Account entities
- `FixedExpenseBuilder.AFixedExpense()` - Fixed Expense entities
- `AvgPriceProfileBuilder.AProfile()` / `.ABrazilianRuleProfile()` / `.AFifoProfile()`
- `AvgPriceLineBuilder.ABuyLine()` / `.ASellLine()` / `.ASetupLine()`
- `FakeClock` - Time-dependent testing

**Builder Usage:**
```csharp
// Good - Using builders with fluent API
var account = FiatAccountBuilder.AnAccount()
    .WithName("My Account")
    .WithFiatCurrency(FiatCurrency.Brl)
    .Build();

var line = AvgPriceLineBuilder.ABuyLine()
    .WithDate(new DateOnly(2024, 1, 1))
    .WithQuantity(1m)
    .WithAmount(FiatValue.New(50000m))
    .Build();

// Bad - Direct construction (brittle)
var account = new FiatAccount(...);  // DON'T DO THIS
```

## Test Structure

**Arrange-Act-Assert Pattern:**
```csharp
[Test]
public void Should_Calculate_Total_After_Buy()
{
    // Arrange: Set up initial state
    var profile = AvgPriceProfileBuilder.AProfile().Build();

    // Act: Perform the action being tested
    profile.AddLine(...);

    // Assert: Verify expected outcomes
    Assert.That(profile.AvgPriceLines.Count, Is.EqualTo(1));
}
```

**Region Organization:**
```csharp
#region Creation Tests
[Test]
public void Should_Create_Transaction_With_Valid_Data() { }
#endregion

#region Validation Tests
[Test]
public void Should_Throw_When_Invalid_Input() { }
#endregion
```

## Test Setup

**IdGenerator Configuration:**
```csharp
[OneTimeSetUp]
public void OneTimeSetUp()
{
    IdGenerator.Configure(new LiteDbIdProvider());
}
```

**Database Seeding:**
```csharp
protected override async Task SeedDatabase()
{
    _fiatAccountId = IdGenerator.Generate();
    var fiatAccount = FiatAccountBuilder.AnAccount()
        .WithId(_fiatAccountId)
        .Build();
    _localDatabase.GetAccounts().Insert(fiatAccount);
    await base.SeedDatabase();
}
```

## Assertion Patterns

**NUnit Constraint Model:**
```csharp
// Equality
Assert.That(actual, Is.EqualTo(expected));

// Type checking
Assert.That(object, Is.InstanceOf(typeof(BtcAccount)));

// Collections
Assert.That(collection, Is.Empty);
Assert.That(collection.Count, Is.EqualTo(5));

// Null checking
Assert.That(result, Is.Null);
Assert.That(result, Is.Not.Null);
```

**Mock Verification (NSubstitute):**
```csharp
await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<AccountCreatedEvent>());
_repository.DidNotReceive().SaveAsync(Arg.Any<Transaction>());
```

## Test Types

**Unit Tests:**
- Location: `Domain/`
- Scope: Single entity behavior
- Examples: `TransactionTests.cs`, `AvgPriceProfileTests.cs`

**Integration Tests:**
- Location: `UseCases/`, repository tests
- Scope: Multiple modules working together
- Base: `DatabaseTest` or `IntegrationTest`

**Architecture Tests:**
- Location: `Architecture/`
- Purpose: Verify layer dependencies
- Framework: NetArchTest.Rules

**Examples:**
```csharp
[Test]
public void Domain_Should_Not_Depend_On_Infrastructure()
{
    var result = Types.InAssembly(typeof(Transaction).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Valt.Infra")
        .GetResult();

    Assert.That(result.IsSuccessful, Is.True);
}
```

## Coverage

**Requirements:**
- No enforced coverage target
- Focus on critical paths (domain logic, calculations)

**Run Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

*Testing analysis: 2026-01-13*
*Update when test patterns change*
