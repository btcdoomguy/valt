namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Pepe theme - Inspired by the iconic Pepe the Frog meme
/// </summary>
public static class PepeTheme
{
    public static ThemePalette Palette => new(
        "Pepe",
        "Dark",
        new ColorScale("#DAFCD5", "#B5F5AB", "#88E87A", "#63D855", "#4EC840", "#3BA832", "#2D8A26", "#226E1D", "#1A5616"),
        new ColorScale("#D0E4FF", "#A3CAFF", "#6EAAFF", "#4A8EF5", "#2B6FE0", "#1A5ABF", "#12469E", "#0D3580", "#082568"),
        new ColorScale("#F4F7F2", "#E4EAE0", "#CED6C8", "#AFB8A8", "#8D9686", "#6B7466", "#4D5548", "#333A30", "#1C211A"),
        new ColorScale("#6A8264", "#577254", "#466245", "#385338", "#2E452E", "#253A25", "#1E301E", "#172717", "#101E10"),
        new ColorScale("#6A8264", "#577254", "#466245", "#385338", "#2E452E", "#253A25", "#1E301E", "#172717", "#101E10"),
        new ColorScale("#CED6C8", "#AFB8A8", "#8D9686", "#6B7466", "#4D5548", "#333A30", "#1E2A1E", "#16201A", "#0A120A"),
        new ColorScale("#FFE4CC", "#FFCDA3", "#FFB070", "#F59345", "#E07A2B", "#BF6220", "#9E4D18", "#803B12", "#682D0C"),
        new ColorScale("#99F6E4", "#5EEAD4", "#2DD4BF", "#14B8A6", "#0D9488", "#0F766E", "#115E59", "#134E4A", "#0A2A28"),
        new ColorScale("#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D", "#450A0A"),
        new ColorScale("#C6FCC0", "#96F58E", "#63E85A", "#3DD835", "#28B822", "#1F981A", "#187815", "#125C10", "#0A3A0A"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#2E0E46"),
        new ColorScale("#FEF08A", "#FDE047", "#FACC15", "#EAB308", "#CA8A04", "#A16207", "#854D0E", "#713F12", "#3D2208"),
        new ThemeSpecificColors(
            "#060A06", "#F4F7F2", "#63D855", "#FF7D7D", "#F59345",
            "#FFFFFF", "#172717", "#4EC840", "#2E452E", "#1E301E",
            "#40FFFFFF", "#26FFFFFF", "#4EC840", "#466245", "#6A8264",
            "#14B8A6", "#EAB308", "#EF4444", "#A855F7", "#63D855",
            "#253A25", "#1A5616", "#226E1D", "#4EC840", "#88E87A",
            "#F4F7F2", "#101E10", "#F4F7F2"
        )
    );
}
