namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Forest Canopy theme - Natural green earth tones dark theme
/// </summary>
public static class ForestCanopyTheme
{
    public static ThemePalette Palette => new(
        "ForestCanopy",
        "Dark",
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#F2D8DA", "#F1C2C8", "#F09BA6", "#EE6B7E", "#E83C59", "#D61B44", "#B41139", "#971136", "#811234"),
        new ColorScale("#F5F5F0", "#E8E8E0", "#D5D5CA", "#B8B8AC", "#96968A", "#72726A", "#52524A", "#363632", "#1E1E1A"),
        new ColorScale("#7A9078", "#688066", "#587056", "#4A6048", "#3E523C", "#344632", "#2C3A2A", "#243022", "#182218"),
        new ColorScale("#7A9078", "#688066", "#587056", "#4A6048", "#3E523C", "#344632", "#2C3A2A", "#243022", "#182218"),
        new ColorScale("#D5D5CA", "#B8B8AC", "#96968A", "#72726A", "#52524A", "#363632", "#222A22", "#1A201A", "#0C100C"),
        new ColorScale("#FFE4E6", "#FECDD3", "#FDA4AF", "#FB7185", "#F43F5E", "#E11D48", "#BE123C", "#9F1239", "#881337"),
        new ColorScale("#99F6E4", "#5EEAD4", "#2DD4BF", "#14B8A6", "#0D9488", "#0F766E", "#115E59", "#134E4A", "#0A2A28"),
        new ColorScale("#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D", "#450A0A"),
        new ColorScale("#BBF7D0", "#86EFAC", "#4ADE80", "#22C55E", "#16A34A", "#15803D", "#166534", "#14532D", "#0A2915"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#2E0E46"),
        new ColorScale("#FEF08A", "#FDE047", "#FACC15", "#EAB308", "#CA8A04", "#A16207", "#854D0E", "#713F12", "#3D2208"),
        new ThemeSpecificColors(
            "#060806", "#F5F5F0", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#243022", "#059669", "#3E523C", "#2C3A2A",
            "#40FFFFFF", "#26FFFFFF", "#10B981", "#587056", "#7A9078",
            "#14B8A6", "#EAB308", "#EF4444", "#A855F7", "#34D399",
            "#344632", "#064E3B", "#065F46", "#34D399", "#6EE7B7",
            "#F5F5F0", "#182218", "#F5F5F0"
        )
    );
}
