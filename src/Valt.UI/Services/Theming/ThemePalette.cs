using Avalonia.Media;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Represents a 9-step color scale (100-900).
/// </summary>
public record ColorScale(
    string C100,
    string C200,
    string C300,
    string C400,
    string C500,
    string C600,
    string C700,
    string C800,
    string C900
);

/// <summary>
/// UI-specific colors that don't follow a 9-step scale.
/// </summary>
public record ThemeSpecificColors(
    string Black,
    string White,
    string CreditBase,
    string DebtBase,
    string TransferBase,
    string TitleBarForeground,
    string LiveRatesBackground,
    string LiveRatesBorderGradientStart,
    string LiveRatesBorderGradientEnd,
    string LiveRatesVariationBackground,
    string ButtonOverlayLight,
    string ButtonOverlayMedium,
    string StepIndicatorActive,
    string StepIndicatorInactive,
    string StepIndicatorInactiveLight,
    string MessageBoxInfo,
    string MessageBoxWarning,
    string MessageBoxError,
    string MessageBoxQuestion,
    string ColorPickerSelectedBorder,
    string ModalTopBarBackground,
    string TopBarButtonBackground,
    string TopBarButtonBackgroundHover,
    string TopBarButtonBackgroundSelected,
    string TopBarButtonForeground,
    string TopBarButtonForegroundHover,
    string FooterGradientStart,
    string IconSelectorDefault
);

/// <summary>
/// Complete color palette definition for a theme.
/// </summary>
public record ThemePalette(
    string Name,
    string BaseTheme,
    ColorScale Accent,
    ColorScale Secondary,
    ColorScale Text,
    ColorScale Background,
    ColorScale Disabled,
    ColorScale Divider,
    ColorScale Icon,
    ColorScale SemanticInfo,
    ColorScale SemanticNegative,
    ColorScale SemanticPositive,
    ColorScale SemanticSpecial,
    ColorScale SemanticWarning,
    ThemeSpecificColors Specific
);
