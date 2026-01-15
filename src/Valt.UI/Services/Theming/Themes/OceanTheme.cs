using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Ocean theme - Teal/Cyan accent dark theme
/// </summary>
public static class OceanTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Teal/Cyan)
        resources["Accent100Color"] = Color.Parse("#B2F5EA");
        resources["Accent200Color"] = Color.Parse("#81E6D9");
        resources["Accent300Color"] = Color.Parse("#4FD1C5");
        resources["Accent400Color"] = Color.Parse("#38B2AC");
        resources["Accent500Color"] = Color.Parse("#319795");
        resources["Accent600Color"] = Color.Parse("#2C7A7B");
        resources["Accent700Color"] = Color.Parse("#285E61");
        resources["Accent800Color"] = Color.Parse("#234E52");
        resources["Accent900Color"] = Color.Parse("#1D4044");

        // Secondary (Deep Blue)
        resources["Secondary100Color"] = Color.Parse("#BEE3F8");
        resources["Secondary200Color"] = Color.Parse("#90CDF4");
        resources["Secondary300Color"] = Color.Parse("#63B3ED");
        resources["Secondary400Color"] = Color.Parse("#4299E1");
        resources["Secondary500Color"] = Color.Parse("#3182CE");
        resources["Secondary600Color"] = Color.Parse("#2B6CB0");
        resources["Secondary700Color"] = Color.Parse("#2C5282");
        resources["Secondary800Color"] = Color.Parse("#2A4365");
        resources["Secondary900Color"] = Color.Parse("#1A365D");

        // Text (Cool Gray for dark backgrounds)
        resources["Text100Color"] = Color.Parse("#F7FAFC");
        resources["Text200Color"] = Color.Parse("#EDF2F7");
        resources["Text300Color"] = Color.Parse("#E2E8F0");
        resources["Text400Color"] = Color.Parse("#CBD5E0");
        resources["Text500Color"] = Color.Parse("#A0AEC0");
        resources["Text600Color"] = Color.Parse("#718096");
        resources["Text700Color"] = Color.Parse("#4A5568");
        resources["Text800Color"] = Color.Parse("#2D3748");
        resources["Text900Color"] = Color.Parse("#1A202C");

        // Background (Deep teal-tinted - visible cyan/teal tint)
        resources["Background100Color"] = Color.Parse("#5A7A7A");  // Lightest - teal tinted borders
        resources["Background200Color"] = Color.Parse("#4A6868");  // Light borders
        resources["Background300Color"] = Color.Parse("#3C5656");  // Medium borders
        resources["Background400Color"] = Color.Parse("#2F4545");  // Subtle borders
        resources["Background500Color"] = Color.Parse("#263838");  // Border color
        resources["Background600Color"] = Color.Parse("#1E2E2E");  // Elevated surface
        resources["Background700Color"] = Color.Parse("#182626");  // Card background
        resources["Background800Color"] = Color.Parse("#121E1E");  // Main background
        resources["Background900Color"] = Color.Parse("#080E0E");  // Darkest background

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#5A7A7A");
        resources["Disabled200Color"] = Color.Parse("#4A6868");
        resources["Disabled300Color"] = Color.Parse("#3C5656");
        resources["Disabled400Color"] = Color.Parse("#2F4545");
        resources["Disabled500Color"] = Color.Parse("#263838");
        resources["Disabled600Color"] = Color.Parse("#1E2E2E");
        resources["Disabled700Color"] = Color.Parse("#182626");
        resources["Disabled800Color"] = Color.Parse("#121E1E");
        resources["Disabled900Color"] = Color.Parse("#080E0E");

        // Divider (Cool Gray)
        resources["Divider100Color"] = Color.Parse("#E2E8F0");
        resources["Divider200Color"] = Color.Parse("#CBD5E0");
        resources["Divider300Color"] = Color.Parse("#A0AEC0");
        resources["Divider400Color"] = Color.Parse("#718096");
        resources["Divider500Color"] = Color.Parse("#4A5568");
        resources["Divider600Color"] = Color.Parse("#2D3748");
        resources["Divider700Color"] = Color.Parse("#1A202C");
        resources["Divider800Color"] = Color.Parse("#171923");
        resources["Divider900Color"] = Color.Parse("#0D1117");

        // Icon (Teal tinted)
        resources["Icon100Color"] = Color.Parse("#B2F5EA");
        resources["Icon200Color"] = Color.Parse("#81E6D9");
        resources["Icon300Color"] = Color.Parse("#4FD1C5");
        resources["Icon400Color"] = Color.Parse("#38B2AC");
        resources["Icon500Color"] = Color.Parse("#319795");
        resources["Icon600Color"] = Color.Parse("#2C7A7B");
        resources["Icon700Color"] = Color.Parse("#285E61");
        resources["Icon800Color"] = Color.Parse("#234E52");
        resources["Icon900Color"] = Color.Parse("#1D4044");

        // Semantic Info (Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#90CDF4");
        resources["SemanticInfo200Color"] = Color.Parse("#63B3ED");
        resources["SemanticInfo300Color"] = Color.Parse("#4299E1");
        resources["SemanticInfo400Color"] = Color.Parse("#3182CE");
        resources["SemanticInfo500Color"] = Color.Parse("#2B6CB0");
        resources["SemanticInfo600Color"] = Color.Parse("#2C5282");
        resources["SemanticInfo700Color"] = Color.Parse("#2A4365");
        resources["SemanticInfo800Color"] = Color.Parse("#1A365D");
        resources["SemanticInfo900Color"] = Color.Parse("#0D1B2A");

        // Semantic Negative (Red)
        resources["SemanticNegative100Color"] = Color.Parse("#FEB2B2");
        resources["SemanticNegative200Color"] = Color.Parse("#FC8181");
        resources["SemanticNegative300Color"] = Color.Parse("#F56565");
        resources["SemanticNegative400Color"] = Color.Parse("#E53E3E");
        resources["SemanticNegative500Color"] = Color.Parse("#C53030");
        resources["SemanticNegative600Color"] = Color.Parse("#9B2C2C");
        resources["SemanticNegative700Color"] = Color.Parse("#742A2A");
        resources["SemanticNegative800Color"] = Color.Parse("#4A1F1F");
        resources["SemanticNegative900Color"] = Color.Parse("#1A0A0A");

        // Semantic Positive (Green)
        resources["SemanticPositive100Color"] = Color.Parse("#9AE6B4");
        resources["SemanticPositive200Color"] = Color.Parse("#68D391");
        resources["SemanticPositive300Color"] = Color.Parse("#48BB78");
        resources["SemanticPositive400Color"] = Color.Parse("#38A169");
        resources["SemanticPositive500Color"] = Color.Parse("#2F855A");
        resources["SemanticPositive600Color"] = Color.Parse("#276749");
        resources["SemanticPositive700Color"] = Color.Parse("#22543D");
        resources["SemanticPositive800Color"] = Color.Parse("#1C4532");
        resources["SemanticPositive900Color"] = Color.Parse("#0D2818");

        // Semantic Special (Purple)
        resources["SemanticSpecial100Color"] = Color.Parse("#D6BCFA");
        resources["SemanticSpecial200Color"] = Color.Parse("#B794F4");
        resources["SemanticSpecial300Color"] = Color.Parse("#9F7AEA");
        resources["SemanticSpecial400Color"] = Color.Parse("#805AD5");
        resources["SemanticSpecial500Color"] = Color.Parse("#6B46C1");
        resources["SemanticSpecial600Color"] = Color.Parse("#553C9A");
        resources["SemanticSpecial700Color"] = Color.Parse("#44337A");
        resources["SemanticSpecial800Color"] = Color.Parse("#322659");
        resources["SemanticSpecial900Color"] = Color.Parse("#1A1033");

        // Semantic Warning (Yellow/Amber)
        resources["SemanticWarning100Color"] = Color.Parse("#FAF089");
        resources["SemanticWarning200Color"] = Color.Parse("#F6E05E");
        resources["SemanticWarning300Color"] = Color.Parse("#ECC94B");
        resources["SemanticWarning400Color"] = Color.Parse("#D69E2E");
        resources["SemanticWarning500Color"] = Color.Parse("#B7791F");
        resources["SemanticWarning600Color"] = Color.Parse("#975A16");
        resources["SemanticWarning700Color"] = Color.Parse("#744210");
        resources["SemanticWarning800Color"] = Color.Parse("#5F370E");
        resources["SemanticWarning900Color"] = Color.Parse("#3D2508");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#0D1117");
        resources["WhiteColor"] = Color.Parse("#F7FAFC");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#121E1E");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#2C7A7B");  // Teal accent for border
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#263838");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#182626");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#319795");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#3C5656");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#5A7A7A");
        resources["MessageBoxInfoColor"] = Color.Parse("#4299E1");
        resources["MessageBoxWarningColor"] = Color.Parse("#D69E2E");
        resources["MessageBoxErrorColor"] = Color.Parse("#E53E3E");
        resources["MessageBoxQuestionColor"] = Color.Parse("#805AD5");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#38B2AC");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#1E2E2E");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#1D4044");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#285E61");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#38B2AC");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#4FD1C5");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#F7FAFC");
        resources["FooterGradientStartColor"] = Color.Parse("#080E0E");
        resources["IconSelectorDefaultColor"] = Color.Parse("#F7FAFC");

        return resources;
    }
}
