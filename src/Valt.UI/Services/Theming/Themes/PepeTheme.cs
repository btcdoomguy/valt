using Avalonia.Controls;
using Avalonia.Media;

namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Pepe theme - Inspired by the iconic Pepe the Frog meme
/// Vibrant frog greens with royal blue accents on a deep green backdrop
/// </summary>
public static class PepeTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        // Accent (Pepe Frog Green - the iconic bright green skin)
        resources["Accent100Color"] = Color.Parse("#DAFCD5");
        resources["Accent200Color"] = Color.Parse("#B5F5AB");
        resources["Accent300Color"] = Color.Parse("#88E87A");
        resources["Accent400Color"] = Color.Parse("#63D855");
        resources["Accent500Color"] = Color.Parse("#4EC840");
        resources["Accent600Color"] = Color.Parse("#3BA832");
        resources["Accent700Color"] = Color.Parse("#2D8A26");
        resources["Accent800Color"] = Color.Parse("#226E1D");
        resources["Accent900Color"] = Color.Parse("#1A5616");

        // Secondary (Pepe's Blue Shirt)
        resources["Secondary100Color"] = Color.Parse("#D0E4FF");
        resources["Secondary200Color"] = Color.Parse("#A3CAFF");
        resources["Secondary300Color"] = Color.Parse("#6EAAFF");
        resources["Secondary400Color"] = Color.Parse("#4A8EF5");
        resources["Secondary500Color"] = Color.Parse("#2B6FE0");
        resources["Secondary600Color"] = Color.Parse("#1A5ABF");
        resources["Secondary700Color"] = Color.Parse("#12469E");
        resources["Secondary800Color"] = Color.Parse("#0D3580");
        resources["Secondary900Color"] = Color.Parse("#082568");

        // Text (Slightly warm off-white for readability on dark green)
        resources["Text100Color"] = Color.Parse("#F4F7F2");
        resources["Text200Color"] = Color.Parse("#E4EAE0");
        resources["Text300Color"] = Color.Parse("#CED6C8");
        resources["Text400Color"] = Color.Parse("#AFB8A8");
        resources["Text500Color"] = Color.Parse("#8D9686");
        resources["Text600Color"] = Color.Parse("#6B7466");
        resources["Text700Color"] = Color.Parse("#4D5548");
        resources["Text800Color"] = Color.Parse("#333A30");
        resources["Text900Color"] = Color.Parse("#1C211A");

        // Background (Deep swampy green - Pepe's habitat)
        resources["Background100Color"] = Color.Parse("#6A8264");
        resources["Background200Color"] = Color.Parse("#577254");
        resources["Background300Color"] = Color.Parse("#466245");
        resources["Background400Color"] = Color.Parse("#385338");
        resources["Background500Color"] = Color.Parse("#2E452E");
        resources["Background600Color"] = Color.Parse("#253A25");
        resources["Background700Color"] = Color.Parse("#1E301E");
        resources["Background800Color"] = Color.Parse("#172717");
        resources["Background900Color"] = Color.Parse("#101E10");

        // Disabled (matches Background scale)
        resources["Disabled100Color"] = Color.Parse("#6A8264");
        resources["Disabled200Color"] = Color.Parse("#577254");
        resources["Disabled300Color"] = Color.Parse("#466245");
        resources["Disabled400Color"] = Color.Parse("#385338");
        resources["Disabled500Color"] = Color.Parse("#2E452E");
        resources["Disabled600Color"] = Color.Parse("#253A25");
        resources["Disabled700Color"] = Color.Parse("#1E301E");
        resources["Disabled800Color"] = Color.Parse("#172717");
        resources["Disabled900Color"] = Color.Parse("#101E10");

        // Divider (Muted green-gray)
        resources["Divider100Color"] = Color.Parse("#CED6C8");
        resources["Divider200Color"] = Color.Parse("#AFB8A8");
        resources["Divider300Color"] = Color.Parse("#8D9686");
        resources["Divider400Color"] = Color.Parse("#6B7466");
        resources["Divider500Color"] = Color.Parse("#4D5548");
        resources["Divider600Color"] = Color.Parse("#333A30");
        resources["Divider700Color"] = Color.Parse("#1E2A1E");
        resources["Divider800Color"] = Color.Parse("#16201A");
        resources["Divider900Color"] = Color.Parse("#0A120A");

        // Icon (Pepe's Orange-Brown Mouth/Lips)
        resources["Icon100Color"] = Color.Parse("#FFE4CC");
        resources["Icon200Color"] = Color.Parse("#FFCDA3");
        resources["Icon300Color"] = Color.Parse("#FFB070");
        resources["Icon400Color"] = Color.Parse("#F59345");
        resources["Icon500Color"] = Color.Parse("#E07A2B");
        resources["Icon600Color"] = Color.Parse("#BF6220");
        resources["Icon700Color"] = Color.Parse("#9E4D18");
        resources["Icon800Color"] = Color.Parse("#803B12");
        resources["Icon900Color"] = Color.Parse("#682D0C");

        // Semantic Info (Teal-green, swamp water)
        resources["SemanticInfo100Color"] = Color.Parse("#99F6E4");
        resources["SemanticInfo200Color"] = Color.Parse("#5EEAD4");
        resources["SemanticInfo300Color"] = Color.Parse("#2DD4BF");
        resources["SemanticInfo400Color"] = Color.Parse("#14B8A6");
        resources["SemanticInfo500Color"] = Color.Parse("#0D9488");
        resources["SemanticInfo600Color"] = Color.Parse("#0F766E");
        resources["SemanticInfo700Color"] = Color.Parse("#115E59");
        resources["SemanticInfo800Color"] = Color.Parse("#134E4A");
        resources["SemanticInfo900Color"] = Color.Parse("#0A2A28");

        // Semantic Negative (Sad Pepe red - lips/frown color)
        resources["SemanticNegative100Color"] = Color.Parse("#FECACA");
        resources["SemanticNegative200Color"] = Color.Parse("#FCA5A5");
        resources["SemanticNegative300Color"] = Color.Parse("#F87171");
        resources["SemanticNegative400Color"] = Color.Parse("#EF4444");
        resources["SemanticNegative500Color"] = Color.Parse("#DC2626");
        resources["SemanticNegative600Color"] = Color.Parse("#B91C1C");
        resources["SemanticNegative700Color"] = Color.Parse("#991B1B");
        resources["SemanticNegative800Color"] = Color.Parse("#7F1D1D");
        resources["SemanticNegative900Color"] = Color.Parse("#450A0A");

        // Semantic Positive (Happy Pepe bright green)
        resources["SemanticPositive100Color"] = Color.Parse("#C6FCC0");
        resources["SemanticPositive200Color"] = Color.Parse("#96F58E");
        resources["SemanticPositive300Color"] = Color.Parse("#63E85A");
        resources["SemanticPositive400Color"] = Color.Parse("#3DD835");
        resources["SemanticPositive500Color"] = Color.Parse("#28B822");
        resources["SemanticPositive600Color"] = Color.Parse("#1F981A");
        resources["SemanticPositive700Color"] = Color.Parse("#187815");
        resources["SemanticPositive800Color"] = Color.Parse("#125C10");
        resources["SemanticPositive900Color"] = Color.Parse("#0A3A0A");

        // Semantic Special (Purple - rare Pepe vibes)
        resources["SemanticSpecial100Color"] = Color.Parse("#E9D5FF");
        resources["SemanticSpecial200Color"] = Color.Parse("#D8B4FE");
        resources["SemanticSpecial300Color"] = Color.Parse("#C084FC");
        resources["SemanticSpecial400Color"] = Color.Parse("#A855F7");
        resources["SemanticSpecial500Color"] = Color.Parse("#9333EA");
        resources["SemanticSpecial600Color"] = Color.Parse("#7E22CE");
        resources["SemanticSpecial700Color"] = Color.Parse("#6B21A8");
        resources["SemanticSpecial800Color"] = Color.Parse("#581C87");
        resources["SemanticSpecial900Color"] = Color.Parse("#2E0E46");

        // Semantic Warning (Amber/yellow - caution Pepe)
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
        resources["BlackColor"] = Color.Parse("#060A06");
        resources["WhiteColor"] = Color.Parse("#F4F7F2");

        // Transaction Row Colors
        resources["CreditBaseColor"] = Color.Parse("#63D855");
        resources["DebtBaseColor"] = Color.Parse("#FF7D7D");
        resources["TransferBaseColor"] = Color.Parse("#F59345");

        // Title Bar
        resources["TitleBarForegroundColor"] = Color.Parse("#FFFFFF");

        // UI Element Colors
        resources["LiveRatesBackgroundColor"] = Color.Parse("#172717");
        resources["LiveRatesBorderGradientStartColor"] = Color.Parse("#4EC840");
        resources["LiveRatesBorderGradientEndColor"] = Color.Parse("#2E452E");
        resources["LiveRatesVariationBackgroundColor"] = Color.Parse("#1E301E");
        resources["ButtonOverlayLightColor"] = Color.Parse("#40FFFFFF");
        resources["ButtonOverlayMediumColor"] = Color.Parse("#26FFFFFF");
        resources["StepIndicatorActiveColor"] = Color.Parse("#4EC840");
        resources["StepIndicatorInactiveColor"] = Color.Parse("#466245");
        resources["StepIndicatorInactiveLightColor"] = Color.Parse("#6A8264");
        resources["MessageBoxInfoColor"] = Color.Parse("#14B8A6");
        resources["MessageBoxWarningColor"] = Color.Parse("#EAB308");
        resources["MessageBoxErrorColor"] = Color.Parse("#EF4444");
        resources["MessageBoxQuestionColor"] = Color.Parse("#A855F7");
        resources["ColorPickerSelectedBorderColor"] = Color.Parse("#63D855");
        resources["ModalTopBarBackgroundColor"] = Color.Parse("#253A25");
        resources["TopBarButtonBackgroundColor"] = Color.Parse("#1A5616");
        resources["TopBarButtonBackgroundHoverColor"] = Color.Parse("#226E1D");
        resources["TopBarButtonBackgroundSelectedColor"] = Color.Parse("#4EC840");
        resources["TopBarButtonForegroundColor"] = Color.Parse("#88E87A");
        resources["TopBarButtonForegroundHoverColor"] = Color.Parse("#F4F7F2");
        resources["FooterGradientStartColor"] = Color.Parse("#101E10");
        resources["IconSelectorDefaultColor"] = Color.Parse("#F4F7F2");

        return resources;
    }
}
