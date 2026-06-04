using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Builds a ResourceDictionary from a ThemePalette.
/// </summary>
public static class ThemeBuilder
{
    public static ResourceDictionary Create(ThemePalette palette)
    {
        var resources = new ResourceDictionary();

        AddScale(resources, "Accent", palette.Accent);
        AddScale(resources, "Secondary", palette.Secondary);
        AddScale(resources, "Text", palette.Text);
        AddScale(resources, "Background", palette.Background);
        AddScale(resources, "Disabled", palette.Disabled);
        AddScale(resources, "Divider", palette.Divider);
        AddScale(resources, "Icon", palette.Icon);
        AddScale(resources, "SemanticInfo", palette.SemanticInfo);
        AddScale(resources, "SemanticNegative", palette.SemanticNegative);
        AddScale(resources, "SemanticPositive", palette.SemanticPositive);
        AddScale(resources, "SemanticSpecial", palette.SemanticSpecial);
        AddScale(resources, "SemanticWarning", palette.SemanticWarning);

        var s = palette.Specific;
        resources["BlackColor"] = Color.Parse(s.Black);
        resources["WhiteColor"] = Color.Parse(s.White);
        resources["CreditBaseColor"] = Color.Parse(s.CreditBase);
        resources["DebtBaseColor"] = Color.Parse(s.DebtBase);
        resources["TransferBaseColor"] = Color.Parse(s.TransferBase);
        resources["TitleBarForegroundColor"] = Color.Parse(s.TitleBarForeground);
        resources["LiveRatesBackgroundColor"] = Color.Parse(s.LiveRatesBackground);
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse(s.LiveRatesBorderGradientStart);
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse(s.LiveRatesBorderGradientEnd);
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse(s.LiveRatesVariationBackground);
        resources["ButtonOverlayLightColor"] = Color.Parse(s.ButtonOverlayLight);
        resources["ButtonOverlayMediumColor"] = Color.Parse(s.ButtonOverlayMedium);
        resources["StepIndicatorActiveColor"] = Color.Parse(s.StepIndicatorActive);
        resources["StepIndicatorInactiveColor"] = Color.Parse(s.StepIndicatorInactive);
        resources["StepIndicatorInactiveLightColor"] = Color.Parse(s.StepIndicatorInactiveLight);
        resources["MessageBoxInfoColor"] = Color.Parse(s.MessageBoxInfo);
        resources["MessageBoxWarningColor"] = Color.Parse(s.MessageBoxWarning);
        resources["MessageBoxErrorColor"] = Color.Parse(s.MessageBoxError);
        resources["MessageBoxQuestionColor"] = Color.Parse(s.MessageBoxQuestion);
        resources["ColorPickerSelectedBorderColor"] = Color.Parse(s.ColorPickerSelectedBorder);
        resources["ModalTopBarBackgroundColor"] = Color.Parse(s.ModalTopBarBackground);
        resources["TopBarButtonBackgroundColor"] = Color.Parse(s.TopBarButtonBackground);
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse(s.TopBarButtonBackgroundHover);
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse(s.TopBarButtonBackgroundSelected);
        resources["TopBarButtonForegroundColor"] = Color.Parse(s.TopBarButtonForeground);
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse(s.TopBarButtonForegroundHover);
        resources["FooterGradientStartColor"] = Color.Parse(s.FooterGradientStart);
        resources["IconSelectorDefaultColor"] = Color.Parse(s.IconSelectorDefault);

        return resources;
    }

    private static void AddScale(ResourceDictionary resources, string prefix, ColorScale scale)
    {
        resources[$"{prefix}100Color"] = Color.Parse(scale.C100);
        resources[$"{prefix}200Color"] = Color.Parse(scale.C200);
        resources[$"{prefix}300Color"] = Color.Parse(scale.C300);
        resources[$"{prefix}400Color"] = Color.Parse(scale.C400);
        resources[$"{prefix}500Color"] = Color.Parse(scale.C500);
        resources[$"{prefix}600Color"] = Color.Parse(scale.C600);
        resources[$"{prefix}700Color"] = Color.Parse(scale.C700);
        resources[$"{prefix}800Color"] = Color.Parse(scale.C800);
        resources[$"{prefix}900Color"] = Color.Parse(scale.C900);
    }
}
