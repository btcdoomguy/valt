using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Copper Forge theme - Metallic copper/bronze accent dark theme
/// Industrial and refined aesthetic
/// </summary>
public static class CopperForgeTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Copper/Bronze)
        resources["Accent100Color"] = Color.Parse("#F5D6C6");
        resources["Accent200Color"] = Color.Parse("#E8B89E");
        resources["Accent300Color"] = Color.Parse("#D4936F");
        resources["Accent400Color"] = Color.Parse("#C07848");
        resources["Accent500Color"] = Color.Parse("#B87333");  // Classic copper
        resources["Accent600Color"] = Color.Parse("#9A5F2A");
        resources["Accent700Color"] = Color.Parse("#7C4B22");
        resources["Accent800Color"] = Color.Parse("#5E3918");
        resources["Accent900Color"] = Color.Parse("#402710");

        // Secondary (Antique Gold/Brass)
        resources["Secondary100Color"] = Color.Parse("#F5E6C8");
        resources["Secondary200Color"] = Color.Parse("#E8D4A0");
        resources["Secondary300Color"] = Color.Parse("#D4BC72");
        resources["Secondary400Color"] = Color.Parse("#C9A84A");
        resources["Secondary500Color"] = Color.Parse("#CD9834");  // Antique brass
        resources["Secondary600Color"] = Color.Parse("#A67C2A");
        resources["Secondary700Color"] = Color.Parse("#806020");
        resources["Secondary800Color"] = Color.Parse("#5A4418");
        resources["Secondary900Color"] = Color.Parse("#3A2C10");

        // Text (Warm metallic-tinted)
        resources["Text100Color"] = Color.Parse("#FAF5F0");
        resources["Text200Color"] = Color.Parse("#F5EBE2");
        resources["Text300Color"] = Color.Parse("#E8DCD0");
        resources["Text400Color"] = Color.Parse("#D4C4B4");
        resources["Text500Color"] = Color.Parse("#B8A494");
        resources["Text600Color"] = Color.Parse("#8A7868");
        resources["Text700Color"] = Color.Parse("#5C504A");
        resources["Text800Color"] = Color.Parse("#3A3230");
        resources["Text900Color"] = Color.Parse("#1E1A18");

        // Background (Deep copper/bronze tinted - metallic warmth)
        resources["Background100Color"] = Color.Parse("#8A7468");
        resources["Background200Color"] = Color.Parse("#78645A");
        resources["Background300Color"] = Color.Parse("#68564C");
        resources["Background400Color"] = Color.Parse("#584840");
        resources["Background500Color"] = Color.Parse("#4A3C36");
        resources["Background600Color"] = Color.Parse("#3E322C");
        resources["Background700Color"] = Color.Parse("#342824");
        resources["Background800Color"] = Color.Parse("#2A201C");
        resources["Background900Color"] = Color.Parse("#1E1614");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#8A7468");
        resources["Disabled200Color"] = Color.Parse("#78645A");
        resources["Disabled300Color"] = Color.Parse("#68564C");
        resources["Disabled400Color"] = Color.Parse("#584840");
        resources["Disabled500Color"] = Color.Parse("#4A3C36");
        resources["Disabled600Color"] = Color.Parse("#3E322C");
        resources["Disabled700Color"] = Color.Parse("#342824");
        resources["Disabled800Color"] = Color.Parse("#2A201C");
        resources["Disabled900Color"] = Color.Parse("#1E1614");

        // Divider (Metallic brown-gray)
        resources["Divider100Color"] = Color.Parse("#F5EBE2");
        resources["Divider200Color"] = Color.Parse("#E8DCD0");
        resources["Divider300Color"] = Color.Parse("#D4C0A8");
        resources["Divider400Color"] = Color.Parse("#B89878");
        resources["Divider500Color"] = Color.Parse("#8A6850");
        resources["Divider600Color"] = Color.Parse("#5C4438");
        resources["Divider700Color"] = Color.Parse("#3A2C24");
        resources["Divider800Color"] = Color.Parse("#281E18");
        resources["Divider900Color"] = Color.Parse("#180E0C");

        // Icon (Copper tinted)
        resources["Icon100Color"] = Color.Parse("#F5D6C6");
        resources["Icon200Color"] = Color.Parse("#E8B89E");
        resources["Icon300Color"] = Color.Parse("#D4936F");
        resources["Icon400Color"] = Color.Parse("#C07848");
        resources["Icon500Color"] = Color.Parse("#B87333");
        resources["Icon600Color"] = Color.Parse("#9A5F2A");
        resources["Icon700Color"] = Color.Parse("#7C4B22");
        resources["Icon800Color"] = Color.Parse("#5E3918");
        resources["Icon900Color"] = Color.Parse("#402710");

        // Semantic Info (Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#BAE6FD");
        resources["SemanticInfo200Color"] = Color.Parse("#7DD3FC");
        resources["SemanticInfo300Color"] = Color.Parse("#38BDF8");
        resources["SemanticInfo400Color"] = Color.Parse("#0EA5E9");
        resources["SemanticInfo500Color"] = Color.Parse("#0284C7");
        resources["SemanticInfo600Color"] = Color.Parse("#0369A1");
        resources["SemanticInfo700Color"] = Color.Parse("#075985");
        resources["SemanticInfo800Color"] = Color.Parse("#0C4A6E");
        resources["SemanticInfo900Color"] = Color.Parse("#082F49");

        // Semantic Negative (Red)
        resources["SemanticNegative100Color"] = Color.Parse("#FEE2E2");
        resources["SemanticNegative200Color"] = Color.Parse("#FECACA");
        resources["SemanticNegative300Color"] = Color.Parse("#FCA5A5");
        resources["SemanticNegative400Color"] = Color.Parse("#F87171");
        resources["SemanticNegative500Color"] = Color.Parse("#EF4444");
        resources["SemanticNegative600Color"] = Color.Parse("#DC2626");
        resources["SemanticNegative700Color"] = Color.Parse("#B91C1C");
        resources["SemanticNegative800Color"] = Color.Parse("#991B1B");
        resources["SemanticNegative900Color"] = Color.Parse("#7F1D1D");

        // Semantic Positive (Green)
        resources["SemanticPositive100Color"] = Color.Parse("#D1FAE5");
        resources["SemanticPositive200Color"] = Color.Parse("#A7F3D0");
        resources["SemanticPositive300Color"] = Color.Parse("#6EE7B7");
        resources["SemanticPositive400Color"] = Color.Parse("#34D399");
        resources["SemanticPositive500Color"] = Color.Parse("#10B981");
        resources["SemanticPositive600Color"] = Color.Parse("#059669");
        resources["SemanticPositive700Color"] = Color.Parse("#047857");
        resources["SemanticPositive800Color"] = Color.Parse("#065F46");
        resources["SemanticPositive900Color"] = Color.Parse("#064E3B");

        // Semantic Special (Purple)
        resources["SemanticSpecial100Color"] = Color.Parse("#E9D5FF");
        resources["SemanticSpecial200Color"] = Color.Parse("#D8B4FE");
        resources["SemanticSpecial300Color"] = Color.Parse("#C084FC");
        resources["SemanticSpecial400Color"] = Color.Parse("#A855F7");
        resources["SemanticSpecial500Color"] = Color.Parse("#9333EA");
        resources["SemanticSpecial600Color"] = Color.Parse("#7E22CE");
        resources["SemanticSpecial700Color"] = Color.Parse("#6B21A8");
        resources["SemanticSpecial800Color"] = Color.Parse("#581C87");
        resources["SemanticSpecial900Color"] = Color.Parse("#3B0764");

        // Semantic Warning (Amber/Gold)
        resources["SemanticWarning100Color"] = Color.Parse("#FEF3C7");
        resources["SemanticWarning200Color"] = Color.Parse("#FDE68A");
        resources["SemanticWarning300Color"] = Color.Parse("#FCD34D");
        resources["SemanticWarning400Color"] = Color.Parse("#FBBF24");
        resources["SemanticWarning500Color"] = Color.Parse("#F59E0B");
        resources["SemanticWarning600Color"] = Color.Parse("#D97706");
        resources["SemanticWarning700Color"] = Color.Parse("#B45309");
        resources["SemanticWarning800Color"] = Color.Parse("#92400E");
        resources["SemanticWarning900Color"] = Color.Parse("#78350F");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#180E0C");
        resources["WhiteColor"] = Color.Parse("#FAF5F0");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#2A201C");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#B87333");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#4A3C36");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#342824");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#C07848");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#68564C");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#8A7468");
        resources["MessageBoxInfoColor"] = Color.Parse("#38BDF8");
        resources["MessageBoxWarningColor"] = Color.Parse("#FBBF24");
        resources["MessageBoxErrorColor"] = Color.Parse("#F87171");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#C07848");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#3E322C");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#402710");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#5E3918");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#C07848");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#D4936F");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FAF5F0");
        resources["FooterGradientStartColor"] = Color.Parse("#1E1614");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FAF5F0");

        return resources;
    }
}
