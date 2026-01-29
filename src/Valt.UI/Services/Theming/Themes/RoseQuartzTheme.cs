using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Rose Quartz theme - Soft pink/rose/magenta accent dark theme
/// Elegant and warm aesthetic
/// </summary>
public static class RoseQuartzTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Rose/Pink)
        resources["Accent100Color"] = Color.Parse("#FECDD3");
        resources["Accent200Color"] = Color.Parse("#FBB6C4");
        resources["Accent300Color"] = Color.Parse("#F687A5");
        resources["Accent400Color"] = Color.Parse("#EC4889");
        resources["Accent500Color"] = Color.Parse("#DB2777");
        resources["Accent600Color"] = Color.Parse("#BE185D");
        resources["Accent700Color"] = Color.Parse("#9D174D");
        resources["Accent800Color"] = Color.Parse("#831843");
        resources["Accent900Color"] = Color.Parse("#500724");

        // Secondary (Teal/Seafoam - complementary to rose)
        resources["Secondary100Color"] = Color.Parse("#CCFBF1");
        resources["Secondary200Color"] = Color.Parse("#99F6E4");
        resources["Secondary300Color"] = Color.Parse("#5EEAD4");
        resources["Secondary400Color"] = Color.Parse("#2DD4BF");
        resources["Secondary500Color"] = Color.Parse("#14B8A6");
        resources["Secondary600Color"] = Color.Parse("#0D9488");
        resources["Secondary700Color"] = Color.Parse("#0F766E");
        resources["Secondary800Color"] = Color.Parse("#115E59");
        resources["Secondary900Color"] = Color.Parse("#134E4A");

        // Text (Warm rose-tinted white)
        resources["Text100Color"] = Color.Parse("#FFF1F2");
        resources["Text200Color"] = Color.Parse("#FFE4E6");
        resources["Text300Color"] = Color.Parse("#FECDD3");
        resources["Text400Color"] = Color.Parse("#E8BDC2");
        resources["Text500Color"] = Color.Parse("#C9A0A8");
        resources["Text600Color"] = Color.Parse("#9A7580");
        resources["Text700Color"] = Color.Parse("#6B5058");
        resources["Text800Color"] = Color.Parse("#3D2E32");
        resources["Text900Color"] = Color.Parse("#1F1718");

        // Background (Deep rose-tinted - visible pink tint)
        resources["Background100Color"] = Color.Parse("#9A7A85");
        resources["Background200Color"] = Color.Parse("#886878");
        resources["Background300Color"] = Color.Parse("#765868");
        resources["Background400Color"] = Color.Parse("#644858");
        resources["Background500Color"] = Color.Parse("#543C4A");
        resources["Background600Color"] = Color.Parse("#46323E");
        resources["Background700Color"] = Color.Parse("#3A2832");
        resources["Background800Color"] = Color.Parse("#302028");
        resources["Background900Color"] = Color.Parse("#22161C");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#9A7A85");
        resources["Disabled200Color"] = Color.Parse("#886878");
        resources["Disabled300Color"] = Color.Parse("#765868");
        resources["Disabled400Color"] = Color.Parse("#644858");
        resources["Disabled500Color"] = Color.Parse("#543C4A");
        resources["Disabled600Color"] = Color.Parse("#46323E");
        resources["Disabled700Color"] = Color.Parse("#3A2832");
        resources["Disabled800Color"] = Color.Parse("#302028");
        resources["Disabled900Color"] = Color.Parse("#22161C");

        // Divider (Rose-tinted gray)
        resources["Divider100Color"] = Color.Parse("#FFE4E6");
        resources["Divider200Color"] = Color.Parse("#FECDD3");
        resources["Divider300Color"] = Color.Parse("#E8B0B8");
        resources["Divider400Color"] = Color.Parse("#B8808A");
        resources["Divider500Color"] = Color.Parse("#8A5A62");
        resources["Divider600Color"] = Color.Parse("#5C3A40");
        resources["Divider700Color"] = Color.Parse("#3A2428");
        resources["Divider800Color"] = Color.Parse("#281A1E");
        resources["Divider900Color"] = Color.Parse("#180E12");

        // Icon (Teal/Seafoam - complementary to rose)
        resources["Icon100Color"] = Color.Parse("#CCFBF1");
        resources["Icon200Color"] = Color.Parse("#99F6E4");
        resources["Icon300Color"] = Color.Parse("#5EEAD4");
        resources["Icon400Color"] = Color.Parse("#2DD4BF");
        resources["Icon500Color"] = Color.Parse("#14B8A6");
        resources["Icon600Color"] = Color.Parse("#0D9488");
        resources["Icon700Color"] = Color.Parse("#0F766E");
        resources["Icon800Color"] = Color.Parse("#115E59");
        resources["Icon900Color"] = Color.Parse("#134E4A");

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
        resources["BlackColor"] = Color.Parse("#180E12");
        resources["WhiteColor"] = Color.Parse("#FFF1F2");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#302028");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#DB2777");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#543C4A");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#3A2832");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#EC4889");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#765868");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#9A7A85");
        resources["MessageBoxInfoColor"] = Color.Parse("#38BDF8");
        resources["MessageBoxWarningColor"] = Color.Parse("#FBBF24");
        resources["MessageBoxErrorColor"] = Color.Parse("#F87171");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#EC4889");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#46323E");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#500724");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#831843");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#EC4889");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#F687A5");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFF1F2");
        resources["FooterGradientStartColor"] = Color.Parse("#22161C");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFF1F2");

        return resources;
    }
}
