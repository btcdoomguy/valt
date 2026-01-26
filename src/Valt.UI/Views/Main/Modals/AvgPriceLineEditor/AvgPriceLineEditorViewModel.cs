using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Modules.AvgPrice.Commands.AddLine;
using Valt.App.Modules.AvgPrice.Commands.EditLine;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.AvgPriceLineEditor;

public partial class AvgPriceLineEditorViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;

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
    private DateTime? _date = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuy))]
    [NotifyPropertyChangedFor(nameof(IsSell))]
    [NotifyPropertyChangedFor(nameof(IsSetup))]
    [NotifyPropertyChangedFor(nameof(AmountLabel))]
    private AvgPriceLineTypes _lineType = AvgPriceLineTypes.Buy;

    public bool IsBuy => LineType == AvgPriceLineTypes.Buy;
    public bool IsSell => LineType == AvgPriceLineTypes.Sell;
    public bool IsSetup => LineType == AvgPriceLineTypes.Setup;

    public string AmountLabel => IsSetup
        ? language.AvgPriceLineEditor_AvgCost
        : language.AvgPriceLineEditor_Amount;

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    [ObservableProperty]
    private decimal _quantity;

    [Required(ErrorMessage = "Amount is required")]
    [ObservableProperty]
    private FiatValue? _amount = FiatValue.Empty;

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

    public AvgPriceLineEditorViewModel(ICommandDispatcher commandDispatcher)
    {
        _commandDispatcher = commandDispatcher;
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
            Amount = FiatValue.New(request.ExistingLine.Amount);
            Comment = request.ExistingLine.Comment;
            DisplayOrder = request.ExistingLine.DisplayOrder;
        }
        else
        {
            // Apply preset values if provided
            if (request.PresetDate.HasValue)
                Date = request.PresetDate.Value.ToDateTime(TimeOnly.MinValue);

            if (request.PresetLineType.HasValue)
                LineType = request.PresetLineType.Value;

            if (request.PresetQuantity.HasValue)
                Quantity = request.PresetQuantity.Value;

            if (request.PresetAmount.HasValue)
                Amount = FiatValue.New(request.PresetAmount.Value);
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

        var date = DateOnly.FromDateTime(Date!.Value);

        if (IsEditMode)
        {
            var result = await _commandDispatcher!.DispatchAsync(new EditLineCommand
            {
                ProfileId = ProfileId,
                LineId = LineId!,
                Date = date,
                LineTypeId = (int)LineType,
                Quantity = Quantity,
                Amount = Amount!.Value,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment
            });

            if (result.IsFailure)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                return;
            }
        }
        else
        {
            var result = await _commandDispatcher!.DispatchAsync(new AddLineCommand
            {
                ProfileId = ProfileId,
                Date = date,
                LineTypeId = (int)LineType,
                Quantity = Quantity,
                Amount = Amount!.Value,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment
            });

            if (result.IsFailure)
            {
                await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                return;
            }
        }

        CloseDialog?.Invoke(new Response(true));
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

        // Optional preset values from transaction
        public DateOnly? PresetDate { get; init; }
        public AvgPriceLineTypes? PresetLineType { get; init; }
        public decimal? PresetQuantity { get; init; }
        public decimal? PresetAmount { get; init; }
    }

    public record Response(bool Ok);
}
