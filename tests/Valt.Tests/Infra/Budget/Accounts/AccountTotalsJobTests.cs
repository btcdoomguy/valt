using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts.Services;

namespace Valt.Tests.Infrastructure.Budget.Accounts;

[TestFixture]
public class AccountTotalsJobTests
{
    private AccountTotalsJob _job = null!;
    private IClock _clock = null!;
    private IAccountCacheService _accountCacheService = null!;
    private IPriceDatabase _priceDatabase = null!;
    private ILocalDatabase _localDatabase = null!;
    private ILogger<AccountTotalsJob> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = Substitute.For<IClock>();
        _accountCacheService = Substitute.For<IAccountCacheService>();
        _priceDatabase = Substitute.For<IPriceDatabase>();
        _localDatabase = Substitute.For<ILocalDatabase>();
        _logger = Substitute.For<ILogger<AccountTotalsJob>>();

        _priceDatabase.HasDatabaseOpen.Returns(true);
        _localDatabase.HasDatabaseOpen.Returns(true);

        _job = new AccountTotalsJob(
            _clock,
            _accountCacheService,
            _priceDatabase,
            _localDatabase,
            _logger);
    }

    [TearDown]
    public void TearDown()
    {
        (_clock as IDisposable)?.Dispose();
        (_accountCacheService as IDisposable)?.Dispose();
        (_priceDatabase as IDisposable)?.Dispose();
        (_localDatabase as IDisposable)?.Dispose();
        (_logger as IDisposable)?.Dispose();
    }

    [Test]
    public async Task RunAsync_When_Date_Changes_Should_Refresh_Totals()
    {
        var firstDay = new DateOnly(2024, 1, 15);
        var secondDay = new DateOnly(2024, 1, 16);
        _clock.GetCurrentLocalDate().Returns(firstDay, secondDay);

        await _job.RunAsync(CancellationToken.None);
        await _job.RunAsync(CancellationToken.None);

        await _accountCacheService.Received(1).RefreshCurrentTotalsAsync(secondDay);
    }

    [Test]
    public async Task RunAsync_When_Date_Does_Not_Change_Should_Not_Refresh_Totals()
    {
        var today = new DateOnly(2024, 1, 15);
        _clock.GetCurrentLocalDate().Returns(today, today);

        await _job.RunAsync(CancellationToken.None);
        await _job.RunAsync(CancellationToken.None);

        await _accountCacheService.Received(1).RefreshCurrentTotalsAsync(today);
    }

    [Test]
    public async Task RunAsync_When_Database_Not_Open_Should_Not_Refresh()
    {
        _localDatabase.HasDatabaseOpen.Returns(false);

        await _job.RunAsync(CancellationToken.None);

        await _accountCacheService.DidNotReceive().RefreshCurrentTotalsAsync(Arg.Any<DateOnly>());
    }

    [Test]
    public void Interval_Should_Be_120_Seconds()
    {
        Assert.That(_job.Interval, Is.EqualTo(TimeSpan.FromSeconds(120)));
    }
}
