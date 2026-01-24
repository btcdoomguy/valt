using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.StatusDisplay;

public partial class StatusDisplayViewModel : ValtModalViewModel
{
    private readonly BackgroundJobManager _jobManager = null!;
    public AvaloniaList<JobInfo> Jobs { get; set; } = new();

    [ObservableProperty]
    private JobInfo? _selectedJob;

    public StatusDisplayViewModel()
    {
        Jobs = [
            new JobInfo(new FooBackgroundJob("Job 1", "Job1")),
            new JobInfo(new FooBackgroundJob("Job 2", "Job2")),
            new JobInfo(new FooBackgroundJob("Job 3", "Job3")),
            new JobInfo(new FooBackgroundJob("Job 4", "Job4")),
        ];
    }

    public StatusDisplayViewModel(BackgroundJobManager jobManager)
    {
        _jobManager = jobManager;
        Jobs = new AvaloniaList<JobInfo>(_jobManager.GetJobInfos());
    }

    [RelayCommand]
    private void OpenJobLog(JobInfo? jobInfo)
    {
        if (jobInfo == null)
            return;

        var logViewer = new JobLogViewerView
        {
            DataContext = new JobLogViewerViewModel(jobInfo)
        };

#pragma warning disable CS8602 // Dereference of a possibly null reference
        logViewer.ShowDialog(GetWindow()!);
#pragma warning restore CS8602
    }

    public class FooBackgroundJob : IBackgroundJob
    {
        private readonly string _name;
        private readonly string _systemName;

        public FooBackgroundJob(string name, string systemName)
        {
            _name = name;
            _systemName = systemName;
        }
        
        public string Name => _name;
        public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.Foo;
        public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
        public TimeSpan Interval { get; }
        
        public Task StartAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
        
        public Task RunAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}