# Testing Patterns

**Analysis Date:** 2026-05-27

## Test Framework

**Runner:**
- NUnit 4.4.0
- NUnit3TestAdapter 5.2.0
- Microsoft.NET.Test.Sdk 18.0.0
- Config: `tests/Valt.Tests/Valt.Tests.csproj`

**Assertion Library:**
- NUnit built-in assertions (`Assert.That`, `Assert.Throws`, `Assert.Multiple`)

**Architecture Testing:**
- NetArchTest.Rules 1.3.2

**Mocking:**
- NSubstitute 5.3.0

**Coverage:**
- coverlet.collector 3.2.0

**Run Commands:**
```bash
dotnet test                              # Run all tests
dotnet test --filter "FullyQualifiedName~Valt.Tests.Domain.Budget.Transactions.TransactionTests"  # Specific test class
dotnet test --list-tests                 # List all test methods
```

## Test File Organization

**Location:**
- All tests in `tests/Valt.Tests/`
- Mirror source structure with layer prefixes:
  - `Domain/` — entity and value object unit tests
  - `Application/` — command and query handler tests
  - `Infrastructure/` — repository, data access, and integration tests
  - `UI/` — ViewModel and converter tests
  - `Reports/` — report generation tests
  - `CsvImport/` / `CsvExport/` — CSV processing tests
  - `Jobs/` — background job tests
  - `LivePriceCrawlers/` — price provider tests
  - `Architecture/` — architecture rule tests (NetArchTest)
  - `Builders/` — test data builders

**Naming:**
- Test classes: `{ClassUnderTest}Tests.cs`
- All test classes decorated with `[TestFixture]`

**Structure:**
```
tests/Valt.Tests/
├── GlobalUsings.cs
├── DatabaseTest.cs
├── IntegrationTest.cs
├── LocalDatabaseTests.cs
├── Builders/
│   ├── TransactionBuilder.cs
│   ├── FiatAccountBuilder.cs
│   ├── BtcAccountBuilder.cs
│   ├── CategoryBuilder.cs
│   ├── FixedExpenseBuilder.cs
│   ├── AvgPriceLineBuilder.cs
│   ├── AvgPriceProfileBuilder.cs
│   ├── GoalBuilder.cs
│   ├── AssetBuilder.cs
│   ├── AssetGroupBuilder.cs
│   ├── AccountGroupBuilder.cs
│   └── FakeClock.cs
├── Domain/
│   ├── Budget/Transactions/TransactionTests.cs
│   ├── Common/BtcValueTests.cs
│   ├── Common/FiatValueTests.cs
│   └── ...
├── Application/
│   ├── Budget/Commands/AddTransactionHandlerTests.cs
│   └── ...
├── Infrastructure/
│   ├── Budget/Accounts/AccountRepositoryTests.cs
│   └── ...
├── UI/
│   ├── ViewModels/AccountViewModelTests.cs
│   └── Screens/TransactionEditorViewModelTests.cs
├── Reports/
│   ├── MonthlyTotalsReportTests.cs
│   └── ...
└── Architecture/
    ├── LayerDependencyTests.cs
    ├── NamingConventionTests.cs
    ├── HandlerVisibilityTests.cs
    └── DomainModelTests.cs
```

## Test Structure

**Suite Organization:**
```csharp
[TestFixture]
public class CreateGoalHandlerTests : DatabaseTest
{
    private CreateGoalHandler _handler = null!;
    private Category _category = null!;

    protected override async Task SeedDatabase()
    {
        _category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_category);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateGoalHandler(
            _goalRepository,
            _categoryRepository,
            new CreateGoalValidator());
    }

    [Test]
    public async Task HandleAsync_WithStackBitcoinGoal_CreatesGoal()
    {
        var command = new CreateGoalCommand { ... };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
        });
    }
}
```

**Patterns:**
- Use `protected override Task SeedDatabase()` in `DatabaseTest` subclasses to set up persistent test data
- Use `[SetUp]` for per-test instance initialization
- Use `[OneTimeSetUp]` / `[OneTimeTearDown]` for database lifecycle management
- Use `null!` for fields initialized in SetUp to satisfy nullable analysis

**Assertion Patterns:**
- `Assert.That(result.IsSuccess, Is.True)` — result verification
- `Assert.Multiple(() => { ... })` — group multiple assertions
- `Assert.Throws<TException>(() => ...)` — exception testing

## Mocking

**Framework:** NSubstitute

**Patterns:**
```csharp
// Create mock
var mockPublisher = Substitute.For<IDomainEventPublisher>();

// Setup return values
_queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
    .Returns(callInfo => Task.FromResult<IReadOnlyList<AccountDTO>>(_accounts.ToList()));

// Verify calls (used sparingly — prefer state-based testing)
await _repository.Received(1).SaveAsync(Arg.Any<Transaction>());
```

**What to Mock:**
- External dependencies (repositories in handler tests, dispatchers in ViewModel tests)
- `IDomainEventPublisher` in `DatabaseTest` base class
- `ILocalStorageService` in integration tests
- `INotificationPublisher` for price database tests

**What NOT to Mock:**
- Domain entities and value objects (test directly)
- Validators (use real instances)
- Builders (use real instances)
- LiteDB in `DatabaseTest` — uses real in-memory database

## Fixtures and Factories

**Test Data Builders:**
All builders in `tests/Valt.Tests/Builders/` use fluent API:

```csharp
// Good — use builders
var account = FiatAccountBuilder.AnAccount()
    .WithName("Checking")
    .WithFiatCurrency(FiatCurrency.Usd)
    .Build();

var transaction = TransactionBuilder.ATransaction()
    .WithDate(new DateOnly(2024, 1, 1))
    .AsBitcoinPurchase(1_000_000)
    .Build();
```

