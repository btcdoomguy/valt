using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public static class TransactionGridResources
{
    private static SolidColorBrush? _creditResource = null;
    private static SolidColorBrush? _debtResource = null;
    private static SolidColorBrush? _transferResource = null;
    private static SolidColorBrush? _futureLineResource = null;
    private static SolidColorBrush? _regularLineResource = null;

    public static SolidColorBrush Credit
    {
        get
        {
            if (_creditResource is not null) return _creditResource;

            if (!Application.Current!.TryGetResource("TransactionAmountCredit", ThemeVariant.Default,
                    out var color))
            {
                return new SolidColorBrush(Colors.Gray);
            }

            _creditResource = (SolidColorBrush)color!;
            
            return _creditResource;
        }
    }

    public static SolidColorBrush Debt
    {
        get
        {
            if (_debtResource is not null) return _debtResource;

            if (!Application.Current!.TryGetResource("TransactionAmountDebt", ThemeVariant.Default,
                    out var color))
            {
                return new SolidColorBrush(Colors.Gray);
            }

            _debtResource = (SolidColorBrush)color!;
            
            return _debtResource;
        }
    }

    public static SolidColorBrush Transfer
    {
        get
        {
            if (_transferResource is not null) return _transferResource;

            if (!Application.Current!.TryGetResource("TransactionAmountTransfer", ThemeVariant.Default,
                    out var color))
            {
                return new SolidColorBrush(Colors.Gray);
            }

            _transferResource = (SolidColorBrush)color!;
            
            return _transferResource;
        }
    }

    public static SolidColorBrush FutureLine
    {
        get
        {
            if (_futureLineResource is not null) return _futureLineResource;
            
            if (!Application.Current!.TryGetResource("FutureColor", ThemeVariant.Default,
                    out var color))
            {
                return new SolidColorBrush(Colors.Gray);
            }
            
            _futureLineResource = (SolidColorBrush)color!;

            return _futureLineResource;
        }
    }
    
    public static SolidColorBrush RegularLine
    {
        get
        {
            if (_regularLineResource is not null) return _regularLineResource;
            
            if (!Application.Current!.TryGetResource("RegularColor", ThemeVariant.Default,
                    out var color))
            {
                return new SolidColorBrush(Colors.Gray);
            }
            
            _regularLineResource = (SolidColorBrush)color!;

            return _regularLineResource;
        }
    }
}