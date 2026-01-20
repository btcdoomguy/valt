using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Monochrome theme - Pure black, white, and gray tones only
/// </summary>
public static class MonochromeTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Pure white to black gradient)
        resources["Accent100Color"] = Color.Parse("#FFFFFF");
        resources["Accent200Color"] = Color.Parse("#E5E5E5");
        resources["Accent300Color"] = Color.Parse("#CCCCCC");
        resources["Accent400Color"] = Color.Parse("#B3B3B3");
        resources["Accent500Color"] = Color.Parse("#999999");
        resources["Accent600Color"] = Color.Parse("#808080");
        resources["Accent700Color"] = Color.Parse("#666666");
        resources["Accent800Color"] = Color.Parse("#4D4D4D");
        resources["Accent900Color"] = Color.Parse("#333333");

        // Secondary (Same neutral gray scale)
        resources["Secondary100Color"] = Color.Parse("#F5F5F5");
        resources["Secondary200Color"] = Color.Parse("#E0E0E0");
        resources["Secondary300Color"] = Color.Parse("#BDBDBD");
        resources["Secondary400Color"] = Color.Parse("#9E9E9E");
        resources["Secondary500Color"] = Color.Parse("#757575");
        resources["Secondary600Color"] = Color.Parse("#616161");
        resources["Secondary700Color"] = Color.Parse("#424242");
        resources["Secondary800Color"] = Color.Parse("#303030");
        resources["Secondary900Color"] = Color.Parse("#212121");

        // Text (Pure neutral gray)
        resources["Text100Color"] = Color.Parse("#FFFFFF");
        resources["Text200Color"] = Color.Parse("#FAFAFA");
        resources["Text300Color"] = Color.Parse("#E0E0E0");
        resources["Text400Color"] = Color.Parse("#BDBDBD");
        resources["Text500Color"] = Color.Parse("#9E9E9E");
        resources["Text600Color"] = Color.Parse("#757575");
        resources["Text700Color"] = Color.Parse("#616161");
        resources["Text800Color"] = Color.Parse("#424242");
        resources["Text900Color"] = Color.Parse("#212121");

        // Background (Pure neutral grays - brighter)
        resources["Background100Color"] = Color.Parse("#8F8F8F");
        resources["Background200Color"] = Color.Parse("#7A7A7A");
        resources["Background300Color"] = Color.Parse("#686868");
        resources["Background400Color"] = Color.Parse("#565656");
        resources["Background500Color"] = Color.Parse("#484848");
        resources["Background600Color"] = Color.Parse("#3C3C3C");
        resources["Background700Color"] = Color.Parse("#323232");
        resources["Background800Color"] = Color.Parse("#2A2A2A");
        resources["Background900Color"] = Color.Parse("#1E1E1E");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#8F8F8F");
        resources["Disabled200Color"] = Color.Parse("#7A7A7A");
        resources["Disabled300Color"] = Color.Parse("#686868");
        resources["Disabled400Color"] = Color.Parse("#565656");
        resources["Disabled500Color"] = Color.Parse("#484848");
        resources["Disabled600Color"] = Color.Parse("#3C3C3C");
        resources["Disabled700Color"] = Color.Parse("#323232");
        resources["Disabled800Color"] = Color.Parse("#2A2A2A");
        resources["Disabled900Color"] = Color.Parse("#1E1E1E");

        // Divider (Brighter grays for high contrast borders)
        resources["Divider100Color"] = Color.Parse("#FFFFFF");
        resources["Divider200Color"] = Color.Parse("#E0E0E0");
        resources["Divider300Color"] = Color.Parse("#BDBDBD");
        resources["Divider400Color"] = Color.Parse("#9E9E9E");
        resources["Divider500Color"] = Color.Parse("#808080");
        resources["Divider600Color"] = Color.Parse("#5E5E5E");
        resources["Divider700Color"] = Color.Parse("#454545");
        resources["Divider800Color"] = Color.Parse("#333333");
        resources["Divider900Color"] = Color.Parse("#1A1A1A");

        // Icon (White to gray)
        resources["Icon100Color"] = Color.Parse("#FFFFFF");
        resources["Icon200Color"] = Color.Parse("#E5E5E5");
        resources["Icon300Color"] = Color.Parse("#CCCCCC");
        resources["Icon400Color"] = Color.Parse("#B3B3B3");
        resources["Icon500Color"] = Color.Parse("#999999");
        resources["Icon600Color"] = Color.Parse("#808080");
        resources["Icon700Color"] = Color.Parse("#666666");
        resources["Icon800Color"] = Color.Parse("#4D4D4D");
        resources["Icon900Color"] = Color.Parse("#333333");

        // Semantic Info (Light gray - subtle distinction)
        resources["SemanticInfo100Color"] = Color.Parse("#E0E0E0");
        resources["SemanticInfo200Color"] = Color.Parse("#BDBDBD");
        resources["SemanticInfo300Color"] = Color.Parse("#9E9E9E");
        resources["SemanticInfo400Color"] = Color.Parse("#858585");
        resources["SemanticInfo500Color"] = Color.Parse("#6E6E6E");
        resources["SemanticInfo600Color"] = Color.Parse("#575757");
        resources["SemanticInfo700Color"] = Color.Parse("#424242");
        resources["SemanticInfo800Color"] = Color.Parse("#303030");
        resources["SemanticInfo900Color"] = Color.Parse("#1A1A1A");

        // Semantic Negative (Darker grays for negative)
        resources["SemanticNegative100Color"] = Color.Parse("#D4D4D4");
        resources["SemanticNegative200Color"] = Color.Parse("#A3A3A3");
        resources["SemanticNegative300Color"] = Color.Parse("#737373");
        resources["SemanticNegative400Color"] = Color.Parse("#525252");
        resources["SemanticNegative500Color"] = Color.Parse("#404040");
        resources["SemanticNegative600Color"] = Color.Parse("#303030");
        resources["SemanticNegative700Color"] = Color.Parse("#262626");
        resources["SemanticNegative800Color"] = Color.Parse("#1A1A1A");
        resources["SemanticNegative900Color"] = Color.Parse("#0F0F0F");

        // Semantic Positive (Lighter grays for positive)
        resources["SemanticPositive100Color"] = Color.Parse("#FAFAFA");
        resources["SemanticPositive200Color"] = Color.Parse("#F0F0F0");
        resources["SemanticPositive300Color"] = Color.Parse("#E0E0E0");
        resources["SemanticPositive400Color"] = Color.Parse("#D0D0D0");
        resources["SemanticPositive500Color"] = Color.Parse("#B8B8B8");
        resources["SemanticPositive600Color"] = Color.Parse("#A0A0A0");
        resources["SemanticPositive700Color"] = Color.Parse("#888888");
        resources["SemanticPositive800Color"] = Color.Parse("#707070");
        resources["SemanticPositive900Color"] = Color.Parse("#585858");

        // Semantic Special (Mid grays)
        resources["SemanticSpecial100Color"] = Color.Parse("#E8E8E8");
        resources["SemanticSpecial200Color"] = Color.Parse("#D0D0D0");
        resources["SemanticSpecial300Color"] = Color.Parse("#B0B0B0");
        resources["SemanticSpecial400Color"] = Color.Parse("#909090");
        resources["SemanticSpecial500Color"] = Color.Parse("#787878");
        resources["SemanticSpecial600Color"] = Color.Parse("#606060");
        resources["SemanticSpecial700Color"] = Color.Parse("#484848");
        resources["SemanticSpecial800Color"] = Color.Parse("#383838");
        resources["SemanticSpecial900Color"] = Color.Parse("#282828");

        // Semantic Warning (Warm-ish mid grays)
        resources["SemanticWarning100Color"] = Color.Parse("#F5F5F5");
        resources["SemanticWarning200Color"] = Color.Parse("#E5E5E5");
        resources["SemanticWarning300Color"] = Color.Parse("#D5D5D5");
        resources["SemanticWarning400Color"] = Color.Parse("#C0C0C0");
        resources["SemanticWarning500Color"] = Color.Parse("#A8A8A8");
        resources["SemanticWarning600Color"] = Color.Parse("#909090");
        resources["SemanticWarning700Color"] = Color.Parse("#787878");
        resources["SemanticWarning800Color"] = Color.Parse("#606060");
        resources["SemanticWarning900Color"] = Color.Parse("#484848");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#000000");
        resources["WhiteColor"] = Color.Parse("#FFFFFF");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors (high contrast)
        resources["LiveRatesBackgroundColor"] = Color.Parse("#2A2A2A");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#9E9E9E");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#484848");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#323232");
        resources["ButtonOverlayLightColor"] = Color.Parse("#50FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#30FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#FFFFFF");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#686868");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#8F8F8F");
        resources["MessageBoxInfoColor"] = Color.Parse("#BDBDBD");
        resources["MessageBoxWarningColor"] = Color.Parse("#E0E0E0");
        resources["MessageBoxErrorColor"] = Color.Parse("#808080");
        resources["MessageBoxQuestionColor"] = Color.Parse("#B0B0B0");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#FFFFFF");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#3C3C3C");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#484848");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#686868");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#FFFFFF");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#E0E0E0");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFFFFF");
        resources["FooterGradientStartColor"] = Color.Parse("#1E1E1E");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFFFFF");

        return resources;
    }
}
