using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying the current-month burn rate gauge on the reports dashboard.
/// </summary>
public partial class BurnRatePanelViewModel : DashboardPanelViewModel
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AccountsTotalState _accountsTotalState;
    private readonly CurrencySettings _currencySettings;
    private readonly ILogger<BurnRatePanelViewModel> _logger;

    private IReadOnlyList<string> _selectedCategoryIds = Array.Empty<string>();

    public BurnRatePanelViewModel(
        IQueryDispatcher queryDispatcher,
        AccountsTotalState accountsTotalState,
        CurrencySettings currencySettings,
        ILogger<BurnRatePanelViewModel> logger)
        : base(logger)
    {
        _queryDispatcher = queryDispatcher;
        _accountsTotalState = accountsTotalState;
        _currencySettings = currencySettings;
        _logger = logger;
    }

    public void SetCategoryFilter(IReadOnlyList<string> selectedCategoryIds)
    {
        _selectedCategoryIds = selectedCategoryIds ?? Array.Empty<string>();
    }

    public override async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;

            var dto = await _queryDispatcher.DispatchAsync(new GetBurnRateQuery
            {
                CurrentWealthInFiat = _accountsTotalState.CurrentWealth.AllWealthInMainFiatCurrency,
                CategoryIds = _selectedCategoryIds.ToArray()
            });

            var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

            if (!dto.HasData)
            {
                SetData(language.Reports_BurnRate_Title,
                    new ObservableCollection<RowItem>
                    {
                        new(language.Reports_BurnRate_Empty, string.Empty)
                    },
                    "\uE80E");
                IsVisible = true;
                return;
            }

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_BurnRate_SpentSoFar, FormatFiat(dto.SpentSoFar, fiatCurrency.Code)),
                new(language.Reports_BurnRate_AvgDaily, FormatFiat(dto.AvgDailySpend, fiatCurrency.Code))
            };

            if (dto.ProjectedMonthEnd.HasValue)
            {
                rows.Add(new(language.Reports_BurnRate_Projected, FormatFiat(dto.ProjectedMonthEnd.Value, fiatCurrency.Code)));
                rows.Add(new(language.Reports_BurnRate_MedianMonth, FormatFiat(dto.MedianMonthlyExpenses, fiatCurrency.Code)));

                var vsMedianText = dto.VsMedianPercent.HasValue
                    ? FormatSignedPercent(dto.VsMedianPercent.Value)
                    : "\u2014";
                var vsMedianColor = dto.ProjectedMonthEnd.Value <= dto.MedianMonthlyExpenses
                    ? TransactionGridResources.Credit
                    : TransactionGridResources.Debt;

                rows.Add(new(language.Reports_BurnRate_VsMedian, vsMedianText,
                    TooltipContent.Text(language.Reports_BurnRate_VsMedian_Tooltip),
                    RightTextForeground: vsMedianColor));
            }
            else
            {
                rows.Add(new(language.Reports_BurnRate_EarlyNote, "\u2014"));
            }

            SetData(language.Reports_BurnRate_Title, rows, "\uE80E");
            IsVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating burn rate data");
            SetError(language.Reports_BurnRate_Title, ex, "\uE80E");
            IsVisible = true;
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

    private static string FormatSignedPercent(decimal value) =>
        value >= 0 ? $"+{value}%" : $"{value}%";
}
