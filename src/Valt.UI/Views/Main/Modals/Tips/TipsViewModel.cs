using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.LocalStorage;

namespace Valt.UI.Views.Main.Modals.Tips;

public partial class TipsViewModel : ValtModalViewModel
{
    private readonly ILocalStorageService _localStorageService;
    private List<string> _tips = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTipText))]
    [NotifyPropertyChangedFor(nameof(Counter))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentIndex;

    [ObservableProperty]
    private bool _dontShowOnStartup;

    public string CurrentTipText => _tips.Count > 0 ? _tips[CurrentIndex] : string.Empty;

    public string Counter => string.Format(language.Tips_Counter, CurrentIndex + 1, _tips.Count);

    /// <summary>Design-time constructor</summary>
    public TipsViewModel()
    {
        _localStorageService = null!;
        _tips = new List<string>
        {
            "Use **F2** to quickly add a new transaction from anywhere in the app."
        };
    }

    public TipsViewModel(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public override Task OnBindParameterAsync()
    {
        var resourceSet = language.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
        if (resourceSet != null)
        {
            _tips = resourceSet.Cast<DictionaryEntry>()
                .Where(e => e.Key is string key && key.StartsWith("Tips.Message."))
                .OrderBy(e => (string)e.Key)
                .Select(e => (string)e.Value!)
                .ToList();
        }

        CurrentIndex = _tips.Count > 1 ? Random.Shared.Next(0, _tips.Count) : 0;
        DontShowOnStartup = !_localStorageService.LoadShowTipsOnStartup();

        OnPropertyChanged(nameof(CurrentTipText));
        OnPropertyChanged(nameof(Counter));
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();

        return Task.CompletedTask;
    }

    partial void OnDontShowOnStartupChanged(bool value)
        => _ = _localStorageService?.ChangeShowTipsOnStartupAsync(!value);

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void Previous()
    {
        CurrentIndex--;
    }

    private bool CanGoPrevious() => CurrentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        CurrentIndex++;
    }

    private bool CanGoNext() => _tips.Count > 0 && CurrentIndex < _tips.Count - 1;

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }
}
