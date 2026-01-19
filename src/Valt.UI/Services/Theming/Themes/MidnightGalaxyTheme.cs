using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Midnight Galaxy theme - Deep purple/violet cosmic dark theme
/// </summary>
public static class MidnightGalaxyTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Violet/Purple)
        resources["Accent100Color"] = Color.Parse("#E9D8FD");
        resources["Accent200Color"] = Color.Parse("#D6BCFA");
        resources["Accent300Color"] = Color.Parse("#B794F4");
        resources["Accent400Color"] = Color.Parse("#9F7AEA");
        resources["Accent500Color"] = Color.Parse("#805AD5");
        resources["Accent600Color"] = Color.Parse("#6B46C1");
        resources["Accent700Color"] = Color.Parse("#553C9A");
        resources["Accent800Color"] = Color.Parse("#44337A");
        resources["Accent900Color"] = Color.Parse("#322659");

        // Secondary (Deep Indigo)
        resources["Secondary100Color"] = Color.Parse("#C3DAFE");
        resources["Secondary200Color"] = Color.Parse("#A3BFFA");
        resources["Secondary300Color"] = Color.Parse("#7F9CF5");
        resources["Secondary400Color"] = Color.Parse("#667EEA");
        resources["Secondary500Color"] = Color.Parse("#5A67D8");
        resources["Secondary600Color"] = Color.Parse("#4C51BF");
        resources["Secondary700Color"] = Color.Parse("#434190");
        resources["Secondary800Color"] = Color.Parse("#3C366B");
        resources["Secondary900Color"] = Color.Parse("#2D2A5C");

        // Text (Cool violet-tinted gray)
        resources["Text100Color"] = Color.Parse("#FAF5FF");
        resources["Text200Color"] = Color.Parse("#E9E3F0");
        resources["Text300Color"] = Color.Parse("#D6CFE2");
        resources["Text400Color"] = Color.Parse("#B8B0C8");
        resources["Text500Color"] = Color.Parse("#9590A8");
        resources["Text600Color"] = Color.Parse("#706B85");
        resources["Text700Color"] = Color.Parse("#524D66");
        resources["Text800Color"] = Color.Parse("#363245");
        resources["Text900Color"] = Color.Parse("#1E1B2A");

        // Background (Deep space purple - visible purple tint)
        resources["Background100Color"] = Color.Parse("#7A6B9A");
        resources["Background200Color"] = Color.Parse("#685A88");
        resources["Background300Color"] = Color.Parse("#564A75");
        resources["Background400Color"] = Color.Parse("#453B62");
        resources["Background500Color"] = Color.Parse("#382F52");
        resources["Background600Color"] = Color.Parse("#2C2542");
        resources["Background700Color"] = Color.Parse("#251F38");
        resources["Background800Color"] = Color.Parse("#1E1A2E");
        resources["Background900Color"] = Color.Parse("#0A0810");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#7A6B9A");
        resources["Disabled200Color"] = Color.Parse("#685A88");
        resources["Disabled300Color"] = Color.Parse("#564A75");
        resources["Disabled400Color"] = Color.Parse("#453B62");
        resources["Disabled500Color"] = Color.Parse("#382F52");
        resources["Disabled600Color"] = Color.Parse("#2C2542");
        resources["Disabled700Color"] = Color.Parse("#251F38");
        resources["Disabled800Color"] = Color.Parse("#1E1A2E");
        resources["Disabled900Color"] = Color.Parse("#0A0810");

        // Divider (Violet-tinted gray)
        resources["Divider100Color"] = Color.Parse("#E9E3F0");
        resources["Divider200Color"] = Color.Parse("#D6CFE2");
        resources["Divider300Color"] = Color.Parse("#B8B0C8");
        resources["Divider400Color"] = Color.Parse("#9590A8");
        resources["Divider500Color"] = Color.Parse("#706B85");
        resources["Divider600Color"] = Color.Parse("#524D66");
        resources["Divider700Color"] = Color.Parse("#363245");
        resources["Divider800Color"] = Color.Parse("#252035");
        resources["Divider900Color"] = Color.Parse("#14111E");

        // Icon (Violet tinted)
        resources["Icon100Color"] = Color.Parse("#E9D8FD");
        resources["Icon200Color"] = Color.Parse("#D6BCFA");
        resources["Icon300Color"] = Color.Parse("#B794F4");
        resources["Icon400Color"] = Color.Parse("#9F7AEA");
        resources["Icon500Color"] = Color.Parse("#805AD5");
        resources["Icon600Color"] = Color.Parse("#6B46C1");
        resources["Icon700Color"] = Color.Parse("#553C9A");
        resources["Icon800Color"] = Color.Parse("#44337A");
        resources["Icon900Color"] = Color.Parse("#322659");

        // Semantic Info (Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#A3BFFA");
        resources["SemanticInfo200Color"] = Color.Parse("#7F9CF5");
        resources["SemanticInfo300Color"] = Color.Parse("#667EEA");
        resources["SemanticInfo400Color"] = Color.Parse("#5A67D8");
        resources["SemanticInfo500Color"] = Color.Parse("#4C51BF");
        resources["SemanticInfo600Color"] = Color.Parse("#434190");
        resources["SemanticInfo700Color"] = Color.Parse("#3C366B");
        resources["SemanticInfo800Color"] = Color.Parse("#2D2A5C");
        resources["SemanticInfo900Color"] = Color.Parse("#1A1744");

        // Semantic Negative (Red/Pink)
        resources["SemanticNegative100Color"] = Color.Parse("#FED7E2");
        resources["SemanticNegative200Color"] = Color.Parse("#FBB6CE");
        resources["SemanticNegative300Color"] = Color.Parse("#F687B3");
        resources["SemanticNegative400Color"] = Color.Parse("#ED64A6");
        resources["SemanticNegative500Color"] = Color.Parse("#D53F8C");
        resources["SemanticNegative600Color"] = Color.Parse("#B83280");
        resources["SemanticNegative700Color"] = Color.Parse("#97266D");
        resources["SemanticNegative800Color"] = Color.Parse("#702459");
        resources["SemanticNegative900Color"] = Color.Parse("#4A1942");

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

        // Semantic Special (Cyan)
        resources["SemanticSpecial100Color"] = Color.Parse("#B2F5EA");
        resources["SemanticSpecial200Color"] = Color.Parse("#81E6D9");
        resources["SemanticSpecial300Color"] = Color.Parse("#4FD1C5");
        resources["SemanticSpecial400Color"] = Color.Parse("#38B2AC");
        resources["SemanticSpecial500Color"] = Color.Parse("#319795");
        resources["SemanticSpecial600Color"] = Color.Parse("#2C7A7B");
        resources["SemanticSpecial700Color"] = Color.Parse("#285E61");
        resources["SemanticSpecial800Color"] = Color.Parse("#234E52");
        resources["SemanticSpecial900Color"] = Color.Parse("#1D4044");

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
        resources["BlackColor"] = Color.Parse("#08060C");
        resources["WhiteColor"] = Color.Parse("#FAF5FF");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#1E1A2E");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#6B46C1");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#382F52");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#251F38");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#805AD5");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#564A75");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#7A6B9A");
        resources["MessageBoxInfoColor"] = Color.Parse("#667EEA");
        resources["MessageBoxWarningColor"] = Color.Parse("#D69E2E");
        resources["MessageBoxErrorColor"] = Color.Parse("#ED64A6");
        resources["MessageBoxQuestionColor"] = Color.Parse("#38B2AC");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#9F7AEA");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#2C2542");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#322659");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#44337A");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#9F7AEA");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#B794F4");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FAF5FF");
        resources["FooterGradientStartColor"] = Color.Parse("#0A0810");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FAF5FF");

        return resources;
    }
}
