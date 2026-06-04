namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Sunset Blaze theme - Warm orange/coral sunset accent dark theme
/// </summary>
public static class SunsetBlazeTheme
{
    public static ThemePalette Palette => new(
        "SunsetBlaze",
        "Dark",
        new ColorScale("#FFEDD5", "#FED7AA", "#FDBA74", "#FB923C", "#F97316", "#EA580C", "#C2410C", "#9A3412", "#7C2D12"),
        new ColorScale("#C4EDF1", "#9CE7EF", "#62DCEC", "#20C8E2", "#05ADC9", "#088AA9", "#0D6E89", "#14596F", "#154A5E"),
        new ColorScale("#FFFBEB", "#FEF3E2", "#FDE6C8", "#E8D0AE", "#C9B08A", "#9A856A", "#6B5A4B", "#3D3430", "#1F1A18"),
        new ColorScale("#9A8072", "#886E62", "#765E54", "#644E46", "#54403A", "#46342E", "#3A2A24", "#30221C", "#221814"),
        new ColorScale("#9A8072", "#886E62", "#765E54", "#644E46", "#54403A", "#46342E", "#3A2A24", "#30221C", "#221814"),
        new ColorScale("#FEF3E2", "#FDE6C8", "#E8C8A8", "#B89878", "#8A6A52", "#5C4438", "#3A2A22", "#281C16", "#180E0C"),
        new ColorScale("#CFFAFE", "#A5F3FC", "#67E8F9", "#22D3EE", "#06B6D4", "#0891B2", "#0E7490", "#155E75", "#164E63"),
        new ColorScale("#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E", "#082F49"),
        new ColorScale("#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D"),
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#3B0764"),
        new ColorScale("#FEF9C3", "#FEF08A", "#FDE047", "#FACC15", "#EAB308", "#CA8A04", "#A16207", "#854D0E", "#713F12"),
        new ThemeSpecificColors(
            "#180E0C", "#FFFBEB", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#30221C", "#F97316", "#54403A", "#3A2A24",
            "#40FFFFFF", "#26FFFFFF", "#FB923C", "#765E54", "#9A8072",
            "#38BDF8", "#FACC15", "#F87171", "#A855F7", "#FB923C",
            "#46342E", "#7C2D12", "#9A3412", "#FB923C", "#FDBA74",
            "#FFFBEB", "#221814", "#FFFBEB"
        )
    );
}
