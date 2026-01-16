using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Crimson Ember theme - Deep red/rose dark theme with burgundy-tinted backgrounds
/// </summary>
public static class CrimsonEmberTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Crimson/Rose)
        resources["Accent100Color"] = Color.Parse("#FECDD3");
        resources["Accent200Color"] = Color.Parse("#FDA4AF");
        resources["Accent300Color"] = Color.Parse("#FB7185");
        resources["Accent400Color"] = Color.Parse("#F43F5E");
        resources["Accent500Color"] = Color.Parse("#E11D48");
        resources["Accent600Color"] = Color.Parse("#BE123C");
        resources["Accent700Color"] = Color.Parse("#9F1239");
        resources["Accent800Color"] = Color.Parse("#881337");
        resources["Accent900Color"] = Color.Parse("#4C0519");

        // Secondary (Amber/Gold)
        resources["Secondary100Color"] = Color.Parse("#FEF3C7");
        resources["Secondary200Color"] = Color.Parse("#FDE68A");
        resources["Secondary300Color"] = Color.Parse("#FCD34D");
        resources["Secondary400Color"] = Color.Parse("#FBBF24");
        resources["Secondary500Color"] = Color.Parse("#F59E0B");
        resources["Secondary600Color"] = Color.Parse("#D97706");
        resources["Secondary700Color"] = Color.Parse("#B45309");
        resources["Secondary800Color"] = Color.Parse("#92400E");
        resources["Secondary900Color"] = Color.Parse("#451A03");

        // Text (Warm rose-gray)
        resources["Text100Color"] = Color.Parse("#FFF1F2");
        resources["Text200Color"] = Color.Parse("#FFE4E6");
        resources["Text300Color"] = Color.Parse("#FECDD3");
        resources["Text400Color"] = Color.Parse("#E8C0C5");
        resources["Text500Color"] = Color.Parse("#C9A0A5");
        resources["Text600Color"] = Color.Parse("#9A7578");
        resources["Text700Color"] = Color.Parse("#6B5052");
        resources["Text800Color"] = Color.Parse("#3D2E2F");
        resources["Text900Color"] = Color.Parse("#1F1718");

        // Background (Rich burgundy/crimson tinted - visible color)
        resources["Background100Color"] = Color.Parse("#9E5A5A");
        resources["Background200Color"] = Color.Parse("#8A4848");
        resources["Background300Color"] = Color.Parse("#723838");
        resources["Background400Color"] = Color.Parse("#5C2A2A");
        resources["Background500Color"] = Color.Parse("#4A2020");
        resources["Background600Color"] = Color.Parse("#3C1818");
        resources["Background700Color"] = Color.Parse("#301414");
        resources["Background800Color"] = Color.Parse("#261010");
        resources["Background900Color"] = Color.Parse("#1C0C0C");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#9E5A5A");
        resources["Disabled200Color"] = Color.Parse("#8A4848");
        resources["Disabled300Color"] = Color.Parse("#723838");
        resources["Disabled400Color"] = Color.Parse("#5C2A2A");
        resources["Disabled500Color"] = Color.Parse("#4A2020");
        resources["Disabled600Color"] = Color.Parse("#3C1818");
        resources["Disabled700Color"] = Color.Parse("#301414");
        resources["Disabled800Color"] = Color.Parse("#261010");
        resources["Disabled900Color"] = Color.Parse("#1C0C0C");

        // Divider (Brighter rose-tinted for high contrast borders)
        resources["Divider100Color"] = Color.Parse("#FFF1F2");
        resources["Divider200Color"] = Color.Parse("#FFE4E6");
        resources["Divider300Color"] = Color.Parse("#FECDD3");
        resources["Divider400Color"] = Color.Parse("#E8B0B8");
        resources["Divider500Color"] = Color.Parse("#B8808A");
        resources["Divider600Color"] = Color.Parse("#8A5A62");
        resources["Divider700Color"] = Color.Parse("#5C3A40");
        resources["Divider800Color"] = Color.Parse("#3A2428");
        resources["Divider900Color"] = Color.Parse("#201416");

        // Icon (Crimson tinted)
        resources["Icon100Color"] = Color.Parse("#FECDD3");
        resources["Icon200Color"] = Color.Parse("#FDA4AF");
        resources["Icon300Color"] = Color.Parse("#FB7185");
        resources["Icon400Color"] = Color.Parse("#F43F5E");
        resources["Icon500Color"] = Color.Parse("#E11D48");
        resources["Icon600Color"] = Color.Parse("#BE123C");
        resources["Icon700Color"] = Color.Parse("#9F1239");
        resources["Icon800Color"] = Color.Parse("#881337");
        resources["Icon900Color"] = Color.Parse("#4C0519");

        // Semantic Info (Sky Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#BAE6FD");
        resources["SemanticInfo200Color"] = Color.Parse("#7DD3FC");
        resources["SemanticInfo300Color"] = Color.Parse("#38BDF8");
        resources["SemanticInfo400Color"] = Color.Parse("#0EA5E9");
        resources["SemanticInfo500Color"] = Color.Parse("#0284C7");
        resources["SemanticInfo600Color"] = Color.Parse("#0369A1");
        resources["SemanticInfo700Color"] = Color.Parse("#075985");
        resources["SemanticInfo800Color"] = Color.Parse("#0C4A6E");
        resources["SemanticInfo900Color"] = Color.Parse("#082F49");

        // Semantic Negative (Deep Red)
        resources["SemanticNegative100Color"] = Color.Parse("#FEE2E2");
        resources["SemanticNegative200Color"] = Color.Parse("#FECACA");
        resources["SemanticNegative300Color"] = Color.Parse("#FCA5A5");
        resources["SemanticNegative400Color"] = Color.Parse("#F87171");
        resources["SemanticNegative500Color"] = Color.Parse("#EF4444");
        resources["SemanticNegative600Color"] = Color.Parse("#DC2626");
        resources["SemanticNegative700Color"] = Color.Parse("#B91C1C");
        resources["SemanticNegative800Color"] = Color.Parse("#991B1B");
        resources["SemanticNegative900Color"] = Color.Parse("#7F1D1D");

        // Semantic Positive (Emerald Green)
        resources["SemanticPositive100Color"] = Color.Parse("#D1FAE5");
        resources["SemanticPositive200Color"] = Color.Parse("#A7F3D0");
        resources["SemanticPositive300Color"] = Color.Parse("#6EE7B7");
        resources["SemanticPositive400Color"] = Color.Parse("#34D399");
        resources["SemanticPositive500Color"] = Color.Parse("#10B981");
        resources["SemanticPositive600Color"] = Color.Parse("#059669");
        resources["SemanticPositive700Color"] = Color.Parse("#047857");
        resources["SemanticPositive800Color"] = Color.Parse("#065F46");
        resources["SemanticPositive900Color"] = Color.Parse("#064E3B");

        // Semantic Special (Violet)
        resources["SemanticSpecial100Color"] = Color.Parse("#EDE9FE");
        resources["SemanticSpecial200Color"] = Color.Parse("#DDD6FE");
        resources["SemanticSpecial300Color"] = Color.Parse("#C4B5FD");
        resources["SemanticSpecial400Color"] = Color.Parse("#A78BFA");
        resources["SemanticSpecial500Color"] = Color.Parse("#8B5CF6");
        resources["SemanticSpecial600Color"] = Color.Parse("#7C3AED");
        resources["SemanticSpecial700Color"] = Color.Parse("#6D28D9");
        resources["SemanticSpecial800Color"] = Color.Parse("#5B21B6");
        resources["SemanticSpecial900Color"] = Color.Parse("#4C1D95");

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
        resources["BlackColor"] = Color.Parse("#1C0C0C");
        resources["WhiteColor"] = Color.Parse("#FFF1F2");

        // UI Element Colors (high contrast)
        resources["LiveRatesBackgroundColor"] = Color.Parse("#261010");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#E11D48");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#5C2A2A");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#301414");
        resources["ButtonOverlayLightColor"] = Color.Parse("#50FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#30FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#F43F5E");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#8A4848");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#B8808A");
        resources["MessageBoxInfoColor"] = Color.Parse("#38BDF8");
        resources["MessageBoxWarningColor"] = Color.Parse("#FBBF24");
        resources["MessageBoxErrorColor"] = Color.Parse("#F87171");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A78BFA");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#FB7185");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#3C1818");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#4C0519");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#9F1239");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#FB7185");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#FDA4AF");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFF1F2");
        resources["FooterGradientStartColor"] = Color.Parse("#1C0C0C");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFF1F2");

        return resources;
    }
}
