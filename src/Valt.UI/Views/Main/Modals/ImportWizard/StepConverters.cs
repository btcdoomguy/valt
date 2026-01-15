using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace Valt.UI.Views.Main.Modals.ImportWizard;

/// <summary>
/// Converts WizardStep to background brush for step indicators.
/// Active/completed steps use accent color, inactive steps use gray.
/// </summary>
public class StepBackgroundConverter : IValueConverter
{
    public static readonly StepBackgroundConverter Instance = new();

    private static SolidColorBrush GetActiveBrush() =>
        Application.Current!.TryGetResource("StepIndicatorActiveBrush", ThemeVariant.Default, out var brush)
            ? (SolidColorBrush)brush! : new SolidColorBrush(Color.Parse("#0078D4"));

    private static SolidColorBrush GetInactiveBrush() =>
        Application.Current!.TryGetResource("StepIndicatorInactiveBrush", ThemeVariant.Default, out var brush)
            ? (SolidColorBrush)brush! : new SolidColorBrush(Color.Parse("#6B6B6B"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WizardStep currentStep || parameter is not string stepParam)
            return GetInactiveBrush();

        if (!int.TryParse(stepParam, out var targetStep))
            return GetInactiveBrush();

        return (int)currentStep >= targetStep ? GetActiveBrush() : GetInactiveBrush();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts WizardStep to foreground brush for step labels.
/// Active/completed steps use accent color, inactive steps use gray.
/// </summary>
public class StepForegroundConverter : IValueConverter
{
    public static readonly StepForegroundConverter Instance = new();

    private static SolidColorBrush GetForegroundActiveBrush() =>
        Application.Current!.TryGetResource("StepIndicatorActiveBrush", ThemeVariant.Default, out var brush)
            ? (SolidColorBrush)brush! : new SolidColorBrush(Color.Parse("#0078D4"));

    private static SolidColorBrush GetForegroundInactiveBrush() =>
        Application.Current!.TryGetResource("StepIndicatorInactiveLightBrush", ThemeVariant.Default, out var brush)
            ? (SolidColorBrush)brush! : new SolidColorBrush(Color.Parse("#9E9E9E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WizardStep currentStep || parameter is not string stepParam)
            return GetForegroundInactiveBrush();

        if (!int.TryParse(stepParam, out var targetStep))
            return GetForegroundInactiveBrush();

        return (int)currentStep >= targetStep ? GetForegroundActiveBrush() : GetForegroundInactiveBrush();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts WizardStep to opacity for connector lines.
/// Lines between completed steps are fully visible, others are dimmed.
/// </summary>
public class StepConnectorConverter : IValueConverter
{
    public static readonly StepConnectorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WizardStep currentStep || parameter is not string stepParam)
            return 0.3;

        if (!int.TryParse(stepParam, out var targetStep))
            return 0.3;

        // Connector before step N is visible if current step >= N
        return (int)currentStep >= targetStep ? 1.0 : 0.3;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts WizardStep to visibility for step content panels.
/// Only the current step content is visible.
/// </summary>
public class StepVisibilityConverter : IValueConverter
{
    public static readonly StepVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WizardStep currentStep || parameter is not string stepParam)
            return false;

        if (!int.TryParse(stepParam, out var targetStep))
            return false;

        return (int)currentStep == targetStep;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
