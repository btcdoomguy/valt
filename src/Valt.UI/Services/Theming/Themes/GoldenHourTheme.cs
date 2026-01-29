using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Golden Hour theme - Warm amber/gold dark theme
/// Inspired by Bitcoin's "digital gold" identity
/// </summary>
public static class GoldenHourTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Gold/Amber)
        resources["Accent100Color"] = Color.Parse("#FEF3C7");
        resources["Accent200Color"] = Color.Parse("#FDE68A");
        resources["Accent300Color"] = Color.Parse("#FCD34D");
        resources["Accent400Color"] = Color.Parse("#FBBF24");
        resources["Accent500Color"] = Color.Parse("#F59E0B");
        resources["Accent600Color"] = Color.Parse("#D97706");
        resources["Accent700Color"] = Color.Parse("#B45309");
        resources["Accent800Color"] = Color.Parse("#92400E");
        resources["Accent900Color"] = Color.Parse("#78350F");

        // Secondary (Blue/Indigo - complementary to gold)
        resources["Secondary100Color"] = Color.Parse("#D0DEF1");
        resources["Secondary200Color"] = Color.Parse("#B5D0F1");
        resources["Secondary300Color"] = Color.Parse("#8CBBF0");
        resources["Secondary400Color"] = Color.Parse("#5B9DEE");
        resources["Secondary500Color"] = Color.Parse("#387BE9");
        resources["Secondary600Color"] = Color.Parse("#235EDF");
        resources["Secondary700Color"] = Color.Parse("#1C4ACD");
        resources["Secondary800Color"] = Color.Parse("#1C3DA6");
        resources["Secondary900Color"] = Color.Parse("#1C3783");

        // Text (Warm cream-tinted)
        resources["Text100Color"] = Color.Parse("#FFFBEB");
        resources["Text200Color"] = Color.Parse("#FEF3DC");
        resources["Text300Color"] = Color.Parse("#F5E6C8");
        resources["Text400Color"] = Color.Parse("#E2D1AE");
        resources["Text500Color"] = Color.Parse("#C4B08A");
        resources["Text600Color"] = Color.Parse("#9A8A6A");
        resources["Text700Color"] = Color.Parse("#6B604B");
        resources["Text800Color"] = Color.Parse("#453D30");
        resources["Text900Color"] = Color.Parse("#261F18");

        // Background (Warm amber-brown - visible warm tint)
        resources["Background100Color"] = Color.Parse("#9A8570");
        resources["Background200Color"] = Color.Parse("#887360");
        resources["Background300Color"] = Color.Parse("#766250");
        resources["Background400Color"] = Color.Parse("#645042");
        resources["Background500Color"] = Color.Parse("#544236");
        resources["Background600Color"] = Color.Parse("#46382C");
        resources["Background700Color"] = Color.Parse("#3A2E24");
        resources["Background800Color"] = Color.Parse("#30261E");
        resources["Background900Color"] = Color.Parse("#221A14");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#9A8570");
        resources["Disabled200Color"] = Color.Parse("#887360");
        resources["Disabled300Color"] = Color.Parse("#766250");
        resources["Disabled400Color"] = Color.Parse("#645042");
        resources["Disabled500Color"] = Color.Parse("#544236");
        resources["Disabled600Color"] = Color.Parse("#46382C");
        resources["Disabled700Color"] = Color.Parse("#3A2E24");
        resources["Disabled800Color"] = Color.Parse("#30261E");
        resources["Disabled900Color"] = Color.Parse("#221A14");

        // Divider (Warm gray)
        resources["Divider100Color"] = Color.Parse("#F5E6C8");
        resources["Divider200Color"] = Color.Parse("#E2D1AE");
        resources["Divider300Color"] = Color.Parse("#C4B08A");
        resources["Divider400Color"] = Color.Parse("#9A8A6A");
        resources["Divider500Color"] = Color.Parse("#6B604B");
        resources["Divider600Color"] = Color.Parse("#453D30");
        resources["Divider700Color"] = Color.Parse("#2A231C");
        resources["Divider800Color"] = Color.Parse("#1F1A15");
        resources["Divider900Color"] = Color.Parse("#100D0A");

        // Icon (Blue/Indigo - complementary to gold)
        resources["Icon100Color"] = Color.Parse("#DBEAFE");
        resources["Icon200Color"] = Color.Parse("#BFDBFE");
        resources["Icon300Color"] = Color.Parse("#93C5FD");
        resources["Icon400Color"] = Color.Parse("#60A5FA");
        resources["Icon500Color"] = Color.Parse("#3B82F6");
        resources["Icon600Color"] = Color.Parse("#2563EB");
        resources["Icon700Color"] = Color.Parse("#1D4ED8");
        resources["Icon800Color"] = Color.Parse("#1E40AF");
        resources["Icon900Color"] = Color.Parse("#1E3A8A");

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
        resources["SemanticNegative700Color"] = Color.Parse("#822727");
        resources["SemanticNegative800Color"] = Color.Parse("#63171B");
        resources["SemanticNegative900Color"] = Color.Parse("#3B0D0F");

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

        // Semantic Warning (Orange)
        resources["SemanticWarning100Color"] = Color.Parse("#FED7AA");
        resources["SemanticWarning200Color"] = Color.Parse("#FDBA74");
        resources["SemanticWarning300Color"] = Color.Parse("#FB923C");
        resources["SemanticWarning400Color"] = Color.Parse("#F97316");
        resources["SemanticWarning500Color"] = Color.Parse("#EA580C");
        resources["SemanticWarning600Color"] = Color.Parse("#C2410C");
        resources["SemanticWarning700Color"] = Color.Parse("#9A3412");
        resources["SemanticWarning800Color"] = Color.Parse("#7C2D12");
        resources["SemanticWarning900Color"] = Color.Parse("#431407");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#0A0806");
        resources["WhiteColor"] = Color.Parse("#FFFBEB");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#30261E");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#D97706");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#544236");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#3A2E24");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#F59E0B");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#766250");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#9A8570");
        resources["MessageBoxInfoColor"] = Color.Parse("#4299E1");
        resources["MessageBoxWarningColor"] = Color.Parse("#F97316");
        resources["MessageBoxErrorColor"] = Color.Parse("#E53E3E");
        resources["MessageBoxQuestionColor"] = Color.Parse("#805AD5");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#FBBF24");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#46382C");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#78350F");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#92400E");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#FBBF24");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#FCD34D");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFFBEB");
        resources["FooterGradientStartColor"] = Color.Parse("#221A14");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFFBEB");

        return resources;
    }
}
