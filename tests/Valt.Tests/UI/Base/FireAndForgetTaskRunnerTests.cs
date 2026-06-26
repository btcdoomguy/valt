using Microsoft.Extensions.Logging;
using Valt.UI.Base;

namespace Valt.Tests.UI.Base;

[TestFixture]
public class FireAndForgetTaskRunnerTests
{
    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    [Test]
    public async Task RunAsync_WhenTaskThrows_LogsError()
    {
        var logger = new TestLogger();
        var runner = new FireAndForgetTaskRunner();
        var exception = new InvalidOperationException("boom");

        runner.RunAsync(Task.FromException(exception), logger);

        await Task.Delay(50);

        Assert.That(logger.Entries, Has.Count.EqualTo(1));
        Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logger.Entries[0].Exception, Is.SameAs(exception));
        Assert.That(logger.Entries[0].Message, Does.Contain("failed"));
    }

    [Test]
    public async Task RunAsync_WhenTaskIsCancelled_LogsDebug()
    {
        var logger = new TestLogger();
        var runner = new FireAndForgetTaskRunner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        runner.RunAsync(Task.FromCanceled(cts.Token), logger);

        await Task.Delay(50);

        Assert.That(logger.Entries, Has.Count.EqualTo(1));
        Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Debug));
        Assert.That(logger.Entries[0].Message, Does.Contain("cancelled"));
    }
}
