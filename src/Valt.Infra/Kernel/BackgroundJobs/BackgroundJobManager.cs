using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Valt.Infra.Kernel.BackgroundJobs;

public readonly struct JobExecutionRequest
{
    public TaskCompletionSource<JobExecutionResult>? CompletionSource { get; init; }
}

public readonly struct JobExecutionResult
{
    public bool Success { get; init; }
    public Exception? Exception { get; init; }
}

public sealed class BackgroundJobManager : IAsyncDisposable
{
    private readonly Dictionary<IBackgroundJob, JobInfo> _jobInfos = new();
    private readonly Dictionary<BackgroundJobTypes, CancellationTokenSource> _ctsMap = new();

    public BackgroundJobManager(IEnumerable<IBackgroundJob> jobs)
    {
        foreach (var job in jobs)
        {
            _jobInfos[job] = new JobInfo(job);
        }
    }
    
    public IEnumerable<JobInfo> GetJobInfos()
    {
        return _jobInfos.Values;
    }

    public async Task StartAllJobsAsync(BackgroundJobTypes jobType, bool triggerInitialRun = true)
    {
        foreach (var jobInfo in _jobInfos.Where(jobInfo => jobInfo.Key.JobType == jobType))
        {
            await Task.Delay(100).ConfigureAwait(false);
            StartJob(jobInfo.Key, GetCancellationToken(jobType));
            if (triggerInitialRun)
                jobInfo.Value.RequestRun();
        }
    }

    private void StartJob(IBackgroundJob job, CancellationToken token)
    {
        if (_jobInfos.TryGetValue(job, out var jobInfo))
        {
            jobInfo.Start(token);
        }
    }
    
    public void TriggerJobManually(BackgroundJobSystemNames systemName)
    {
        var jobInfo = _jobInfos.Values.SingleOrDefault(x => x.Job.SystemName == systemName);
        jobInfo?.RequestRun();  // Fire-and-forget, non-blocking
    }

    public async Task TriggerJobAndWaitAsync(BackgroundJobSystemNames systemName)
    {
        var jobInfo = _jobInfos.Values.SingleOrDefault(x => x.Job.SystemName == systemName);
        if (jobInfo != null)
        {
            var result = await jobInfo.RequestRunAndWaitAsync(CancellationToken.None);
            if (!result.Success && result.Exception != null)
                throw result.Exception;
        }
    }

    public async Task StopAll()
    {
        foreach (var token in _ctsMap.Values)
            await token.CancelAsync();
        
        var tasks = _jobInfos.Values
            .Select(j => j.StopAsync());
        await Task.WhenAll(tasks);
    }

    private CancellationToken GetCancellationToken(BackgroundJobTypes jobType)
    {
        if (_ctsMap.TryGetValue(jobType, out var cts))
            return cts.Token;
        
        var newCts = new CancellationTokenSource();
        _ctsMap[jobType] = newCts;
        return newCts.Token;
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var token in _ctsMap.Values)
        {
            await token.CancelAsync();
            token.Dispose();
        }

        foreach (var jobInfo in _jobInfos.Values)
        {
            await jobInfo.DisposeAsync();
        }
    }

    public bool IsRunningTasksOf(BackgroundJobTypes jobType)
    {
        return _jobInfos.Any(x => x.Value.State == BackgroundJobState.Running && x.Key.JobType == jobType);
    }

    public bool HasErrors(BackgroundJobTypes jobType)
    {
        return _jobInfos.Any(x => x.Value.State == BackgroundJobState.Error && x.Key.JobType == jobType);
    }
}

public sealed class JobInfo : IStatusItem, IAsyncDisposable
{
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly IBackgroundJob _job;
    private readonly Channel<JobExecutionRequest> _channel;
    private readonly JobLogPool _logPool = new();
    private Task? _consumerTask;
    private Timer? _periodicTimer;
    private CancellationTokenSource? _cts;
    private BackgroundJobState _state = BackgroundJobState.Stopped;

    public JobInfo(IBackgroundJob job)
    {
        _job = job;
        _channel = Channel.CreateBounded<JobExecutionRequest>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public IBackgroundJob Job => _job;
    public JobLogPool LogPool => _logPool;

    // IStatusItem implementation
    public string Name => _job.Name;
    public string StateDisplay => _state.ToString();

    public BackgroundJobState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateDisplay));
            }
        }
    }

    public string? ErrorMessage { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Start(CancellationToken token)
    {
        if (_consumerTask != null) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        // Start consumer loop
        _consumerTask = Task.Run(() => ConsumeLoopAsync(_cts.Token), _cts.Token);

        // Timer writes to channel instead of executing directly
        _periodicTimer = new Timer(_ => RequestRun(), null, _job.Interval, _job.Interval);
    }

    public void RequestRun()
    {
        _channel.Writer.TryWrite(new JobExecutionRequest());
    }

    public async Task<JobExecutionResult> RequestRunAndWaitAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<JobExecutionResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // WriteAsync blocks if channel full (waits for current pending to be consumed)
        await _channel.Writer.WriteAsync(
            new JobExecutionRequest { CompletionSource = tcs }, ct);

        return await tcs.Task;
    }

    private async Task ConsumeLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(ct))
            {
                await ExecuteJobAsync(request, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        // Note: State is set by StopAsync after awaiting this task to avoid
        // deadlock when PropertyChanged tries to marshal to UI thread
    }

    private async Task ExecuteJobAsync(JobExecutionRequest request, CancellationToken ct)
    {
        var result = new JobExecutionResult();
        try
        {
            if (State == BackgroundJobState.Stopped)
                await _job.StartAsync(ct);

            State = BackgroundJobState.Running;
            ErrorMessage = null;
            await RunWithRetryAsync(ct);
            State = BackgroundJobState.Ok;
            result = new JobExecutionResult { Success = true };
        }
        catch (OperationCanceledException)
        {
            State = BackgroundJobState.Stopped;
            result = new JobExecutionResult { Success = false };
        }
        catch (Exception ex)
        {
            State = BackgroundJobState.Error;
            ErrorMessage = ex.Message;
            result = new JobExecutionResult { Success = false, Exception = ex };
        }
        finally
        {
            request.CompletionSource?.TrySetResult(result);
        }
    }

    private async Task RunWithRetryAsync(CancellationToken token)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                await _job.RunAsync(token);
                return; // Success, exit retry loop
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < MaxRetryAttempts)
                {
                    await Task.Delay(RetryDelay, token);
                }
            }
        }

        // All retries exhausted, throw the last exception
        throw lastException!;
    }

    public async Task StopAsync()
    {
        if (_consumerTask == null) return;

        _channel.Writer.TryComplete();
        if (_cts != null) await _cts.CancelAsync();

        try
        {
            // Use timeout to prevent indefinite blocking if job doesn't respect cancellation
            await _consumerTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (TimeoutException)
        {
            // Job didn't stop in time, proceed anyway
        }

        _periodicTimer?.Dispose();
        _periodicTimer = null;
        _consumerTask = null;
        State = BackgroundJobState.Stopped;
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        if (_cts != null) await _cts.CancelAsync();
        if (_consumerTask != null)
        {
            try
            {
                await _consumerTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
        }
        _periodicTimer?.Dispose();
        _cts?.Dispose();
    }
}