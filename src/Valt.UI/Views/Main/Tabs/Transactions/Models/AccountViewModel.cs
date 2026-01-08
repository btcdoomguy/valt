using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public partial class AccountViewModel : ObservableObject
{
    
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public bool Visible { get; set; }
    public string? Icon { get; set; }
    public string? Currency { get; set; }
    public bool IsBtcAccount { get; set; }
    public decimal? FiatTotal { get; set; }
    public long? SatsTotal { get; set; }
    public bool HasFutureTotal { get; set; }
    public decimal? FutureFiatTotal { get; set; }
    public long? FutureSatsTotal { get; set; }

    public Icon? RenderIcon => Icon is not null ? Core.Common.Icon.RestoreFromId(Icon) : null;

    public bool IsHidden => !Visible;

    public AccountViewModel(string id, string type, string name, bool visible, string? icon, string? currency,
        bool isBtcAccount, decimal? fiatTotal, long? satsTotal, bool hasFutureTotal, decimal? futureFiatTotal, long? futureSatsTotal)
    {
        Id = id;
        Type = type;
        Name = name;
        Visible = visible;
        Icon = icon;
        Currency = currency;
        IsBtcAccount = isBtcAccount;
        FiatTotal = fiatTotal;
        SatsTotal = satsTotal;
        HasFutureTotal = hasFutureTotal;
        FutureFiatTotal = futureFiatTotal;
        FutureSatsTotal = futureSatsTotal;
    }

    public AccountViewModel(AccountSummaryDTO dto)
    {
        Id = dto.Id;
        Type = dto.Type;
        Name = dto.Name;
        Visible = dto.Visible;
        Icon = dto.Icon;
        Currency = dto.Currency;
        IsBtcAccount = dto.IsBtcAccount;
        FiatTotal = dto.FiatTotal;
        SatsTotal = dto.SatsTotal;
        HasFutureTotal = dto.HasFutureTotal;
        FutureFiatTotal = dto.FutureFiatTotal;
        FutureSatsTotal = dto.FutureSatsTotal;
    }

    public string FormattedTotal
    {
        get
        {
            if (FiatTotal is not null && Currency is not null)
            {
                return CurrencyDisplay.FormatFiat(FiatTotal.Value, Currency);
            }

            return SatsTotal is not null ? CurrencyDisplay.FormatSatsAsBitcoin(SatsTotal.Value) : string.Empty;
        }
    }
    
    public string FormattedFutureTotal
    {
        get
        {
            if (FutureFiatTotal is not null && Currency is not null)
            {
                return CurrencyDisplay.FormatFiat(FutureFiatTotal.Value, Currency);
            }

            return FutureSatsTotal is not null ? CurrencyDisplay.FormatSatsAsBitcoin(FutureSatsTotal.Value) : string.Empty;
        }
    }
}