# Testing Patterns

**Analysis Date:** 2026-01-14

## Test Framework

**Runner:**
- NUnit 4.4.0
- NUnit3TestAdapter 5.2.0 for test discovery
- NUnit.Analyzers 4.11.1 for static analysis

**Mocking:**
- NSubstitute 5.3.0 for interface mocking

**Architecture Testing:**
- NetArchTest.Rules 1.3.2 for layer dependency validation

**Run Commands:**
```bash
dotnet test                                    # Run all tests
dotnet test --filter "FullyQualifiedName~TransactionTests"  # Run specific class
dotnet test --filter "FullyQualifiedName~Should_Create"     # Run by name pattern
```

## Test File Organization

**Location:**
- All tests in `tests/Valt.Tests/`
- Mirror source structure: `Domain/Budget/Accounts/`, `UseCases/Budget/Transactions/`

**Naming:**
- `{ClassName}Tests.cs` for test files: `TransactionTests.cs`, `CategoryNameTests.cs`
- `Should_{Behavior}_When_{Condition}` for test methods

**Structure:**
```
tests/Valt.Tests/
├── Builders/           # Test data factories
├── Domain/             # Domain model tests
│   ├── Budget/
│   │   ├── Accounts/
│   │   ├── Transactions/
│   │   └── Categories/
│   └── AvgPrice/
├── UseCases/           # Query handler tests
├── UI/                 # ViewModel tests
├── Reports/            # Report generator tests
├── CsvImport/          # CSV import tests
└── Architecture/       # Layer constraint tests
```

## Test Structure

**Suite Organization:**
```csharp
[TestFixture]
public class TransactionTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        // Setup test data
    }

    #region Creation Tests
    [Test]
    public void Should_Create_Transaction_With_Valid_Data()
    {
        // Arrange
        var transaction = new TransactionBuilder()
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("Test Transaction")
            .BuildDomainObject();

        // Act & Assert
        Assert.That(transaction.Name.Value, Is.EqualTo("Test Transaction"));
    }
    #endregion
}
```

**Patterns:**
- `[OneTimeSetUp]` for IdGenerator configuration
- `[SetUp]` for per-test setup
- `#region` blocks for grouping related tests
- Arrange/Act/Assert pattern with comments

## Base Test Classes

**DatabaseTest:**
- Location: `tests/Valt.Tests/DatabaseTest.cs`
- Purpose: In-memory LiteDB for isolated unit tests
- Provides: `_localDatabase`, `_priceDatabase`, repositories
- Override: `SeedDatabase()` for test data

```csharp
public class AccountRepositoryTests : DatabaseTest
{
    protected override async Task SeedDatabase()
    {
        // Add test accounts
    }
}
```

**IntegrationTest:**
- Location: `tests/Valt.Tests/IntegrationTest.cs`
- Purpose: Full DI container with all services
- Provides: `_serviceProvider`, `_serviceCollection`
- Method: `ReplaceService<T>()` for mocking specific services

## Builder Pattern (CRITICAL)

**Always use Builder classes for test data creation.**

**Available Builders (`tests/Valt.Tests/Builders/`):**

```csharp
// TransactionBuilder
var transaction = new TransactionBuilder()
    .WithDate(new DateOnly(2023, 1, 1))
    .WithName("My Transaction")
    .WithCategoryId(_categoryId)
    .WithTransactionDetails(new FiatDetails(_fiatAccountId, 153.32m, true))
    .BuildDomainObject();

// FiatAccountBuilder
var account = FiatAccountBuilder.AnAccount()
    .WithName("Checking")
    .WithFiatCurrency(FiatCurrency.Brl)
    .Build();

// BtcAccountBuilder
var btcAccount = BtcAccountBuilder.AnAccount()
    .WithName("Cold Wallet")
    .Build();

// CategoryBuilder
var category = CategoryBuilder.ACategory()
    .WithName("Food")
    .Build();

// FixedExpenseBuilder
var expense = FixedExpenseBuilder.AFixedExpense()
    .WithName("Rent")
    .WithAmount(1000m)
    .Build();

// AvgPriceLineBuilder
var line = AvgPriceLineBuilder.ABuyLine()
    .WithDate(new DateOnly(2024, 1, 1))
    .WithQuantity(1m)
    .WithAmount(FiatValue.New(50000m))
    .Build();

// AvgPriceProfileBuilder
var profile = AvgPriceProfileBuilder.ABrazilianRuleProfile()
    .Build();
```

**Builder Static Factories:**
- `TransactionBuilder.ATransaction()`
- `FiatAccountBuilder.AnAccount()`
- `BtcAccountBuilder.AnAccount()`
- `CategoryBuilder.ACategory()`
- `FixedExpenseBuilder.AFixedExpense()`, `.AFixedExpenseWithAccount()`, `.AFixedExpenseWithCurrency()`
- `AvgPriceLineBuilder.ABuyLine()`, `.ASellLine()`, `.ASetupLine()`
- `AvgPriceProfileBuilder.AProfile()`, `.ABrazilianRuleProfile()`, `.AFifoProfile()`

## Mocking

**NSubstitute Patterns:**
```csharp
// Create substitute
_domainEventPublisher = Substitute.For<IDomainEventPublisher>();

// Set return value
_accountQueries.GetAccountsAsync(showHiddenAccounts: true)
    .Returns(Task.FromResult(accounts));

// Verify calls
await _domainEventPublisher.Received(1)
    .PublishAsync(Arg.Any<AccountCreatedEvent>());
```

**What to Mock:**
- Domain event publisher (`IDomainEventPublisher`)
- External API clients
- Query services when testing ViewModels

**What NOT to Mock:**
- Domain entities and value objects
- Internal pure functions
- LiteDB (use in-memory database instead)

## Test Categories

**Domain Tests:**
- Test aggregate behavior in isolation
- Use builders for test data
- Assert on domain events raised

**Repository Tests:**
- Use `DatabaseTest` base class
- Test persistence and retrieval
- Verify event publishing

**Query Handler Tests:**
- Use `DatabaseTest` or `IntegrationTest`
- Test data retrieval and mapping
- Verify DTO structure

**ViewModel Tests:**
- Mock dependencies with NSubstitute
- Test command execution
- Verify property notifications

**Architecture Tests:**
- Use NetArchTest.Rules
- Verify layer dependencies
- Ensure interfaces in contracts

## Coverage

**Requirements:**
- No enforced coverage target
- Focus on critical paths (domain logic, calculations)

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Common Patterns

**Async Testing:**
```csharp
[Test]
public async Task Should_Save_Transaction_Async()
{
    var transaction = new TransactionBuilder().BuildDomainObject();

    await _repository.SaveTransactionAsync(transaction);

    var loaded = await _repository.GetTransactionByIdAsync(transaction.Id);
    Assert.That(loaded, Is.Not.Null);
}
```

**Error Testing:**
```csharp
[Test]
public void Should_Throw_On_Empty_Name()
{
    Assert.Throws<EmptyCategoryNameException>(() =>
        CategoryBuilder.ACategory().WithName("").Build());
}
```

**Event Verification:**
```csharp
[Test]
public async Task Should_Publish_Event_On_Save()
{
    var account = FiatAccountBuilder.AnAccount().Build();

    await _repository.SaveAccountAsync(account);

    await _domainEventPublisher.Received(1)
        .PublishAsync(Arg.Any<AccountCreatedEvent>());
}
```

---

*Testing analysis: 2026-01-14*
*Update when test patterns change*
