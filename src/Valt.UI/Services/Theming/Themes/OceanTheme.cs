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

        // Secondary (Coral/Salmon - complementary to teal)
        resources["Secondary100Color"] = Color.Parse("#F1CCCC");
        resources["Secondary200Color"] = Color.Parse("#F1A9A9");
        resources["Secondary300Color"] = Color.Parse("#EF7A7A");
        resources["Secondary400Color"] = Color.Parse("#E86060");
        resources["Secondary500Color"] = Color.Parse("#D93B3B");
        resources["Secondary600Color"] = Color.Parse("#BB2D2D");
        resources["Secondary700Color"] = Color.Parse("#932A2A");
        resources["Secondary800Color"] = Color.Parse("#7B2525");
        resources["Secondary900Color"] = Color.Parse("#5E1619");

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
        resources["Background100Color"] = Color.Parse("#6B9494");  // Lightest - teal tinted borders
        resources["Background200Color"] = Color.Parse("#5B8282");  // Light borders
        resources["Background300Color"] = Color.Parse("#4D6F6F");  // Medium borders
        resources["Background400Color"] = Color.Parse("#405C5C");  // Subtle borders
        resources["Background500Color"] = Color.Parse("#364D4D");  // Border color
        resources["Background600Color"] = Color.Parse("#2D4040");  // Elevated surface
        resources["Background700Color"] = Color.Parse("#263636");  // Card background
        resources["Background800Color"] = Color.Parse("#1F3333");  // Main background
        resources["Background900Color"] = Color.Parse("#152424");  // Darkest background

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#6B9494");
        resources["Disabled200Color"] = Color.Parse("#5B8282");
        resources["Disabled300Color"] = Color.Parse("#4D6F6F");
        resources["Disabled400Color"] = Color.Parse("#405C5C");
        resources["Disabled500Color"] = Color.Parse("#364D4D");
        resources["Disabled600Color"] = Color.Parse("#2D4040");
        resources["Disabled700Color"] = Color.Parse("#263636");
        resources["Disabled800Color"] = Color.Parse("#1F3333");
        resources["Disabled900Color"] = Color.Parse("#152424");

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

        // Icon (Coral/Salmon - complementary to teal)
        resources["Icon100Color"] = Color.Parse("#FED7D7");
        resources["Icon200Color"] = Color.Parse("#FEB2B2");
        resources["Icon300Color"] = Color.Parse("#FC8181");
        resources["Icon400Color"] = Color.Parse("#F56565");
        resources["Icon500Color"] = Color.Parse("#E53E3E");
        resources["Icon600Color"] = Color.Parse("#C53030");
        resources["Icon700Color"] = Color.Parse("#9B2C2C");
        resources["Icon800Color"] = Color.Parse("#822727");
        resources["Icon900Color"] = Color.Parse("#63171B");

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

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#1F3333");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#2C7A7B");  // Teal accent for border
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#364D4D");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#263636");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#319795");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#4D6F6F");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#6B9494");
        resources["MessageBoxInfoColor"] = Color.Parse("#4299E1");
        resources["MessageBoxWarningColor"] = Color.Parse("#D69E2E");
        resources["MessageBoxErrorColor"] = Color.Parse("#E53E3E");
        resources["MessageBoxQuestionColor"] = Color.Parse("#805AD5");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#38B2AC");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#2D4040");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#1D4044");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#285E61");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#38B2AC");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#4FD1C5");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#F7FAFC");
        resources["FooterGradientStartColor"] = Color.Parse("#152424");
        resources["IconSelectorDefaultColor"] = Color.Parse("#F7FAFC");

        return resources;
    }
}
