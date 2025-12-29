using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.AvgPriceLineEditor;

public partial class AvgPriceLineEditorViewModel : ValtModalValidatorViewModel
{
    private readonly IAvgPriceRepository _avgPriceRepository;

    [ObservableProperty] private string _windowTitle = language.AvgPriceLineEditor_AddTitle;

    #region Profile Context

    [ObservableProperty] private string _profileId = string.Empty;
    [ObservableProperty] private string _assetName = "BTC";
    [ObservableProperty] private int _assetPrecision = 8;
    [ObservableProperty] private string _currencySymbol = "$";
    [ObservableProperty] private bool _currencySymbolOnRight;

    #endregion

    #region Form Data

    [ObservableProperty] private string? _lineId;

    [Required(ErrorMessage = "Date is required")]
    [ObservableProperty]
    private DateTimeOffset _date = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuy))]
    [NotifyPropertyChangedFor(nameof(IsSell))]
    [NotifyPropertyChangedFor(nameof(IsSetup))]
    [NotifyPropertyChangedFor(nameof(UnitPriceLabel))]
    private AvgPriceLineTypes _lineType = AvgPriceLineTypes.Buy;

    public bool IsBuy => LineType == AvgPriceLineTypes.Buy;
    public bool IsSell => LineType == AvgPriceLineTypes.Sell;
    public bool IsSetup => LineType == AvgPriceLineTypes.Setup;

    public string UnitPriceLabel => IsSetup
        ? language.AvgPriceLineEditor_AvgCost
        : language.AvgPriceLineEditor_UnitPrice;

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    [ObservableProperty]
    private decimal _quantity;

    [Required(ErrorMessage = "Unit price is required")]
    [ObservableProperty]
    private FiatValue? _unitPrice = FiatValue.Empty;

    [ObservableProperty] private string _comment = string.Empty;

    [ObservableProperty] private int _displayOrder = 1;

    #endregion

    #region Edit Mode State

    public bool IsEditMode => LineId is not null;
    public bool IsInsertMode => LineId is null;

    #endregion

    public AvgPriceLineEditorViewModel()
    {
        if (!Design.IsDesignMode) return;
    }

    public AvgPriceLineEditorViewModel(IAvgPriceRepository avgPriceRepository)
    {
        _avgPriceRepository = avgPriceRepository;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        ProfileId = request.ProfileId;
        AssetName = request.AssetName;
        AssetPrecision = request.AssetPrecision;
        CurrencySymbol = request.CurrencySymbol;
        CurrencySymbolOnRight = request.CurrencySymbolOnRight;

        if (request.ExistingLine is not null)
        {
            WindowTitle = language.AvgPriceLineEditor_EditTitle;

            LineId = request.ExistingLine.Id;
            Date = request.ExistingLine.Date.ToDateTime(TimeOnly.MinValue);
            LineType = (AvgPriceLineTypes)request.ExistingLine.AvgPriceLineTypeId;
            Quantity = request.ExistingLine.Quantity;
            UnitPrice = FiatValue.New(request.ExistingLine.UnitPrice);
            Comment = request.ExistingLine.Comment;
            DisplayOrder = request.ExistingLine.DisplayOrder;
        }
    }

    [RelayCommand]
    private void SetBuy() => LineType = AvgPriceLineTypes.Buy;

    [RelayCommand]
    private void SetSell() => LineType = AvgPriceLineTypes.Sell;

    [RelayCommand]
    private void SetSetup() => LineType = AvgPriceLineTypes.Setup;

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        try
        {
            var profileId = new AvgPriceProfileId(ProfileId);
            var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(profileId);

            if (profile is null)
            {
                await MessageBoxHelper.ShowErrorAsync("Error", "Profile not found", GetWindow!());
                return;
            }

            var date = DateOnly.FromDateTime(Date.Date);

            if (IsEditMode)
            {
                // Find and remove the existing line
                var existingLine = FindLineById(profile, LineId!);
                if (existingLine is not null)
                {
                    profile.RemoveLine(existingLine);
                }
            }

            // Add the new/updated line
            profile.AddLine(date, DisplayOrder, LineType, Quantity, UnitPrice!, Comment);

            await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

            CloseDialog?.Invoke(new Response(true));
        }
        catch (Exception e)
        {
            await MessageBoxHelper.ShowErrorAsync("Error", e.Message, GetWindow!());
        }
    }

    private static AvgPriceLine? FindLineById(AvgPriceProfile profile, string lineId)
    {
        foreach (var line in profile.AvgPriceLines)
        {
            if (line.Id.Value == lineId)
                return line;
        }

        return null;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }

    public record Request
    {
        public required string ProfileId { get; init; }
        public required string AssetName { get; init; }
        public required int AssetPrecision { get; init; }
        public required string CurrencySymbol { get; init; }
        public required bool CurrencySymbolOnRight { get; init; }
        public AvgPriceLineDTO? ExistingLine { get; init; }
    }

    public record Response(bool Ok);
}
