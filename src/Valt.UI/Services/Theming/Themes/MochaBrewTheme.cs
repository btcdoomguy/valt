using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Mocha Brew theme - Rich coffee/mocha/chocolate accent dark theme
/// Cozy and sophisticated aesthetic
/// </summary>
public static class MochaBrewTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Coffee/Mocha Brown)
        resources["Accent100Color"] = Color.Parse("#E7D4C0");
        resources["Accent200Color"] = Color.Parse("#D4BC9E");
        resources["Accent300Color"] = Color.Parse("#C4A882");
        resources["Accent400Color"] = Color.Parse("#A8876A");
        resources["Accent500Color"] = Color.Parse("#8B6F55");
        resources["Accent600Color"] = Color.Parse("#6F5842");
        resources["Accent700Color"] = Color.Parse("#574533");
        resources["Accent800Color"] = Color.Parse("#3F3225");
        resources["Accent900Color"] = Color.Parse("#2A2119");

        // Secondary (Warm Cream/Latte)
        resources["Secondary100Color"] = Color.Parse("#FAF5EB");
        resources["Secondary200Color"] = Color.Parse("#F5EBD9");
        resources["Secondary300Color"] = Color.Parse("#E8D8BF");
        resources["Secondary400Color"] = Color.Parse("#D4C2A0");
        resources["Secondary500Color"] = Color.Parse("#BBA882");
        resources["Secondary600Color"] = Color.Parse("#9A8866");
        resources["Secondary700Color"] = Color.Parse("#7A6A4E");
        resources["Secondary800Color"] = Color.Parse("#5C503C");
        resources["Secondary900Color"] = Color.Parse("#3E362A");

        // Text (Warm cream-tinted)
        resources["Text100Color"] = Color.Parse("#FAF7F2");
        resources["Text200Color"] = Color.Parse("#F5EFE6");
        resources["Text300Color"] = Color.Parse("#E8DFD2");
        resources["Text400Color"] = Color.Parse("#D4C8B8");
        resources["Text500Color"] = Color.Parse("#B8A898");
        resources["Text600Color"] = Color.Parse("#8A7A6A");
        resources["Text700Color"] = Color.Parse("#5C5048");
        resources["Text800Color"] = Color.Parse("#3A3230");
        resources["Text900Color"] = Color.Parse("#1E1A18");

        // Background (Rich chocolate/coffee tinted)
        resources["Background100Color"] = Color.Parse("#8A7868");
        resources["Background200Color"] = Color.Parse("#78685A");
        resources["Background300Color"] = Color.Parse("#68584C");
        resources["Background400Color"] = Color.Parse("#584A40");
        resources["Background500Color"] = Color.Parse("#4A3E36");
        resources["Background600Color"] = Color.Parse("#3E342C");
        resources["Background700Color"] = Color.Parse("#342A24");
        resources["Background800Color"] = Color.Parse("#2A221C");
        resources["Background900Color"] = Color.Parse("#1E1814");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#8A7868");
        resources["Disabled200Color"] = Color.Parse("#78685A");
        resources["Disabled300Color"] = Color.Parse("#68584C");
        resources["Disabled400Color"] = Color.Parse("#584A40");
        resources["Disabled500Color"] = Color.Parse("#4A3E36");
        resources["Disabled600Color"] = Color.Parse("#3E342C");
        resources["Disabled700Color"] = Color.Parse("#342A24");
        resources["Disabled800Color"] = Color.Parse("#2A221C");
        resources["Disabled900Color"] = Color.Parse("#1E1814");

        // Divider (Warm brown-gray)
        resources["Divider100Color"] = Color.Parse("#F5EFE6");
        resources["Divider200Color"] = Color.Parse("#E8DFD2");
        resources["Divider300Color"] = Color.Parse("#D4C8B0");
        resources["Divider400Color"] = Color.Parse("#B8A088");
        resources["Divider500Color"] = Color.Parse("#8A7260");
        resources["Divider600Color"] = Color.Parse("#5C4C40");
        resources["Divider700Color"] = Color.Parse("#3A3028");
        resources["Divider800Color"] = Color.Parse("#28201A");
        resources["Divider900Color"] = Color.Parse("#18120E");

        // Icon (Coffee tinted)
        resources["Icon100Color"] = Color.Parse("#E7D4C0");
        resources["Icon200Color"] = Color.Parse("#D4BC9E");
        resources["Icon300Color"] = Color.Parse("#C4A882");
        resources["Icon400Color"] = Color.Parse("#A8876A");
        resources["Icon500Color"] = Color.Parse("#8B6F55");
        resources["Icon600Color"] = Color.Parse("#6F5842");
        resources["Icon700Color"] = Color.Parse("#574533");
        resources["Icon800Color"] = Color.Parse("#3F3225");
        resources["Icon900Color"] = Color.Parse("#2A2119");

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

        // Semantic Warning (Amber)
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
        resources["BlackColor"] = Color.Parse("#18120E");
        resources["WhiteColor"] = Color.Parse("#FAF7F2");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#2A221C");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#8B6F55");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#4A3E36");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#342A24");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#A8876A");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#68584C");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#8A7868");
        resources["MessageBoxInfoColor"] = Color.Parse("#38BDF8");
        resources["MessageBoxWarningColor"] = Color.Parse("#FBBF24");
        resources["MessageBoxErrorColor"] = Color.Parse("#F87171");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#A8876A");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#3E342C");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#2A2119");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#3F3225");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#A8876A");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#C4A882");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FAF7F2");
        resources["FooterGradientStartColor"] = Color.Parse("#1E1814");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FAF7F2");

        return resources;
    }
}
