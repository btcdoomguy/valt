using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.CreateDatabase;
using Valt.UI.Views.Main.Modals.InputPassword;

namespace Valt.UI.Views.Main.Modals.InitialSelection;

public partial class InitialSelectionViewModel : ValtModalViewModel
{
    private readonly IModalFactory? _modalFactory;
    private readonly ILocalStorageService? _localStorageService;

    #region Form Data

    [ObservableProperty] private AvaloniaList<string> _recentFiles = new();
    [ObservableProperty] private string _selectedFile = string.Empty;

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public InitialSelectionViewModel()
    {
        if (!Design.IsDesignMode) return;

        RecentFiles = new AvaloniaList<string>()
        {
            @"C:\mydb\db1.valt"
        };
    }

    public InitialSelectionViewModel(IModalFactory modalFactory, ILocalStorageService localStorageService)
    {
        _modalFactory = modalFactory;
        _localStorageService = localStorageService;
    }
    
    [RelayCommand]
    private async Task CreateNew()
    {
        var thisWindow = GetWindow!();

        var createDatabaseModal =
            (CreateDatabaseView)await _modalFactory!.CreateAsync(ApplicationModalNames.CreateDatabase, thisWindow)!;

        var result = await createDatabaseModal.ShowDialog<CreateDatabaseViewModel.Response?>(thisWindow);

        if (result is null)
            return;

        await UpdateRecentFilesAsync(result.Path);

        CloseDialog?.Invoke(new Response()
        {
            File = result.Path,
            Password = result.Password,
            IsNew = true,
            InitialDataLanguage = result.Language,
            SelectedCurrencies = result.SelectedCurrencies
        });
    }

    [RelayCommand]
    private async Task OpenSelected()
    {
        if (string.IsNullOrEmpty(SelectedFile))
            return;

        var thisWindow = GetWindow!();

        if (!File.Exists(SelectedFile))
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, language.Error_FileNotFound, thisWindow);
            RecentFiles.Remove(SelectedFile);
            await _localStorageService!.ChangeRecentFilesAsync(RecentFiles);
            return;
        }

        var inputPasswordModal =
            (InputPasswordView)await _modalFactory!.CreateAsync(ApplicationModalNames.InputPassword, thisWindow)!;

        var inputPasswordResult = await inputPasswordModal.ShowDialog<InputPasswordViewModel.Response?>(thisWindow);

        if (inputPasswordResult?.Password is null)
            return;

        await UpdateRecentFilesAsync(SelectedFile);

        CloseDialog?.Invoke(new Response() { File = SelectedFile!, Password = inputPasswordResult.Password });
    }

    [RelayCommand]
    private async Task OpenExisting()
    {
        var thisWindow = GetWindow!();

        var selectionResult = await OwnerWindow!.StorageProvider.OpenFilePickerAsync(CreateFilePickerOpenOptions());

        var pathResponse = selectionResult.FirstOrDefault()?.Path.LocalPath;

        if (pathResponse is null)
        {
            return;
        }

        var inputPasswordModal =
            (InputPasswordView)await _modalFactory!.CreateAsync(ApplicationModalNames.InputPassword, thisWindow)!;

        var inputPasswordResult = await inputPasswordModal.ShowDialog<InputPasswordViewModel.Response?>(thisWindow);

        if (inputPasswordResult?.Password is null)
            return;

        await UpdateRecentFilesAsync(pathResponse);

        CloseDialog?.Invoke(new Response() { File = pathResponse!, Password = inputPasswordResult.Password });
    }

    [RelayCommand]
    private Task CloseEmpty()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoadRecentFiles()
    {
        var recentFiles = _localStorageService!.LoadRecentFiles();
        RecentFiles.Clear();
        RecentFiles.AddRange(recentFiles);
        return Task.CompletedTask;
    }

    private async Task UpdateRecentFilesAsync(string lastPath)
    {
        RecentFiles.Remove(lastPath);
        RecentFiles.Insert(0, lastPath);
        SelectedFile = lastPath;
        await _localStorageService!.ChangeRecentFilesAsync(RecentFiles);
    }

    private static FilePickerOpenOptions CreateFilePickerOpenOptions()
    {
        return new FilePickerOpenOptions()
        {
            Title = "Open database",
            SuggestedFileName = "",
            FileTypeFilter =
            [
                new FilePickerFileType("VALT Files")
                {
                    Patterns = ["*.valt"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            ]
        };
    }

    public record Response
    {
        public string File { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool IsNew { get; init; }
        public string? InitialDataLanguage { get; init; }
        public List<string>? SelectedCurrencies { get; init; }
    }
}