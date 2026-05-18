using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAssetGroup;

public partial class ManageAssetGroupViewModel : ValtModalValidatorViewModel
{
    private readonly IAssetGroupRepository? _assetGroupRepository;

    private AssetGroupId? _assetGroupId;

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

    public ManageAssetGroupViewModel(IAssetGroupRepository assetGroupRepository)
    {
        _assetGroupRepository = assetGroupRepository;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string groupId)
        {
            var group = await _assetGroupRepository!.GetByIdAsync(new AssetGroupId(groupId));

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
            AssetGroup group;

            if (_assetGroupId is null)
            {
                group = AssetGroup.New(AssetGroupName.New(Name), Description);
            }
            else
            {
                group = (await _assetGroupRepository!.GetByIdAsync(_assetGroupId))!;
                group.Rename(AssetGroupName.New(Name));
                group.ChangeDescription(Description);
            }

            await _assetGroupRepository!.SaveAsync(group);

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
