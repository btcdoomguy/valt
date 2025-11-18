using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public static class TransactionGridResources
{
    private static SolidColorBrush? _creditResource;
    private static SolidColorBrush? _debtResource;
    private static SolidColorBrush? _transferResource;
    private static SolidColorBrush? _futureLineResource;
    private static SolidColorBrush? _regularLineResource;

    public static void Initialize()
    {
        _creditResource = GetResource("TransactionAmountCredit", Colors.Gray);
        _debtResource = GetResource("TransactionAmountDebt", Colors.Gray);
        _transferResource = GetResource("TransactionAmountTransfer", Colors.Gray);
        _futureLineResource = GetResource("FutureColor", Colors.Gray);
        _regularLineResource = GetResource("RegularColor", Colors.Gray);
    }

    private static SolidColorBrush GetResource(string key, Color defaultColor)
    {
        if (!Application.Current!.TryGetResource(key, ThemeVariant.Default, out var resource))
        {
            return new SolidColorBrush(defaultColor);
        }

        return (SolidColorBrush)resource!;
    }

    public static SolidColorBrush Credit
    {
        get
        {
            if (_creditResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _creditResource;
        }
    }

    public static SolidColorBrush Debt
    {
        get
        {
            if (_debtResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _debtResource;
        }
    }

    public static SolidColorBrush Transfer
    {
        get
        {
            if (_transferResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _transferResource;
        }
    }

    public static SolidColorBrush FutureLine
    {
        get
        {
            if (_futureLineResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _futureLineResource;
        }
    }

    public static SolidColorBrush RegularLine
    {
        get
        {
            if (_regularLineResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _regularLineResource;
        }
    }
}