using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public static class FixedExpenseListResources
{
    private static SolidColorBrush? _defaultForegroundResource;
    private static SolidColorBrush? _paidForegroundResource;
    private static SolidColorBrush? _ignoredForegroundResource;
    private static SolidColorBrush? _lateForegroundResource;
    private static SolidColorBrush? _warningForegroundResource;


    public static void Initialize()
    {
        _defaultForegroundResource = GetResource("Text100Brush", Colors.Gray);
        _ignoredForegroundResource = GetResource("Text500Brush", Colors.Gray);
        _paidForegroundResource = GetResource("SemanticPositive500Brush", Colors.Gray);
        _lateForegroundResource = GetResource("SemanticNegative200Brush", Colors.Gray);
        _warningForegroundResource = GetResource("SemanticWarning400Brush", Colors.Yellow);
    }

    private static SolidColorBrush GetResource(string key, Color defaultColor)
    {
        if (!Application.Current!.TryGetResource(key, ThemeVariant.Default, out var resource))
        {
            return new SolidColorBrush(defaultColor);
        }

        return (SolidColorBrush)resource!;
    }

    public static SolidColorBrush DefaultForeground
    {
        get
        {
            if (_defaultForegroundResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _defaultForegroundResource;
        }
    }

    public static SolidColorBrush PaidForeground
    {
        get
        {
            if (_paidForegroundResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _paidForegroundResource;
        }
    }
    
    public static SolidColorBrush IgnoredForeground
    {
        get
        {
            if (_ignoredForegroundResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _ignoredForegroundResource;
        }
    }
    
    public static SolidColorBrush LateForeground
    {
        get
        {
            if (_lateForegroundResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _lateForegroundResource;
        }
    }

    public static SolidColorBrush WarningForeground
    {
        get
        {
            if (_warningForegroundResource is null)
            {
                throw new InvalidOperationException("Resources not initialized. Call Initialize() first.");
            }
            return _warningForegroundResource;
        }
    }
}