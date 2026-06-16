using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAsset;
using Valt.App.Modules.Assets.Queries.GetLatestLoanState;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.UpdateLoanState;

public partial class UpdateLoanStateViewModel : ValtModalValidatorViewModel
{
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly ICommandDispatcher? _commandDispatcher;

    [ObservableProperty] private string _windowTitle = language.UpdateLoanState_Title;

    [ObservableProperty] private string _assetId = string.Empty;
    [ObservableProperty] private string _assetName = string.Empty;
    [ObservableProperty] private string _currencyCode = "USD";
    [ObservableProperty] private string _platformName = string.Empty;
    [ObservableProperty] private string _currencySymbol = "$";
    [ObservableProperty] private bool _symbolOnRight;

    [Required(ErrorMessage = "Effective date is required")]
    [ObservableProperty] private DateTime? _effectiveDate = DateTime.Today;

    [Required(ErrorMessage = "Current total debt is required")]
    [ObservableProperty] private FiatValue _currentTotalDebt = FiatValue.Empty;

    [Required(ErrorMessage = "Collateral is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Collateral must be greater than zero")]
    [ObservableProperty] private long _collateralSats;

    [Required(ErrorMessage = "APR is required")]
    [Range(0, double.MaxValue, ErrorMessage = "APR cannot be negative")]
    [ObservableProperty] private decimal _aprPercentage;

    [Required(ErrorMessage = "Fees are required")]
    [ObservableProperty] private FiatValue _fees = FiatValue.Empty;

    [ObservableProperty] private string _note = string.Empty;

    // Read-only context
    [ObservableProperty] private decimal _loanAmount;
    [ObservableProperty] private decimal _initialLtv;
    [ObservableProperty] private decimal _marginCallLtv;
    [ObservableProperty] private decimal _liquidationLtv;
    [ObservableProperty] private DateTime? _loanStartDate;
    [ObservableProperty] private DateTime? _repaymentDate;

    public string LoanAmountFormatted => CurrencyDisplay.FormatFiat(LoanAmount, CurrencyCode);
    public string InitialLtvFormatted => $"{InitialLtv:N2}%";
    public string MarginCallLtvFormatted => $"{MarginCallLtv:N2}%";
    public string LiquidationLtvFormatted => $"{LiquidationLtv:N2}%";
    public string LoanStartDateFormatted => LoanStartDate.HasValue ? LoanStartDate.Value.ToShortDateString() : "-";
    public string RepaymentDateFormatted => RepaymentDate.HasValue ? RepaymentDate.Value.ToShortDateString() : language.ManageAsset_IndefinitePeriod;

    public UpdateLoanStateViewModel()
    {
        if (!Design.IsDesignMode) return;
    }

    public UpdateLoanStateViewModel(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return;

        AssetId = request.AssetId;

        var latest = await _queryDispatcher!.DispatchAsync(new GetLatestLoanStateQuery
        {
            AssetId = AssetId
        });

        if (latest is not null)
        {
            AssetName = latest.AssetName;
            CurrencyCode = latest.CurrencyCode;
            PlatformName = latest.PlatformName;

            var currency = FiatCurrency.GetFromCode(CurrencyCode);
            CurrencySymbol = currency.Symbol;
            SymbolOnRight = currency.SymbolOnRight;

            EffectiveDate = DateTime.Today;
            CurrentTotalDebt = FiatValue.New(latest.CurrentTotalDebt);
            CollateralSats = latest.CollateralSats;
            AprPercentage = latest.Apr * 100m;
            Fees = FiatValue.New(latest.Fees);
            Note = latest.Note ?? string.Empty;

            LoanAmount = latest.LoanAmount;
            InitialLtv = latest.InitialLtv;
            MarginCallLtv = latest.MarginCallLtv;
            LiquidationLtv = latest.LiquidationLtv;
            LoanStartDate = latest.LoanStartDate.ToDateTime(TimeOnly.MinValue);
            RepaymentDate = latest.RepaymentDate?.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            var asset = await _queryDispatcher.DispatchAsync(new GetAssetQuery { AssetId = AssetId });
            if (asset is not null)
            {
                AssetName = asset.Name;
                CurrencyCode = asset.CurrencyCode;
                PlatformName = asset.PlatformName ?? string.Empty;

                var currency = FiatCurrency.GetFromCode(CurrencyCode);
                CurrencySymbol = currency.Symbol;
                SymbolOnRight = currency.SymbolOnRight;

                EffectiveDate = DateTime.Today;
                CurrentTotalDebt = FiatValue.New(asset.TotalDebt ?? asset.LoanAmount ?? 0m);
                CollateralSats = asset.CollateralSats ?? 0L;
                AprPercentage = (asset.Apr ?? 0m) * 100m;
                Fees = FiatValue.New(asset.Fees ?? 0m);
                Note = string.Empty;

                LoanAmount = asset.LoanAmount ?? 0m;
                InitialLtv = asset.InitialLtv ?? 0m;
                MarginCallLtv = asset.MarginCallLtv ?? 0m;
                LiquidationLtv = asset.LiquidationLtv ?? 0m;
                LoanStartDate = asset.LoanStartDate?.ToDateTime(TimeOnly.MinValue);
                RepaymentDate = asset.RepaymentDate?.ToDateTime(TimeOnly.MinValue);
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        var result = await _commandDispatcher!.DispatchAsync(new AddLoanStateUpdateCommand
        {
            AssetId = AssetId,
            EffectiveDate = DateOnly.FromDateTime(EffectiveDate!.Value),
            CurrentTotalDebt = CurrentTotalDebt.Value,
            CollateralSats = CollateralSats,
            Apr = AprPercentage / 100m,
            Fees = Fees.Value,
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
            return;
        }

        CloseDialog?.Invoke(new Response(true));
    }

    [RelayCommand]
    private void Cancel() => CloseWindow?.Invoke();

    [RelayCommand]
    private void Close() => CloseWindow?.Invoke();

    public record Request
    {
        public required string AssetId { get; init; }
    }

    public record Response(bool Ok);
}
