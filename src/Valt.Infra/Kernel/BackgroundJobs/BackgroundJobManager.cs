using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Valt.Infra.Kernel.BackgroundJobs;

public sealed class BackgroundJobManager : IDisposable
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

    public void StartAllJobs(BackgroundJobTypes jobType, bool triggerInitialRun = true)
    {
        foreach (var jobInfo in _jobInfos.Where(jobInfo => jobInfo.Key.JobType == jobType))
        {
            Task.Delay(100).Wait();
            StartJob(jobInfo.Key, GetCancellationToken(jobType));
            if (triggerInitialRun)
                jobInfo.Value.TriggerManually();
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
        var job = _jobInfos.Keys.SingleOrDefault(x => x.SystemName == systemName);
        if (job is not null && _jobInfos.TryGetValue(job, out var jobInfo))
        {
            jobInfo.TriggerManually();
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
    
    public void Dispose()
    {
        foreach (var token in _ctsMap.Values)
        {
            token.Cancel();
            token.Dispose();
        }

        foreach (var jobInfo in _jobInfos.Values)
        {
            jobInfo.Dispose();
        }
    }

    public bool IsRunningTasksOf(BackgroundJobTypes jobType)
    {
        return _jobInfos.Any(x => x.Value.State == BackgroundJobState.Running && x.Key.JobType == jobType);
    }
}

public sealed class JobInfo : INotifyPropertyChanged, IDisposable
{
    private readonly IBackgroundJob _job;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Timer? _timer;
    private CancellationToken _token;
    private BackgroundJobState _state = BackgroundJobState.Stopped;

    public JobInfo(IBackgroundJob job)
    {
        _job = job;
    }

    public IBackgroundJob Job => _job;
        
    public BackgroundJobState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
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
        if (_timer != null) return;
        _token = token;
        _timer = new Timer(async _ => await RunJobAsync(), null, _job.Interval, _job.Interval);
    }

    public void TriggerManually()
    {
        //reset timer to run now
        _timer?.Change(TimeSpan.Zero, _job.Interval);
    }

    private async Task RunJobAsync(CancellationToken token = default)
    {
        if (!await _semaphore.WaitAsync(0, token)) 
            return;
        
        if (State == BackgroundJobState.Stopped)
            await _job.StartAsync(_token);
        
        State = BackgroundJobState.Running;
        ErrorMessage = null;

        try
        {
            await _job.RunAsync(_token);
            State = BackgroundJobState.Ok;
        }
        catch (Exception ex)
        {
            State = ex is OperationCanceledException ? BackgroundJobState.Stopped : // Cancelled via token
                BackgroundJobState.Error; // Other errors
            
            if (State == BackgroundJobState.Error)
                ErrorMessage = ex.Message;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopAsync()
    {
        if (_timer == null) return;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        await _semaphore.WaitAsync(); // Wait for any running job to finish
        _semaphore.Release();
        await _timer.DisposeAsync();
        _timer = null;
        State = BackgroundJobState.Stopped;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _semaphore.Dispose();
    }
}