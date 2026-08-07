using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.UI.Lang;
using Valt.UI.Base;
using Valt.UI.State;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying BTC-backed loan summary data on the reports dashboard.
/// </summary>
public partial class BtcLoansPanelViewModel : DashboardPanelViewModel, IBtcLoansPanelViewModel
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AccountsTotalState _accountsTotalState;
    private readonly RatesState _ratesState;
    private readonly CustomBtcPriceState _customBtcPriceState;
    private readonly CurrencySettings _currencySettings;
    private readonly ILogger<BtcLoansPanelViewModel> _logger;

    public BtcLoansPanelViewModel(
        IQueryDispatcher queryDispatcher,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CustomBtcPriceState customBtcPriceState,
        CurrencySettings currencySettings,
        ILogger<BtcLoansPanelViewModel> logger)
        : base(logger)
    {
        _queryDispatcher = queryDispatcher;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;
        _customBtcPriceState = customBtcPriceState;
        _currencySettings = currencySettings;
        _logger = logger;
    }

    public override async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;

            var fiatRates = _ratesState.FiatRates;
            var btcPrice = _ratesState.BitcoinPrice;
            var mainCurrency = _currencySettings.MainFiatCurrency;
            var stackSats = _accountsTotalState.CurrentWealth.WealthInSats;

            if (fiatRates is null || !btcPrice.HasValue)
            {
                IsVisible = false;
                IsLoading = false;
                return;
            }

            var dto = await _queryDispatcher.DispatchAsync(new GetBtcLoansDashboardQuery
            {
                MainCurrencyCode = mainCurrency,
                BtcPriceUsd = btcPrice,
                CustomBtcPriceUsd = _customBtcPriceState.CustomBtcPriceUsd,
                FiatRates = fiatRates,
                TotalBtcStackSats = stackSats
            });

            if (!dto.HasActiveLoans)
            {
                IsVisible = false;
                IsLoading = false;
                return;
            }

            var fiatCurrency = FiatCurrency.GetFromCode(mainCurrency);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_BtcLoans_ActiveLoans, dto.ActiveLoansCount.ToString(CultureInfo.InvariantCulture)),
                new(language.Reports_BtcLoans_TotalDebt, CurrencyDisplay.FormatFiat(dto.TotalDebtInMainCurrency, fiatCurrency.Code)),
                new(language.Reports_BtcLoans_TotalDebtBtc, CurrencyDisplay.FormatAsBitcoin(dto.TotalDebtInBtc) + " BTC"),
                new(language.Reports_BtcLoans_TotalBorrowed, CurrencyDisplay.FormatFiat(dto.TotalBorrowedInMainCurrency, fiatCurrency.Code)),
                new(language.Reports_BtcLoans_AvgLtv, dto.DebtWeightedAvgLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                    TooltipContent.Text(language.Reports_BtcLoans_AvgLtv_Tooltip), RightTextForeground: DashboardDataBrushes.ForLtv(dto.DebtWeightedAvgLtv)),
                new(language.Reports_BtcLoans_AvgApr, dto.DebtWeightedAvgApr.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                    TooltipContent.Text(language.Reports_BtcLoans_AvgApr_Tooltip)),
                RowItem.Separator(),
                new(language.Reports_BtcLoans_CollateralFiat, CurrencyDisplay.FormatFiat(dto.TotalCollateralFiatInMainCurrency, fiatCurrency.Code)),
                new(language.Reports_BtcLoans_CollateralSats, CurrencyDisplay.FormatSatsAsBitcoin(dto.TotalCollateralSats) + " BTC"),
                new(language.Reports_BtcLoans_CollateralPercent,
                    dto.CollateralPercentOfStack.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                    TooltipContent.Text(language.Reports_BtcLoans_CollateralPercent_Tooltip),
                    RightTextForeground: DashboardDataBrushes.ForStackPledged(dto.CollateralPercentOfStack)),
                new(language.Reports_BtcLoans_FreeBtc, CurrencyDisplay.FormatSatsAsBitcoin(dto.FreeBtcSats) + " BTC"),
                RowItem.Separator(),
                new(language.Reports_BtcLoans_HealthBreakdown,
                    string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_HealthBreakdown_Format,
                        dto.HealthyCount, dto.WarningCount, dto.DangerCount)),
                new(language.Reports_BtcLoans_HighestLtv, dto.HighestLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                    RightTextForeground: DashboardDataBrushes.ForLtv(dto.HighestLtv)),
                new(language.Reports_BtcLoans_ClosestDistance,
                    dto.ClosestDistanceToLiquidationLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                    TooltipContent.Text(string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_ClosestDistance_Tooltip, dto.ClosestLoanName))),
                new(language.Reports_BtcLoans_WorstCaseLiquidationPrice,
                    CurrencyDisplay.FormatFiat(dto.WorstCaseLiquidationBtcPriceUsd, FiatCurrency.Usd.Code),
                    TooltipContent.Text(language.Reports_BtcLoans_WorstCaseLiquidationPrice_Tooltip)),
                RowItem.Separator(),
                new(language.Reports_BtcLoans_AccruedInterest, CurrencyDisplay.FormatFiat(dto.TotalAccruedInterestInMainCurrency, fiatCurrency.Code)),
                new(language.Reports_BtcLoans_FeesPaid, CurrencyDisplay.FormatFiat(dto.TotalFeesPaidInMainCurrency, fiatCurrency.Code)),
                RowItem.Separator(),
                new(language.Reports_BtcLoans_AvgLoanAge,
                    string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_DaysFormat, (int)Math.Round(dto.AverageLoanAgeDays)))
            };

            if (dto.NextRepaymentDate.HasValue)
            {
                var daysText = string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_DaysFormat, dto.DaysUntilNextRepayment ?? 0);
                rows.Add(new RowItem(language.Reports_BtcLoans_NextRepayment,
                    $"{dto.NextRepaymentDate.Value} ({daysText})",
                    TooltipContent.Text(string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_NextRepayment_Tooltip, dto.NextRepaymentLoanName))));
            }

            SetData(language.Reports_BtcLoans_Title, rows, "\uE227");
            IsVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating BTC loans data");
            IsVisible = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override void Refresh()
    {
        RefreshAsync().FireAndForgetSafeAsync(new FireAndForgetTaskRunner(), _logger);
    }
}
