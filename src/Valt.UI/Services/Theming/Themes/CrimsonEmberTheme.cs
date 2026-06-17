namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Crimson Ember theme - Deep red/rose dark theme
/// </summary>
public static class CrimsonEmberTheme
{
    public static ThemePalette Palette => new(
        "CrimsonEmber",
        "Dark",
        new ColorScale("#FECDD3", "#FDA4AF", "#FB7185", "#F43F5E", "#E11D48", "#BE123C", "#9F1239", "#881337", "#4C0519"),
        new ColorScale("#C4EDF1", "#9CE7EF", "#62DCEC", "#20C8E2", "#05ADC9", "#088AA9", "#0D6E89", "#14596F", "#154A5E"),
        new ColorScale("#FFF1F2", "#FFE4E6", "#FECDD3", "#E8C0C5", "#C9A0A5", "#9A7578", "#6B5052", "#3D2E2F", "#1F1718"),
        new ColorScale("#A86A6A", "#985858", "#884848", "#763A3A", "#643030", "#542828", "#442020", "#361A1A", "#261212"),
        new ColorScale("#A86A6A", "#985858", "#884848", "#763A3A", "#643030", "#542828", "#442020", "#361A1A", "#261212"),
        new ColorScale("#FFF1F2", "#FFE4E6", "#FECDD3", "#E8B0B8", "#B8808A", "#8A5A62", "#5C3A40", "#3A2428", "#201416"),
        new ColorScale("#CFFAFE", "#A5F3FC", "#67E8F9", "#22D3EE", "#06B6D4", "#0891B2", "#0E7490", "#155E75", "#164E63"),
        new ColorScale("#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E", "#082F49"),
        new ColorScale("#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D"),
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#EDE9FE", "#DDD6FE", "#C4B5FD", "#A78BFA", "#8B5CF6", "#7C3AED", "#6D28D9", "#5B21B6", "#4C1D95"),
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ThemeSpecificColors(
            "#1C0C0C", "#FFF1F2", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#361A1A", "#E11D48", "#643030", "#442020",
            "#50FFFFFF", "#30FFFFFF", "#F43F5E", "#884848", "#A86A6A",
            "#38BDF8", "#FBBF24", "#F87171", "#A78BFA", "#FB7185",
            "#542828", "#4C0519", "#9F1239", "#FB7185", "#FDA4AF",
            "#FFF1F2", "#261212", "#FFF1F2"
        )
    );
}
