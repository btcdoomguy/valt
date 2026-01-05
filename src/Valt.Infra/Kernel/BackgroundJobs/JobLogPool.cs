namespace Valt.Infra.Kernel.BackgroundJobs;

public sealed class JobLogPool
{
    private const int MaxLines = 1000;
    private readonly Queue<JobLogEntry> _entries = new();
    private readonly object _lock = new();

    public void AddEntry(JobLogLevel level, string message)
    {
        var entry = new JobLogEntry(DateTime.Now, level, message);

        lock (_lock)
        {
            _entries.Enqueue(entry);

            while (_entries.Count > MaxLines)
            {
                _entries.Dequeue();
            }
        }
    }

    public IReadOnlyList<JobLogEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    public string GetAllText()
    {
        lock (_lock)
        {
            return string.Join(Environment.NewLine, _entries.Select(e => e.ToString()));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}

public enum JobLogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public record JobLogEntry(DateTime Timestamp, JobLogLevel Level, string Message)
{
    public override string ToString() => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
}
