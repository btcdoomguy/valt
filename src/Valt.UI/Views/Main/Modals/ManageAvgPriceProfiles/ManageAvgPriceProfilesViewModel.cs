using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
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
    
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _assetName;
    [ObservableProperty] private int _precision;
    [ObservableProperty] private bool _visible;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SymbolOnRight))]
    [NotifyPropertyChangedFor(nameof(Symbol))]
    private string _currency;
    
    public static List<string> AvailableCurrencies => FiatCurrency.GetAll().Select(x => x.Code).ToList();
    public bool SymbolOnRight => FiatCurrency.GetFromCode(Currency).SymbolOnRight;
    public string Symbol => FiatCurrency.GetFromCode(Currency).Symbol;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IconUiWrapper))] [Required(ErrorMessage = "Icon is required")]
    private Icon _icon = Core.Common.Icon.Empty;
    public IconUIWrapper IconUiWrapper => new(Icon);
    
    public AvaloniaList<AveragePriceProfileItem> AveragePriceProfiles { get; set; } = new();
    public AveragePriceProfileItem? SelectedAveragePriceProfile { get; set; }
    
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
    }

    public ManageAvgPriceProfilesViewModel(IModalFactory modalFactory)
    {
        _modalFactory = modalFactory;
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
        //
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
}