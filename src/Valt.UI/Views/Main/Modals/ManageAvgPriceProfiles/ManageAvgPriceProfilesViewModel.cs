using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Exceptions;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Kernel.Extensions;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.IconSelector;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles.Models;
using Valt.UI.Views.Main.Modals.ManageCategories.Models;

namespace Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;

public partial class ManageAvgPriceProfilesViewModel : ValtModalValidatorViewModel
{
    private readonly IModalFactory _modalFactory;
    private readonly IAvgPriceQueries _avgPriceQueries;
    private readonly IAvgPriceRepository _avgPriceRepository;

    [ObservableProperty] private string? _id;
    [ObservableProperty] private string _name;

    public bool IsBitcoin => AssetName == "BTC" && Precision == 8;

    public bool IsCustomAsset => !IsBitcoin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBitcoin))]
    [NotifyPropertyChangedFor(nameof(IsCustomAsset))]
    private string _assetName;

    [ObservableProperty] [Range(0, 15, ErrorMessage = "Precision must be between 0 and 15")]
    [NotifyPropertyChangedFor(nameof(IsBitcoin))]
    [NotifyPropertyChangedFor(nameof(IsCustomAsset))]
    private int _precision;

    [ObservableProperty] private bool _visible;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(SymbolOnRight))] [NotifyPropertyChangedFor(nameof(Symbol))]
    private string _currency;

    public static List<string> AvailableCurrencies => FiatCurrency.GetAll().Select(x => x.Code).ToList();
    public bool SymbolOnRight => FiatCurrency.GetFromCode(Currency).SymbolOnRight;
    public string Symbol => FiatCurrency.GetFromCode(Currency).Symbol;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IconUiWrapper))] [Required(ErrorMessage = "Icon is required")]
    private Icon _icon = Core.Common.Icon.Empty;

    public IconUIWrapper IconUiWrapper => new(Icon);

    public AvaloniaList<AveragePriceProfileItem> AveragePriceProfiles { get; set; } = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdding))]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(EditFields))]
    private AveragePriceProfileItem? _selectedAveragePriceProfile;

    public AvaloniaList<EnumItem> AvailableStrategies { get; set; }

    [ObservableProperty] private EnumItem _selectedStrategy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdding))]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(EditFields))]
    private bool _editMode;

    public bool IsViewing => SelectedAveragePriceProfile is not null && !EditMode;

    public bool IsAdding => SelectedAveragePriceProfile is null;

    public bool IsEditing => SelectedAveragePriceProfile is not null && EditMode;

    public bool EditFields => IsAdding || IsEditing;

    public ManageAvgPriceProfilesViewModel()
    {
        if (!Design.IsDesignMode)
            return;

        AveragePriceProfiles =
        [
            new AveragePriceProfileItem(new AvgPriceProfileId(), "Test 1"),
            new AveragePriceProfileItem(new AvgPriceProfileId(), "Test 2"),
            new AveragePriceProfileItem(new AvgPriceProfileId(), "Test 3"),
        ];

        AvailableStrategies = new AvaloniaList<EnumItem>(EnumExtensions.ToList<AvgPriceCalculationMethod>());
    }

    public ManageAvgPriceProfilesViewModel(IModalFactory modalFactory,
        IAvgPriceQueries avgPriceQueries,
        IAvgPriceRepository avgPriceRepository)
    {
        _modalFactory = modalFactory;
        _avgPriceQueries = avgPriceQueries;
        _avgPriceRepository = avgPriceRepository;

        AvailableStrategies = new AvaloniaList<EnumItem>(EnumExtensions.ToList<AvgPriceCalculationMethod>());
    }

    public void Initialize()
    {
        _ = FetchAvgPriceProfiles();
    }

    private async Task FetchAvgPriceProfiles()
    {
        var profile = await _avgPriceQueries.GetProfilesAsync(false);

        AveragePriceProfiles.Clear();
        AveragePriceProfiles.AddRange(profile.Select(x => new AveragePriceProfileItem(x.Id, x.Name)));
    }
    
    [RelayCommand]
    private void SetBitcoin()
    {
        AssetName = "BTC";
        Precision = 8;
    }

    [RelayCommand]
    private void SetCustomAsset()
    {
        AssetName = string.Empty;
        Precision = 2;
    }

    [RelayCommand]
    private async Task IconSelectorOpen()
    {
        var modal =
            (IconSelectorView)await _modalFactory!.CreateAsync(ApplicationModalNames.IconSelector, GetWindow!(),
                Icon.ToString())!;

        var response = await modal.ShowDialog<IconSelectorViewModel.Response?>(GetWindow!());

        if (response is null)
            return;

        if (response.Icon is not null)
        {
            Icon = new Icon(response.Icon.Source, response.Icon.Name, response.Icon.Unicode,
                System.Drawing.Color.FromArgb(response.Color.A, response.Color.R, response.Color.G, response.Color.B));
        }
        else
        {
            Icon = Icon.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveChanges()
    {
        ValidateAllProperties();
        
        if (!HasErrors)
        {
            if (Id == null)
            {
                var profile = AvgPriceProfile.New(Name, new AvgPriceAsset(AssetName, Precision), Visible, Icon, FiatCurrency.GetFromCode(Currency), (AvgPriceCalculationMethod) SelectedStrategy.Value);

                await _avgPriceRepository!.SaveAvgPriceProfileAsync(profile);
            }
            else
            {
                var id = new AvgPriceProfileId(Id);
                var name = AvgPriceProfileName.New(Name);
                var strategy = (AvgPriceCalculationMethod)SelectedStrategy.Value;

                var profile = await _avgPriceRepository!.GetAvgPriceProfileByIdAsync(id);

                if (profile is null)
                    throw new EntityNotFoundException(nameof(AvgPriceProfile), id);

                profile.Rename(name);
                profile.ChangeAsset(AssetName, Precision);
                profile.ChangeIcon(Icon);
                profile.ChangeCalculationMethod(strategy);
                profile.ChangeVisibility(Visible);

                await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);
            }

            ClearSelection();
            await FetchAvgPriceProfiles();
        }
    }
    
    [RelayCommand]
    private Task AddNew()
    {
        ClearSelection();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedAveragePriceProfile is not null)
        {
            EditMode = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        EditMode = false;
    }


    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedAveragePriceProfile is null)
            return;

        //
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    partial void OnSelectedAveragePriceProfileChanged(AveragePriceProfileItem? value)
    {
        _ = LoadProfileAsync(value?.Id);
    }

    private async Task LoadProfileAsync(string? id)
    {
        if (id is null)
        {
            ClearSelection();
        }
        else
        {
            var profile = await _avgPriceQueries.GetProfileAsync(id);

            Id = profile.Id;
            AssetName = profile.AssetName;
            Precision = profile.Precision;
            Currency = profile.CurrencyCode;
            Icon = Icon.RestoreFromId(profile.Icon);
            SelectedStrategy = AvailableStrategies.First(x => x.Value == profile.AvgPriceCalculationMethodId);
            Name = profile.Name;
            Visible = profile.Visible;
        }
    }
    
    private void ClearSelection()
    {
        SelectedAveragePriceProfile = null;
        Id = null;
        AssetName = "BTC";
        Precision = 8;
        Currency = AvailableCurrencies.First();
        Icon = Icon.Empty;
        SelectedStrategy = AvailableStrategies.First();
        Name = "";
        Visible = true;
        EditMode = false;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        await LoadProfileAsync(request.Id);
    }

    public record Request(string? Id = null);

    public record Response(bool Ok = true);
}