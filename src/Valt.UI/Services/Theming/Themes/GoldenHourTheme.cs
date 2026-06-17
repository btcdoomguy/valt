namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Golden Hour theme - Warm amber/gold dark theme
/// </summary>
public static class GoldenHourTheme
{
    public static ThemePalette Palette => new(
        "GoldenHour",
        "Dark",
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ColorScale("#D0DEF1", "#B5D0F1", "#8CBBF0", "#5B9DEE", "#387BE9", "#235EDF", "#1C4ACD", "#1C3DA6", "#1C3783"),
        new ColorScale("#FFFBEB", "#FEF3DC", "#F5E6C8", "#E2D1AE", "#C4B08A", "#9A8A6A", "#6B604B", "#453D30", "#261F18"),
        new ColorScale("#9A8570", "#887360", "#766250", "#645042", "#544236", "#46382C", "#3A2E24", "#30261E", "#221A14"),
        new ColorScale("#9A8570", "#887360", "#766250", "#645042", "#544236", "#46382C", "#3A2E24", "#30261E", "#221A14"),
        new ColorScale("#F5E6C8", "#E2D1AE", "#C4B08A", "#9A8A6A", "#6B604B", "#453D30", "#2A231C", "#1F1A15", "#100D0A"),
        new ColorScale("#DBEAFE", "#BFDBFE", "#93C5FD", "#60A5FA", "#3B82F6", "#2563EB", "#1D4ED8", "#1E40AF", "#1E3A8A"),
        new ColorScale("#90CDF4", "#63B3ED", "#4299E1", "#3182CE", "#2B6CB0", "#2C5282", "#2A4365", "#1A365D", "#0D1B2A"),
        new ColorScale("#FEB2B2", "#FC8181", "#F56565", "#E53E3E", "#C53030", "#9B2C2C", "#822727", "#63171B", "#3B0D0F"),
        new ColorScale("#9AE6B4", "#68D391", "#48BB78", "#38A169", "#2F855A", "#276749", "#22543D", "#1C4532", "#0D2818"),
        new ColorScale("#D6BCFA", "#B794F4", "#9F7AEA", "#805AD5", "#6B46C1", "#553C9A", "#44337A", "#322659", "#1A1033"),
        new ColorScale("#FED7AA", "#FDBA74", "#FB923C", "#F97316", "#EA580C", "#C2410C", "#9A3412", "#7C2D12", "#431407"),
        new ThemeSpecificColors(
            "#0A0806", "#FFFBEB", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#30261E", "#D97706", "#544236", "#3A2E24",
            "#40FFFFFF", "#26FFFFFF", "#F59E0B", "#766250", "#9A8570",
            "#4299E1", "#F97316", "#E53E3E", "#805AD5", "#FBBF24",
            "#46382C", "#78350F", "#92400E", "#FBBF24", "#FCD34D",
            "#FFFBEB", "#221A14", "#FFFBEB"
        )
    );
}
