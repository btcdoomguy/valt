using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Forest Canopy theme - Natural green earth tones dark theme
/// Grounded and professional aesthetic
/// </summary>
public static class ForestCanopyTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Forest Green)
        resources["Accent100Color"] = Color.Parse("#D1FAE5");
        resources["Accent200Color"] = Color.Parse("#A7F3D0");
        resources["Accent300Color"] = Color.Parse("#6EE7B7");
        resources["Accent400Color"] = Color.Parse("#34D399");
        resources["Accent500Color"] = Color.Parse("#10B981");
        resources["Accent600Color"] = Color.Parse("#059669");
        resources["Accent700Color"] = Color.Parse("#047857");
        resources["Accent800Color"] = Color.Parse("#065F46");
        resources["Accent900Color"] = Color.Parse("#064E3B");

        // Secondary (Earth Brown)
        resources["Secondary100Color"] = Color.Parse("#E7D8C9");
        resources["Secondary200Color"] = Color.Parse("#D4C4B0");
        resources["Secondary300Color"] = Color.Parse("#BCA88E");
        resources["Secondary400Color"] = Color.Parse("#A08B6C");
        resources["Secondary500Color"] = Color.Parse("#857050");
        resources["Secondary600Color"] = Color.Parse("#6B5A40");
        resources["Secondary700Color"] = Color.Parse("#524432");
        resources["Secondary800Color"] = Color.Parse("#3D3326");
        resources["Secondary900Color"] = Color.Parse("#2A231A");

        // Text (Warm off-white)
        resources["Text100Color"] = Color.Parse("#F5F5F0");
        resources["Text200Color"] = Color.Parse("#E8E8E0");
        resources["Text300Color"] = Color.Parse("#D5D5CA");
        resources["Text400Color"] = Color.Parse("#B8B8AC");
        resources["Text500Color"] = Color.Parse("#96968A");
        resources["Text600Color"] = Color.Parse("#72726A");
        resources["Text700Color"] = Color.Parse("#52524A");
        resources["Text800Color"] = Color.Parse("#363632");
        resources["Text900Color"] = Color.Parse("#1E1E1A");

        // Background (Deep forest green - visible green tint)
        resources["Background100Color"] = Color.Parse("#6A7D68");
        resources["Background200Color"] = Color.Parse("#566A55");
        resources["Background300Color"] = Color.Parse("#445644");
        resources["Background400Color"] = Color.Parse("#364536");
        resources["Background500Color"] = Color.Parse("#2C3A2C");
        resources["Background600Color"] = Color.Parse("#243024");
        resources["Background700Color"] = Color.Parse("#1E281E");
        resources["Background800Color"] = Color.Parse("#182018");
        resources["Background900Color"] = Color.Parse("#080C08");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#6A7D68");
        resources["Disabled200Color"] = Color.Parse("#566A55");
        resources["Disabled300Color"] = Color.Parse("#445644");
        resources["Disabled400Color"] = Color.Parse("#364536");
        resources["Disabled500Color"] = Color.Parse("#2C3A2C");
        resources["Disabled600Color"] = Color.Parse("#243024");
        resources["Disabled700Color"] = Color.Parse("#1E281E");
        resources["Disabled800Color"] = Color.Parse("#182018");
        resources["Disabled900Color"] = Color.Parse("#080C08");

        // Divider (Muted green-gray)
        resources["Divider100Color"] = Color.Parse("#D5D5CA");
        resources["Divider200Color"] = Color.Parse("#B8B8AC");
        resources["Divider300Color"] = Color.Parse("#96968A");
        resources["Divider400Color"] = Color.Parse("#72726A");
        resources["Divider500Color"] = Color.Parse("#52524A");
        resources["Divider600Color"] = Color.Parse("#363632");
        resources["Divider700Color"] = Color.Parse("#222A22");
        resources["Divider800Color"] = Color.Parse("#1A201A");
        resources["Divider900Color"] = Color.Parse("#0C100C");

        // Icon (Forest green tinted)
        resources["Icon100Color"] = Color.Parse("#D1FAE5");
        resources["Icon200Color"] = Color.Parse("#A7F3D0");
        resources["Icon300Color"] = Color.Parse("#6EE7B7");
        resources["Icon400Color"] = Color.Parse("#34D399");
        resources["Icon500Color"] = Color.Parse("#10B981");
        resources["Icon600Color"] = Color.Parse("#059669");
        resources["Icon700Color"] = Color.Parse("#047857");
        resources["Icon800Color"] = Color.Parse("#065F46");
        resources["Icon900Color"] = Color.Parse("#064E3B");

        // Semantic Info (Blue-green)
        resources["SemanticInfo100Color"] = Color.Parse("#99F6E4");
        resources["SemanticInfo200Color"] = Color.Parse("#5EEAD4");
        resources["SemanticInfo300Color"] = Color.Parse("#2DD4BF");
        resources["SemanticInfo400Color"] = Color.Parse("#14B8A6");
        resources["SemanticInfo500Color"] = Color.Parse("#0D9488");
        resources["SemanticInfo600Color"] = Color.Parse("#0F766E");
        resources["SemanticInfo700Color"] = Color.Parse("#115E59");
        resources["SemanticInfo800Color"] = Color.Parse("#134E4A");
        resources["SemanticInfo900Color"] = Color.Parse("#0A2A28");

        // Semantic Negative (Red)
        resources["SemanticNegative100Color"] = Color.Parse("#FECACA");
        resources["SemanticNegative200Color"] = Color.Parse("#FCA5A5");
        resources["SemanticNegative300Color"] = Color.Parse("#F87171");
        resources["SemanticNegative400Color"] = Color.Parse("#EF4444");
        resources["SemanticNegative500Color"] = Color.Parse("#DC2626");
        resources["SemanticNegative600Color"] = Color.Parse("#B91C1C");
        resources["SemanticNegative700Color"] = Color.Parse("#991B1B");
        resources["SemanticNegative800Color"] = Color.Parse("#7F1D1D");
        resources["SemanticNegative900Color"] = Color.Parse("#450A0A");

        // Semantic Positive (Bright Green)
        resources["SemanticPositive100Color"] = Color.Parse("#BBF7D0");
        resources["SemanticPositive200Color"] = Color.Parse("#86EFAC");
        resources["SemanticPositive300Color"] = Color.Parse("#4ADE80");
        resources["SemanticPositive400Color"] = Color.Parse("#22C55E");
        resources["SemanticPositive500Color"] = Color.Parse("#16A34A");
        resources["SemanticPositive600Color"] = Color.Parse("#15803D");
        resources["SemanticPositive700Color"] = Color.Parse("#166534");
        resources["SemanticPositive800Color"] = Color.Parse("#14532D");
        resources["SemanticPositive900Color"] = Color.Parse("#0A2915");

        // Semantic Special (Purple)
        resources["SemanticSpecial100Color"] = Color.Parse("#E9D5FF");
        resources["SemanticSpecial200Color"] = Color.Parse("#D8B4FE");
        resources["SemanticSpecial300Color"] = Color.Parse("#C084FC");
        resources["SemanticSpecial400Color"] = Color.Parse("#A855F7");
        resources["SemanticSpecial500Color"] = Color.Parse("#9333EA");
        resources["SemanticSpecial600Color"] = Color.Parse("#7E22CE");
        resources["SemanticSpecial700Color"] = Color.Parse("#6B21A8");
        resources["SemanticSpecial800Color"] = Color.Parse("#581C87");
        resources["SemanticSpecial900Color"] = Color.Parse("#2E0E46");

        // Semantic Warning (Yellow/Amber)
        resources["SemanticWarning100Color"] = Color.Parse("#FEF08A");
        resources["SemanticWarning200Color"] = Color.Parse("#FDE047");
        resources["SemanticWarning300Color"] = Color.Parse("#FACC15");
        resources["SemanticWarning400Color"] = Color.Parse("#EAB308");
        resources["SemanticWarning500Color"] = Color.Parse("#CA8A04");
        resources["SemanticWarning600Color"] = Color.Parse("#A16207");
        resources["SemanticWarning700Color"] = Color.Parse("#854D0E");
        resources["SemanticWarning800Color"] = Color.Parse("#713F12");
        resources["SemanticWarning900Color"] = Color.Parse("#3D2208");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#060806");
        resources["WhiteColor"] = Color.Parse("#F5F5F0");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#182018");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#059669");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#2C3A2C");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#1E281E");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#10B981");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#445644");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#6A7D68");
        resources["MessageBoxInfoColor"] = Color.Parse("#14B8A6");
        resources["MessageBoxWarningColor"] = Color.Parse("#EAB308");
        resources["MessageBoxErrorColor"] = Color.Parse("#EF4444");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#34D399");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#243024");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#064E3B");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#065F46");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#34D399");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#6EE7B7");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#F5F5F0");
        resources["FooterGradientStartColor"] = Color.Parse("#080C08");
        resources["IconSelectorDefaultColor"] = Color.Parse("#F5F5F0");

        return resources;
    }
}
