using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform;

namespace Valt.UI.Services.IconMaps;

public static class IconMapLoader
{
    private static Dictionary<string, HashSet<IconMap>> _codeMap = new();

    public static void LoadIcons(string source)
    {
        //TODO: semaphore

        if (_codeMap.ContainsKey(source))
            return;

        var iconsMap = AssetLoader.Open(new Uri($"avares://Valt/Assets/Fonts/{source}-map.json"));

        var iconsRaw = JsonSerializer.Deserialize<List<DTO>>(iconsMap)!;

        var iconMapList = iconsRaw.Select(x =>
        {
            if (!uint.TryParse(x.unicode, System.Globalization.NumberStyles.HexNumber, null, out var unicodeValue))
            {
                throw new JsonException($"Invalid Unicode value: {x.unicode}");
            }

            return new IconMap(source, x.name, (char)unicodeValue, x.category);
        }).ToHashSet();

        _codeMap.Add(source, iconMapList);
    }

    public static char GetIcon(string source, string name)
    {
        return !_codeMap.TryGetValue(source, out var iconMap)
            ? char.MinValue
            : iconMap.First(x => x.Name == name)?.Unicode ?? char.MinValue;
    }

    public static ReadOnlyCollection<string> GetIconPackNames() => _codeMap.Keys.ToList().AsReadOnly();

    public static ReadOnlyCollection<string> GetIconPackCategories(string source) => _codeMap[source]
        .Where(x => x.Category != null).Select(x => x.Category).Distinct().ToList().AsReadOnly()!;

    public static HashSet<IconMap> GetIconPack(string source, string? category = null)
    {
        if (!_codeMap.TryGetValue(source, out var value))
            return Enumerable.Empty<IconMap>().ToHashSet();

        return category == null
            ? value
            : value.Where(x => x.Category == category).ToHashSet();
    }

    private record DTO(string name, string unicode, string category);
}