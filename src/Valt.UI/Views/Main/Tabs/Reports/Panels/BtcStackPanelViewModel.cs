using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MaxBtcStack;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Panel displaying BTC stack information (current stack, % of supply, people estimate, max stack).
/// </summary>
public partial class BtcStackPanelViewModel : DashboardPanelViewModel
{
    private readonly AccountsTotalState _accountsTotalState;
    private readonly IMaxBtcStackReport _maxBtcStackReport;
    private readonly IClock _clock;
    private readonly ILogger<BtcStackPanelViewModel> _logger;

    private const long TotalBtcSupplySats = 21_000_000_00_000_000L;
    private MaxBtcStackData? _maxBtcStackData;

    public BtcStackPanelViewModel(
        AccountsTotalState accountsTotalState,
        IMaxBtcStackReport maxBtcStackReport,
        IClock clock,
        ILogger<BtcStackPanelViewModel> logger)
        : base(logger)
    {
        _accountsTotalState = accountsTotalState;
        _maxBtcStackReport = maxBtcStackReport;
        _clock = clock;
        _logger = logger;
    }

    public override Task RefreshAsync()
    {
        Dispatcher.UIThread.Post(Refresh);
        return Task.CompletedTask;
    }

    public override void Refresh()
    {
        try
        {
            var wealth = _accountsTotalState.CurrentWealth;
            if (wealth.WealthInSats == 0)
            {
                var emptyRows = new ObservableCollection<RowItem>
                {
                    new(language.Reports_BtcStack_CurrentStack, "0 BTC"),
                    new(language.Reports_BtcStack_PercentOfSupply, "0%"),
                    new(language.Reports_BtcStack_PeopleWithSameStack, "\u221e", TooltipContent.Text(language.Reports_BtcStack_PeopleWithSameStack_Tooltip))
                };
                SetData(language.Reports_BtcStack_Title, emptyRows, "\uEBC5");
                return;
            }

            var btcFormatted = FormatBtc(wealth.WealthInSats);
            var percentOfSupply = (decimal)wealth.WealthInSats / TotalBtcSupplySats * 100m;
            var percentFormatted = percentOfSupply.ToString("0.############") + "%";
            var peopleWithSameStack = Math.Round((decimal)TotalBtcSupplySats / wealth.WealthInSats);
            var peopleFormatted = peopleWithSameStack.ToString("N0", CultureInfo.CurrentCulture);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_BtcStack_CurrentStack, btcFormatted),
                new(language.Reports_BtcStack_PercentOfSupply, percentFormatted),
                new(language.Reports_BtcStack_PeopleWithSameStack, peopleFormatted, TooltipContent.Text(language.Reports_BtcStack_PeopleWithSameStack_Tooltip))
            };

            if (_maxBtcStackData is not null)
            {
                var maxBtcFormatted = FormatBtc(_maxBtcStackData.MaxStackInSats);
                rows.Add(new RowItem(language.Reports_BtcStack_MaxStack, maxBtcFormatted));
                rows.Add(new RowItem(language.Reports_BtcStack_MaxStackDate, _maxBtcStackData.Date.ToString()));
                rows.Add(new RowItem(language.Reports_BtcStack_DeclineFromMax, $"{_maxBtcStackData.DeclineFromMaxPercent}%"));
            }

            SetData(language.Reports_BtcStack_Title, rows, "\uEBC5");
        }
        catch (Exception ex)
        {
            SetError(language.Reports_BtcStack_Title, ex, "\uEBC5");
        }
    }

    public async Task FetchMaxBtcStackAsync(IReportDataProvider provider)
    {
        try
        {
            var wealth = _accountsTotalState.CurrentWealth;
            if (wealth.WealthInSats == 0)
                return;

            _maxBtcStackData = await _maxBtcStackReport.GetAsync(wealth.WealthInSats, provider);
            Dispatcher.UIThread.Post(Refresh);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching max BTC stack data");
        }
    }
}