**Available Builders:**
- `TransactionBuilder` — `.ATransaction()`, `.AsBitcoinPurchase()`, `.AsBitcoinSale()`, `.AsFiatExpense()`, `.AsFiatIncome()`, `.AsBitcoinIncome()`, `.AsBitcoinExpense()`, `.AsBitcoinToBitcoinTransfer()`
- `FiatAccountBuilder` — `.AnAccount()`, `.WithName()`, `.WithFiatCurrency()`, `.WithValue()`
- `BtcAccountBuilder` — `.AnAccount()`, `.WithName()`, `.WithValue()`
- `CategoryBuilder` — `.ACategory()`, `.WithName()`
- `FixedExpenseBuilder` — `.AFixedExpense()`, `.AFixedExpenseWithAccount()`, `.AFixedExpenseWithCurrency()`
- `AvgPriceLineBuilder` — `.ABuyLine()`, `.ASellLine()`, `.ASetupLine()`
- `AvgPriceProfileBuilder` — `.AProfile()`, `.ABrazilianRuleProfile()`, `.AFifoProfile()`
- `GoalBuilder` — `.AGoal()`, `.AStackBitcoinGoal()`, `.AMonthlyGoal()`
- `AssetBuilder` — `.AnAsset()`, `.WithName()`, `.WithBasicDetails()`, `.WithGroupId()`
- `AssetGroupBuilder` — `.AGroup()`, `.WithName()`
- `AccountGroupBuilder` — `.AGroup()`, `.WithName()`
- `FakeClock` — deterministic time for tests

**IdGenerator Setup:**
```csharp
[OneTimeSetUp]
public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());
```
This is done automatically in `DatabaseTest` and `IntegrationTest` base classes.

## Coverage

**Requirements:** None enforced

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Types

**Unit Tests:**
- Value object tests (e.g., `BtcValueTests`, `FiatValueTests`)
- Pure domain logic tests
- No database, no DI container

**Database Tests (lightweight integration):**
- Inherit from `DatabaseTest` base class
- Uses real LiteDB in-memory databases (`MemoryStream` backed)
- Repositories instantiated directly with mocked `IDomainEventPublisher`
- Domain events are swallowed (publisher is a mock)
- Good for testing repository queries and simple handlers

**Integration Tests:**
- Inherit from `IntegrationTest` base class
- Full DI container via `Microsoft.Extensions.DependencyInjection`
- All real services registered via `.AddValtCore()`, `.AddValtInfrastructure()`, `.AddValtUI()`
- Can replace services via `ReplaceService<T>()`
- Good for testing cross-cutting concerns and full handler pipelines

**Architecture Tests:**
- NetArchTest.Rules for enforcing conventions
- Layer dependency rules (`Core` should not reference `App`, `Infra`, `UI`)
- Naming convention rules (commands end with `Command`, handlers end with `Handler`)
- Visibility rules (handlers should be `internal`)
- Domain model placement rules (entities only in `Core`, DTOs not in `Core`)

**E2E Tests:**
- Not used

## Common Patterns

**Async Testing:**
```csharp
[Test]
public async Task HandleAsync_WithValidCommand_ReturnsSuccess()
{
    var result = await _handler.HandleAsync(command);
    Assert.That(result.IsSuccess, Is.True);
}
```

**Error Testing:**
```csharp
[Test]
public async Task HandleAsync_WithInvalidPeriod_ReturnsValidationError()
{
    var result = await _handler.HandleAsync(command);
    Assert.Multiple(() =>
    {
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
    });
}
```

**State Verification:**
```csharp
var updatedAsset = await _assetRepository.GetByIdAsync(asset.Id);
Assert.That(updatedAsset!.GroupId, Is.EqualTo(group.Id));
```

**Property Changed Testing:**
```csharp
var propertiesChanged = new List<string>();
model.PropertyChanged += (sender, args) => propertiesChanged.Add(args.PropertyName!);
// ... trigger change
Assert.That(propertiesChanged, Contains.Item(nameof(model.FromAccountIsBtc)));
```

**ViewModel Testing with Mocked Dispatchers:**
```csharp
[SetUp]
public new void SetUp()
{
    _commandDispatcher = Substitute.For<ICommandDispatcher>();
    _queryDispatcher = Substitute.For<IQueryDispatcher>();
    _queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
        .Returns(callInfo => Task.FromResult<IReadOnlyList<AccountDTO>>(_accounts.ToList()));
}
```

## Base Classes

**`DatabaseTest`** (`tests/Valt.Tests/DatabaseTest.cs`):
- Creates in-memory `LocalDatabase` and `PriceDatabase` (`MemoryStream` backed)
- Instantiates all repositories directly with mocked `IDomainEventPublisher`
- Refreshes repository instances on each `[SetUp]` to isolate tests
- Provides `SeedDatabase()` override hook
- ~188 test files use this pattern

**`IntegrationTest`** (`tests/Valt.Tests/IntegrationTest.cs`):
- Builds full `IServiceProvider` with all registrations
- Mocks `ILocalStorageService` for UI dependencies
- Provides `ReplaceService<T>()` for swapping implementations
- Provides `RebuildServiceProvider()` for re-creating the container
- Used for end-to-end handler testing with real dependencies

## Test Statistics

- **Test project:** `tests/Valt.Tests/Valt.Tests.csproj`
- **Test files:** ~188 `.cs` files
- **Test methods:** ~1,500+ (detected via `dotnet test --list-tests`)
- **Test project references:** All 4 source projects (`Valt.Core`, `Valt.App`, `Valt.Infra`, `Valt.UI`)

---

*Testing analysis: 2026-05-27*
