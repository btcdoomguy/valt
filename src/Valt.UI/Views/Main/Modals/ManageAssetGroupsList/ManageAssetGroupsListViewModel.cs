using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.DeleteAssetGroup;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAssetGroups;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.ManageAssetGroup;

namespace Valt.UI.Views.Main.Modals.ManageAssetGroupsList;

public partial class ManageAssetGroupsListViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IModalFactory? _modalFactory;

    public AvaloniaList<AssetGroupListItemViewModel> AssetGroups { get; set; } = new();

    [ObservableProperty] private AssetGroupListItemViewModel? _selectedAssetGroup;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAssetGroupsListViewModel()
    {
        if (!Design.IsDesignMode) return;

        AssetGroups.Add(new AssetGroupListItemViewModel
        {
            Id = "1",
            Name = "Stocks",
            Description = "Stock investments"
        });
        AssetGroups.Add(new AssetGroupListItemViewModel
        {
            Id = "2",
            Name = "Real Estate",
            Description = "Property investments"
        });
    }

    public ManageAssetGroupsListViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        IModalFactory modalFactory)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _modalFactory = modalFactory;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchAssetGroupsAsync();
    }

    private async Task FetchAssetGroupsAsync()
    {
        if (_queryDispatcher is null)
            return;

        var groups = await _queryDispatcher.DispatchAsync(new GetAssetGroupsQuery());

        AssetGroups.Clear();
        foreach (var group in groups)
        {
            AssetGroups.Add(new AssetGroupListItemViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description
            });
        }
    }

    [RelayCommand]
    private async Task AddAssetGroup()
    {
        if (_modalFactory is null)
            return;

        var currentWindow = GetWindow!();
        var window = (ManageAssetGroupView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageAssetGroup, currentWindow, null)!;

        _ = await window.ShowDialogSafeAsync<ManageAssetGroupViewModel.Response?>(currentWindow);

        await FetchAssetGroupsAsync();
    }

    [RelayCommand]
    private async Task EditAssetGroup(AssetGroupListItemViewModel? item)
    {
        if (_modalFactory is null || item is null)
            return;

        var currentWindow = GetWindow!();
        var window = (ManageAssetGroupView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageAssetGroup, currentWindow, item.Id)!;

        _ = await window.ShowDialogSafeAsync<ManageAssetGroupViewModel.Response?>(currentWindow);

        await FetchAssetGroupsAsync();
    }

    [RelayCommand]
    private async Task DeleteAssetGroup(AssetGroupListItemViewModel? item)
    {
        if (_commandDispatcher is null || item is null)
            return;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Assets_DeleteGroup,
            language.Assets_DeleteGroupConfirmation,
            GetWindow!());

        if (!confirmed)
            return;

        var result = await _commandDispatcher.DispatchAsync(
            new DeleteAssetGroupCommand { GroupId = item.Id });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(
                language.Error, result.Error!.Message, GetWindow!());
            return;
        }

        await FetchAssetGroupsAsync();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record AssetGroupListItemViewModel
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
    }

    public record Response(bool Ok);
}
