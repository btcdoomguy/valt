using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Sunset Blaze theme - Warm orange/coral sunset accent dark theme
/// Vibrant and energetic aesthetic
/// </summary>
public static class SunsetBlazeTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Orange/Coral)
        resources["Accent100Color"] = Color.Parse("#FFEDD5");
        resources["Accent200Color"] = Color.Parse("#FED7AA");
        resources["Accent300Color"] = Color.Parse("#FDBA74");
        resources["Accent400Color"] = Color.Parse("#FB923C");
        resources["Accent500Color"] = Color.Parse("#F97316");
        resources["Accent600Color"] = Color.Parse("#EA580C");
        resources["Accent700Color"] = Color.Parse("#C2410C");
        resources["Accent800Color"] = Color.Parse("#9A3412");
        resources["Accent900Color"] = Color.Parse("#7C2D12");

        // Secondary (Blue/Cyan - complementary to orange)
        resources["Secondary100Color"] = Color.Parse("#CFFAFE");
        resources["Secondary200Color"] = Color.Parse("#A5F3FC");
        resources["Secondary300Color"] = Color.Parse("#67E8F9");
        resources["Secondary400Color"] = Color.Parse("#22D3EE");
        resources["Secondary500Color"] = Color.Parse("#06B6D4");
        resources["Secondary600Color"] = Color.Parse("#0891B2");
        resources["Secondary700Color"] = Color.Parse("#0E7490");
        resources["Secondary800Color"] = Color.Parse("#155E75");
        resources["Secondary900Color"] = Color.Parse("#164E63");

        // Text (Warm cream-tinted)
        resources["Text100Color"] = Color.Parse("#FFFBEB");
        resources["Text200Color"] = Color.Parse("#FEF3E2");
        resources["Text300Color"] = Color.Parse("#FDE6C8");
        resources["Text400Color"] = Color.Parse("#E8D0AE");
        resources["Text500Color"] = Color.Parse("#C9B08A");
        resources["Text600Color"] = Color.Parse("#9A856A");
        resources["Text700Color"] = Color.Parse("#6B5A4B");
        resources["Text800Color"] = Color.Parse("#3D3430");
        resources["Text900Color"] = Color.Parse("#1F1A18");

        // Background (Warm orange-brown tinted - visible warmth)
        resources["Background100Color"] = Color.Parse("#9A8072");
        resources["Background200Color"] = Color.Parse("#886E62");
        resources["Background300Color"] = Color.Parse("#765E54");
        resources["Background400Color"] = Color.Parse("#644E46");
        resources["Background500Color"] = Color.Parse("#54403A");
        resources["Background600Color"] = Color.Parse("#46342E");
        resources["Background700Color"] = Color.Parse("#3A2A24");
        resources["Background800Color"] = Color.Parse("#30221C");
        resources["Background900Color"] = Color.Parse("#221814");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#9A8072");
        resources["Disabled200Color"] = Color.Parse("#886E62");
        resources["Disabled300Color"] = Color.Parse("#765E54");
        resources["Disabled400Color"] = Color.Parse("#644E46");
        resources["Disabled500Color"] = Color.Parse("#54403A");
        resources["Disabled600Color"] = Color.Parse("#46342E");
        resources["Disabled700Color"] = Color.Parse("#3A2A24");
        resources["Disabled800Color"] = Color.Parse("#30221C");
        resources["Disabled900Color"] = Color.Parse("#221814");

        // Divider (Warm orange-gray)
        resources["Divider100Color"] = Color.Parse("#FEF3E2");
        resources["Divider200Color"] = Color.Parse("#FDE6C8");
        resources["Divider300Color"] = Color.Parse("#E8C8A8");
        resources["Divider400Color"] = Color.Parse("#B89878");
        resources["Divider500Color"] = Color.Parse("#8A6A52");
        resources["Divider600Color"] = Color.Parse("#5C4438");
        resources["Divider700Color"] = Color.Parse("#3A2A22");
        resources["Divider800Color"] = Color.Parse("#281C16");
        resources["Divider900Color"] = Color.Parse("#180E0C");

        // Icon (Blue/Cyan - complementary to orange)
        resources["Icon100Color"] = Color.Parse("#CFFAFE");
        resources["Icon200Color"] = Color.Parse("#A5F3FC");
        resources["Icon300Color"] = Color.Parse("#67E8F9");
        resources["Icon400Color"] = Color.Parse("#22D3EE");
        resources["Icon500Color"] = Color.Parse("#06B6D4");
        resources["Icon600Color"] = Color.Parse("#0891B2");
        resources["Icon700Color"] = Color.Parse("#0E7490");
        resources["Icon800Color"] = Color.Parse("#155E75");
        resources["Icon900Color"] = Color.Parse("#164E63");

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

        // Semantic Warning (Yellow)
        resources["SemanticWarning100Color"] = Color.Parse("#FEF9C3");
        resources["SemanticWarning200Color"] = Color.Parse("#FEF08A");
        resources["SemanticWarning300Color"] = Color.Parse("#FDE047");
        resources["SemanticWarning400Color"] = Color.Parse("#FACC15");
        resources["SemanticWarning500Color"] = Color.Parse("#EAB308");
        resources["SemanticWarning600Color"] = Color.Parse("#CA8A04");
        resources["SemanticWarning700Color"] = Color.Parse("#A16207");
        resources["SemanticWarning800Color"] = Color.Parse("#854D0E");
        resources["SemanticWarning900Color"] = Color.Parse("#713F12");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#180E0C");
        resources["WhiteColor"] = Color.Parse("#FFFBEB");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#30221C");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#F97316");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#54403A");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#3A2A24");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#FB923C");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#765E54");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#9A8072");
        resources["MessageBoxInfoColor"] = Color.Parse("#38BDF8");
        resources["MessageBoxWarningColor"] = Color.Parse("#FACC15");
        resources["MessageBoxErrorColor"] = Color.Parse("#F87171");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#FB923C");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#46342E");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#7C2D12");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#9A3412");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#FB923C");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#FDBA74");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFFBEB");
        resources["FooterGradientStartColor"] = Color.Parse("#221814");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFFBEB");

        return resources;
    }
}
