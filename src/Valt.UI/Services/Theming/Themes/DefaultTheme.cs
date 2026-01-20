using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Default theme - Orange accent dark theme
/// </summary>
public static class DefaultTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Orange)
        resources["Accent100Color"] = Color.Parse("#ffe2bb");
        resources["Accent200Color"] = Color.Parse("#ffcc88");
        resources["Accent300Color"] = Color.Parse("#ffb755");
        resources["Accent400Color"] = Color.Parse("#ffa122");
        resources["Accent500Color"] = Color.Parse("#e98805");
        resources["Accent600Color"] = Color.Parse("#b26a09");
        resources["Accent700Color"] = Color.Parse("#7e4d0a");
        resources["Accent800Color"] = Color.Parse("#4d3008");
        resources["Accent900Color"] = Color.Parse("#1e1304");

        // Secondary (Blue)
        resources["Secondary100Color"] = Color.Parse("#bbd8ff");
        resources["Secondary200Color"] = Color.Parse("#88bbff");
        resources["Secondary300Color"] = Color.Parse("#559dff");
        resources["Secondary400Color"] = Color.Parse("#2280ff");
        resources["Secondary500Color"] = Color.Parse("#0566e9");
        resources["Secondary600Color"] = Color.Parse("#0951b2");
        resources["Secondary700Color"] = Color.Parse("#0a3b7e");
        resources["Secondary800Color"] = Color.Parse("#08254d");
        resources["Secondary900Color"] = Color.Parse("#040f1e");

        // Text (Warm Gray for dark backgrounds)
        resources["Text100Color"] = Color.Parse("#fdfdfc");
        resources["Text200Color"] = Color.Parse("#eeebe8");
        resources["Text300Color"] = Color.Parse("#cfccc9");
        resources["Text400Color"] = Color.Parse("#a8a6a4");
        resources["Text500Color"] = Color.Parse("#83807c");
        resources["Text600Color"] = Color.Parse("#6b6761");
        resources["Text700Color"] = Color.Parse("#544e45");
        resources["Text800Color"] = Color.Parse("#3b342b");
        resources["Text900Color"] = Color.Parse("#1f1a14");

        // Background (Neutral Gray - dark base)
        resources["Background100Color"] = Color.Parse("#fcfcfc");
        resources["Background200Color"] = Color.Parse("#ebebeb");
        resources["Background300Color"] = Color.Parse("#cccccc");
        resources["Background400Color"] = Color.Parse("#a6a6a6");
        resources["Background500Color"] = Color.Parse("#808080");
        resources["Background600Color"] = Color.Parse("#666666");
        resources["Background700Color"] = Color.Parse("#4d4d4d");
        resources["Background800Color"] = Color.Parse("#333333");
        resources["Background900Color"] = Color.Parse("#1a1a1a");

        // Disabled
        resources["Disabled100Color"] = Color.Parse("#fcfcfc");
        resources["Disabled200Color"] = Color.Parse("#ebebeb");
        resources["Disabled300Color"] = Color.Parse("#cccccc");
        resources["Disabled400Color"] = Color.Parse("#a6a6a6");
        resources["Disabled500Color"] = Color.Parse("#808080");
        resources["Disabled600Color"] = Color.Parse("#666666");
        resources["Disabled700Color"] = Color.Parse("#4d4d4d");
        resources["Disabled800Color"] = Color.Parse("#333333");
        resources["Disabled900Color"] = Color.Parse("#1a1a1a");

        // Divider
        resources["Divider100Color"] = Color.Parse("#fdfdfc");
        resources["Divider200Color"] = Color.Parse("#eeebe8");
        resources["Divider300Color"] = Color.Parse("#cfccc9");
        resources["Divider400Color"] = Color.Parse("#a8a6a4");
        resources["Divider500Color"] = Color.Parse("#83807c");
        resources["Divider600Color"] = Color.Parse("#6b6761");
        resources["Divider700Color"] = Color.Parse("#544e45");
        resources["Divider800Color"] = Color.Parse("#3b342b");
        resources["Divider900Color"] = Color.Parse("#1f1a14");

        // Icon (Blue tinted)
        resources["Icon100Color"] = Color.Parse("#bbd8ff");
        resources["Icon200Color"] = Color.Parse("#88bbff");
        resources["Icon300Color"] = Color.Parse("#559dff");
        resources["Icon400Color"] = Color.Parse("#2280ff");
        resources["Icon500Color"] = Color.Parse("#0566e9");
        resources["Icon600Color"] = Color.Parse("#0951b2");
        resources["Icon700Color"] = Color.Parse("#0a3b7e");
        resources["Icon800Color"] = Color.Parse("#08254d");
        resources["Icon900Color"] = Color.Parse("#040f1e");

        // Semantic Info (Blue)
        resources["SemanticInfo100Color"] = Color.Parse("#8888ff");
        resources["SemanticInfo200Color"] = Color.Parse("#5555ff");
        resources["SemanticInfo300Color"] = Color.Parse("#2222ff");
        resources["SemanticInfo400Color"] = Color.Parse("#0606e8");
        resources["SemanticInfo500Color"] = Color.Parse("#0909b2");
        resources["SemanticInfo600Color"] = Color.Parse("#0a0a7e");
        resources["SemanticInfo700Color"] = Color.Parse("#08084d");
        resources["SemanticInfo800Color"] = Color.Parse("#04041e");
        resources["SemanticInfo900Color"] = Color.Parse("#000000");

        // Semantic Negative (Red)
        resources["SemanticNegative100Color"] = Color.Parse("#ff8888");
        resources["SemanticNegative200Color"] = Color.Parse("#ff5555");
        resources["SemanticNegative300Color"] = Color.Parse("#ff2222");
        resources["SemanticNegative400Color"] = Color.Parse("#e80606");
        resources["SemanticNegative500Color"] = Color.Parse("#b20909");
        resources["SemanticNegative600Color"] = Color.Parse("#7e0a0a");
        resources["SemanticNegative700Color"] = Color.Parse("#4d0808");
        resources["SemanticNegative800Color"] = Color.Parse("#1e0404");
        resources["SemanticNegative900Color"] = Color.Parse("#000000");

        // Semantic Positive (Green)
        resources["SemanticPositive100Color"] = Color.Parse("#88ff88");
        resources["SemanticPositive200Color"] = Color.Parse("#55ff55");
        resources["SemanticPositive300Color"] = Color.Parse("#22ff22");
        resources["SemanticPositive400Color"] = Color.Parse("#06e806");
        resources["SemanticPositive500Color"] = Color.Parse("#09b209");
        resources["SemanticPositive600Color"] = Color.Parse("#0a7e0a");
        resources["SemanticPositive700Color"] = Color.Parse("#084d08");
        resources["SemanticPositive800Color"] = Color.Parse("#041e04");
        resources["SemanticPositive900Color"] = Color.Parse("#000000");

        // Semantic Special (Purple)
        resources["SemanticSpecial100Color"] = Color.Parse("#ff88ff");
        resources["SemanticSpecial200Color"] = Color.Parse("#ff55ff");
        resources["SemanticSpecial300Color"] = Color.Parse("#ff22ff");
        resources["SemanticSpecial400Color"] = Color.Parse("#e806e8");
        resources["SemanticSpecial500Color"] = Color.Parse("#b209b2");
        resources["SemanticSpecial600Color"] = Color.Parse("#7e0a7e");
        resources["SemanticSpecial700Color"] = Color.Parse("#4d084d");
        resources["SemanticSpecial800Color"] = Color.Parse("#1e041e");
        resources["SemanticSpecial900Color"] = Color.Parse("#000000");

        // Semantic Warning (Yellow)
        resources["SemanticWarning100Color"] = Color.Parse("#fbffd5");
        resources["SemanticWarning200Color"] = Color.Parse("#f6ffa2");
        resources["SemanticWarning300Color"] = Color.Parse("#f1ff6f");
        resources["SemanticWarning400Color"] = Color.Parse("#eafb40");
        resources["SemanticWarning500Color"] = Color.Parse("#dff414");
        resources["SemanticWarning600Color"] = Color.Parse("#b5c60f");
        resources["SemanticWarning700Color"] = Color.Parse("#86930f");
        resources["SemanticWarning800Color"] = Color.Parse("#5a620d");
        resources["SemanticWarning900Color"] = Color.Parse("#2f3309");

        // Special Colors
        resources["BlackColor"] = Color.Parse("#1f1a14");
        resources["WhiteColor"] = Color.Parse("#ffffff");

        // Transaction Row Colors (for DataGrid highlighting)
        resources["CreditBaseColor"] = Color.Parse("#78DB55");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#FFF866");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#0D0D0D");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#2A2A2A");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#1A1A1A");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#1A1A1A");
        resources["ButtonOverlayLightColor"] = Color.Parse("#69FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#49FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#0078D4");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#6B6B6B");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#9E9E9E");
        resources["MessageBoxInfoColor"] = Color.Parse("#2196F3");
        resources["MessageBoxWarningColor"] = Color.Parse("#FF9800");
        resources["MessageBoxErrorColor"] = Color.Parse("#F44336");
        resources["MessageBoxQuestionColor"] = Color.Parse("#9C27B0");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#FFE98805");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#444444");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#5B3800");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#7A5500");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#FF9E00");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#FFA500");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#FFFFFF");
        resources["FooterGradientStartColor"] = Color.Parse("#000000");
        resources["IconSelectorDefaultColor"] = Color.Parse("#FFFFFF");

        return resources;
    }
}
