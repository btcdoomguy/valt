namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Arctic Frost theme - Cool crisp blue-white dark theme
/// </summary>
public static class ArcticFrostTheme
{
    public static ThemePalette Palette => new(
        "ArcticFrost",
        "Dark",
        new ColorScale("#E0F2FE", "#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E"),
        new ColorScale("#F2E1CA", "#F1CCA1", "#F0B16E", "#EE8A39", "#ED6D15", "#DF530B", "#B83E0B", "#923111", "#762B11"),
        new ColorScale("#F8FAFC", "#F1F5F9", "#E2E8F0", "#CBD5E1", "#94A3B8", "#64748B", "#475569", "#334155", "#1E293B"),
        new ColorScale("#7B8AA0", "#6B7B90", "#5A6B80", "#4A5C70", "#3E4F62", "#334455", "#2A3A4A", "#223040", "#182230"),
        new ColorScale("#7B8AA0", "#6B7B90", "#5A6B80", "#4A5C70", "#3E4F62", "#334455", "#2A3A4A", "#223040", "#182230"),
        new ColorScale("#E2E8F0", "#CBD5E1", "#94A3B8", "#64748B", "#475569", "#334155", "#1E293B", "#172033", "#0D1320"),
        new ColorScale("#FFEDD5", "#FED7AA", "#FDBA74", "#FB923C", "#F97316", "#EA580C", "#C2410C", "#9A3412", "#7C2D12"),
        new ColorScale("#93C5FD", "#60A5FA", "#3B82F6", "#2563EB", "#1D4ED8", "#1E40AF", "#1E3A8A", "#172554", "#0C1426"),
        new ColorScale("#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D", "#450A0A"),
        new ColorScale("#86EFAC", "#4ADE80", "#22C55E", "#16A34A", "#15803D", "#166534", "#14532D", "#134425", "#0A2915"),
        new ColorScale("#C4B5FD", "#A78BFA", "#8B5CF6", "#7C3AED", "#6D28D9", "#5B21B6", "#4C1D95", "#3B1578", "#1E0A3E"),
        new ColorScale("#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F", "#451A03"),
        new ThemeSpecificColors(
            "#04060B", "#F8FAFC", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#223040", "#0284C7", "#3E4F62", "#2A3A4A",
            "#40FFFFFF", "#26FFFFFF", "#0EA5E9", "#5A6B80", "#7B8AA0",
            "#3B82F6", "#F59E0B", "#EF4444", "#8B5CF6", "#38BDF8",
            "#334455", "#0C4A6E", "#075985", "#38BDF8", "#7DD3FC",
            "#F8FAFC", "#182230", "#F8FAFC"
        )
    );
}
