using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.CreateAssetGroup;
using Valt.App.Modules.Assets.Commands.EditAssetGroup;
using Valt.App.Modules.Assets.Queries.GetAssetGroups;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAssetGroup;

public partial class ManageAssetGroupViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;

    private string? _assetGroupId;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Inform a valid group name.")]
    [MaxLength(50, ErrorMessage = "Group name must be 50 characters or less.")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(200, ErrorMessage = "Description must be 200 characters or less.")]
    private string _description = string.Empty;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAssetGroupViewModel()
    {
    }

    public ManageAssetGroupViewModel(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string groupId)
        {
            var groups = await _queryDispatcher!.DispatchAsync(new GetAssetGroupsQuery());
            var group = groups.FirstOrDefault(g => g.Id == groupId);

            if (group is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AssetGroupNotFound, GetWindow!());
                return;
            }

            _assetGroupId = group.Id;
            Name = group.Name;
            Description = group.Description;
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            if (_assetGroupId is null)
            {
                var result = await _commandDispatcher!.DispatchAsync(new CreateAssetGroupCommand
                {
                    Name = Name,
                    Description = Description
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, result.Error!.Message, GetWindow!());
                    return;
                }
            }
            else
            {
                var result = await _commandDispatcher!.DispatchAsync(new EditAssetGroupCommand
                {
                    GroupId = _assetGroupId,
                    Name = Name,
                    Description = Description
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, result.Error!.Message, GetWindow!());
                    return;
                }
            }

            CloseDialog?.Invoke(new Response(true));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response(bool Ok);
}
