namespace Relovia.Api.Features.Places;

public static class CityNames
{
    public readonly record struct Place(string Name, string? Region);

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByCountry(TextReader cities, TextReader regions)
    {
        var regionNames = ReadRegions(regions);
        var places = new Dictionary<string, List<Place>>(StringComparer.OrdinalIgnoreCase);
        while (cities.ReadLine() is string line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var columns = line.Split('\t');
            if (columns.Length < 11)
            {
                continue;
            }

            var name = columns[1].Trim();
            var country = columns[8].Trim();
            if (name.Length == 0 || country.Length != 2 || !country.All(char.IsAsciiLetter))
            {
                continue;
            }

            string? region = null;
            var admin1 = columns[10].Trim();
            if (admin1.Length > 0
                && regionNames.TryGetValue($"{country}.{admin1}", out var regionName)
                && !string.Equals(regionName, name, StringComparison.OrdinalIgnoreCase))
            {
                region = regionName;
            }

            if (!places.TryGetValue(country, out var list))
            {
                list = [];
                places[country] = list;
            }

            list.Add(new Place(name, region));
        }

        var catalog = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (country, list) in places)
        {
            catalog[country.ToUpperInvariant()] = Labels(list);
        }

        return catalog;
    }

    public static IReadOnlyList<string> Labels(IReadOnlyList<Place> places)
    {
        var repeated = places
            .GroupBy(place => place.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var labels = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var place in places)
        {
            var label = repeated.Contains(place.Name) && place.Region is not null
                ? $"{place.Name}, {place.Region}"
                : place.Name;
            if (label.Length > 200)
            {
                label = label[..200];
            }

            if (seen.Add(label))
            {
                labels.Add(label);
            }
        }

        labels.Sort(StringComparer.OrdinalIgnoreCase);
        return labels;
    }

    private static Dictionary<string, string> ReadRegions(TextReader regions)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (regions.ReadLine() is string line)
        {
            var columns = line.Split('\t');
            if (columns.Length < 2)
            {
                continue;
            }

            var code = columns[0].Trim();
            var name = columns[1].Trim();
            if (code.Length > 0 && name.Length > 0)
            {
                names[code] = name;
            }
        }

        return names;
    }
}
