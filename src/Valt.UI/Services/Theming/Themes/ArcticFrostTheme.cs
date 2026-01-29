using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Arctic Frost theme - Cool crisp blue-white dark theme
/// Clean and modern aesthetic
/// </summary>
public static class ArcticFrostTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Ice Blue/Cyan)
        resources["Accent100Color"] = Color.Parse("#E0F2FE");
        resources["Accent200Color"] = Color.Parse("#BAE6FD");
        resources["Accent300Color"] = Color.Parse("#7DD3FC");
        resources["Accent400Color"] = Color.Parse("#38BDF8");
        resources["Accent500Color"] = Color.Parse("#0EA5E9");
        resources["Accent600Color"] = Color.Parse("#0284C7");
        resources["Accent700Color"] = Color.Parse("#0369A1");
        resources["Accent800Color"] = Color.Parse("#075985");
        resources["Accent900Color"] = Color.Parse("#0C4A6E");

        // Secondary (Coral/Peach - complementary to ice blue)
        resources["Secondary100Color"] = Color.Parse("#FFEDD5");
        resources["Secondary200Color"] = Color.Parse("#FED7AA");
        resources["Secondary300Color"] = Color.Parse("#FDBA74");
        resources["Secondary400Color"] = Color.Parse("#FB923C");
        resources["Secondary500Color"] = Color.Parse("#F97316");
        resources["Secondary600Color"] = Color.Parse("#EA580C");
        resources["Secondary700Color"] = Color.Parse("#C2410C");
        resources["Secondary800Color"] = Color.Parse("#9A3412");
        resources["Secondary900Color"] = Color.Parse("#7C2D12");

        // Text (Cool blue-white)
        resources["Text100Color"] = Color.Parse("#F8FAFC");
        resources["Text200Color"] = Color.Parse("#F1F5F9");
        resources["Text300Color"] = Color.Parse("#E2E8F0");
        resources["Text400Color"] = Color.Parse("#CBD5E1");
        resources["Text500Color"] = Color.Parse("#94A3B8");
        resources["Text600Color"] = Color.Parse("#64748B");
        resources["Text700Color"] = Color.Parse("#475569");
        resources["Text800Color"] = Color.Parse("#334155");
        resources["Text900Color"] = Color.Parse("#1E293B");

        // Background (Cool blue-slate - visible blue tint)
        resources["Background100Color"] = Color.Parse("#7B8AA0");
        resources["Background200Color"] = Color.Parse("#6B7B90");
        resources["Background300Color"] = Color.Parse("#5A6B80");
        resources["Background400Color"] = Color.Parse("#4A5C70");
        resources["Background500Color"] = Color.Parse("#3E4F62");
        resources["Background600Color"] = Color.Parse("#334455");
        resources["Background700Color"] = Color.Parse("#2A3A4A");
        resources["Background800Color"] = Color.Parse("#223040");
        resources["Background900Color"] = Color.Parse("#182230");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#7B8AA0");
        resources["Disabled200Color"] = Color.Parse("#6B7B90");
        resources["Disabled300Color"] = Color.Parse("#5A6B80");
        resources["Disabled400Color"] = Color.Parse("#4A5C70");
        resources["Disabled500Color"] = Color.Parse("#3E4F62");
        resources["Disabled600Color"] = Color.Parse("#334455");
        resources["Disabled700Color"] = Color.Parse("#2A3A4A");
        resources["Disabled800Color"] = Color.Parse("#223040");
        resources["Disabled900Color"] = Color.Parse("#182230");

        // Divider (Slate)
        resources["Divider100Color"] = Color.Parse("#E2E8F0");
        resources["Divider200Color"] = Color.Parse("#CBD5E1");
        resources["Divider300Color"] = Color.Parse("#94A3B8");
        resources["Divider400Color"] = Color.Parse("#64748B");
        resources["Divider500Color"] = Color.Parse("#475569");
        resources["Divider600Color"] = Color.Parse("#334155");
        resources["Divider700Color"] = Color.Parse("#1E293B");
        resources["Divider800Color"] = Color.Parse("#172033");
        resources["Divider900Color"] = Color.Parse("#0D1320");

        // Icon (Coral/Peach - complementary to ice blue)
        resources["Icon100Color"] = Color.Parse("#FFEDD5");
        resources["Icon200Color"] = Color.Parse("#FED7AA");
        resources["Icon300Color"] = Color.Parse("#FDBA74");
        resources["Icon400Color"] = Color.Parse("#FB923C");
        resources["Icon500Color"] = Color.Parse("#F97316");
        resources["Icon600Color"] = Color.Parse("#EA580C");
        resources["Icon700Color"] = Color.Parse("#C2410C");
        resources["Icon800Color"] = Color.Parse("#9A3412");
        resources["Icon900Color"] = Color.Parse("#7C2D12");

        // Semantic Info (Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#93C5FD");
        resources["SemanticInfo200Color"] = Color.Parse("#60A5FA");
        resources["SemanticInfo300Color"] = Color.Parse("#3B82F6");
        resources["SemanticInfo400Color"] = Color.Parse("#2563EB");
        resources["SemanticInfo500Color"] = Color.Parse("#1D4ED8");
        resources["SemanticInfo600Color"] = Color.Parse("#1E40AF");
        resources["SemanticInfo700Color"] = Color.Parse("#1E3A8A");
        resources["SemanticInfo800Color"] = Color.Parse("#172554");
        resources["SemanticInfo900Color"] = Color.Parse("#0C1426");

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

        // Semantic Positive (Green)
        resources["SemanticPositive100Color"] = Color.Parse("#86EFAC");
        resources["SemanticPositive200Color"] = Color.Parse("#4ADE80");
        resources["SemanticPositive300Color"] = Color.Parse("#22C55E");
        resources["SemanticPositive400Color"] = Color.Parse("#16A34A");
        resources["SemanticPositive500Color"] = Color.Parse("#15803D");
        resources["SemanticPositive600Color"] = Color.Parse("#166534");
        resources["SemanticPositive700Color"] = Color.Parse("#14532D");
        resources["SemanticPositive800Color"] = Color.Parse("#134425");
        resources["SemanticPositive900Color"] = Color.Parse("#0A2915");

        // Semantic Special (Purple)
        resources["SemanticSpecial100Color"] = Color.Parse("#C4B5FD");
        resources["SemanticSpecial200Color"] = Color.Parse("#A78BFA");
        resources["SemanticSpecial300Color"] = Color.Parse("#8B5CF6");
        resources["SemanticSpecial400Color"] = Color.Parse("#7C3AED");
        resources["SemanticSpecial500Color"] = Color.Parse("#6D28D9");
        resources["SemanticSpecial600Color"] = Color.Parse("#5B21B6");
        resources["SemanticSpecial700Color"] = Color.Parse("#4C1D95");
        resources["SemanticSpecial800Color"] = Color.Parse("#3B1578");
        resources["SemanticSpecial900Color"] = Color.Parse("#1E0A3E");

        // Semantic Warning (Yellow/Amber)
        resources["SemanticWarning100Color"] = Color.Parse("#FDE68A");
        resources["SemanticWarning200Color"] = Color.Parse("#FCD34D");
        resources["SemanticWarning300Color"] = Color.Parse("#FBBF24");
        resources["SemanticWarning400Color"] = Color.Parse("#F59E0B");
        resources["SemanticWarning500Color"] = Color.Parse("#D97706");
        resources["SemanticWarning600Color"] = Color.Parse("#B45309");
        resources["SemanticWarning700Color"] = Color.Parse("#92400E");
        resources["SemanticWarning800Color"] = Color.Parse("#78350F");
        resources["SemanticWarning900Color"] = Color.Parse("#451A03");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#04060B");
        resources["WhiteColor"] = Color.Parse("#F8FAFC");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#223040");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#0284C7");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#3E4F62");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#2A3A4A");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#0EA5E9");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#5A6B80");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#7B8AA0");
        resources["MessageBoxInfoColor"] = Color.Parse("#3B82F6");
        resources["MessageBoxWarningColor"] = Color.Parse("#F59E0B");
        resources["MessageBoxErrorColor"] = Color.Parse("#EF4444");
        resources["MessageBoxQuestionColor"] = Color.Parse("#8B5CF6");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#38BDF8");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#334455");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#0C4A6E");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#075985");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#38BDF8");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#7DD3FC");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#F8FAFC");
        resources["FooterGradientStartColor"] = Color.Parse("#182230");
        resources["IconSelectorDefaultColor"] = Color.Parse("#F8FAFC");

        return resources;
    }
}
