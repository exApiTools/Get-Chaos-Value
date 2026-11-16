using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ninja_Price.API.PoeNinja;

// poe.ninja prices each viscera by name plus its implicit ("variant"), with numbers replaced by '#',
// e.g. "Engorged Eye" / "#% chance to avoid Corrupted Blood".
public class Viscera
{
    private static readonly Regex NumberRegex = new(@"\d+(?:\.\d+)?(?:\(\d+(?:\.\d+)?-\d+(?:\.\d+)?\))?|\(\d+(?:\.\d+)?-\d+(?:\.\d+)?\)", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    // Turns implicit mod texts into poe.ninja's variant form: numbers and ranges become '#', lines are joined with " / ".
    public static string NormalizeVariant(IEnumerable<string> modTexts)
    {
        var lines = modTexts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x.Split('\n'))
            .Select(x => WhitespaceRegex.Replace(NumberRegex.Replace(x, "#"), " ").Trim())
            .Where(x => x.Length > 0);
        return string.Join(" / ", lines);
    }

    public class RootObject
    {
        [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)]
        public List<Line> Lines { get; set; } = new();

        private Dictionary<string, List<Line>> _linesByName;

        [JsonIgnore]
        public Dictionary<string, List<Line>> LinesByName =>
            _linesByName ??= Lines
                .Where(x => x.Name != null)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        public Line Find(string name, string variant)
        {
            if (name == null || !LinesByName.TryGetValue(name, out var lines))
                return null;

            if (!string.IsNullOrEmpty(variant))
            {
                var exact = lines.FirstOrDefault(x => string.Equals(x.Variant, variant, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            // Only fall back to the name alone when it can't be ambiguous.
            return lines.Count == 1 ? lines[0] : null;
        }
    }

    public class Line
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("variant")]
        public string Variant { get; set; }

        [JsonProperty("itemType")]
        public string ItemType { get; set; }

        [JsonProperty("chaosValue")]
        public double? ChaosValue { get; set; }

        [JsonProperty("divineValue")]
        public double? DivineValue { get; set; }

        [JsonProperty("listingCount")]
        public long? ListingCount { get; set; }

        [JsonProperty("detailsId")]
        public string DetailsId { get; set; }

        [JsonProperty("sparkLine")]
        public Sparkline Sparkline { get; set; }
    }

    public class Sparkline
    {
        [JsonProperty("data")]
        public List<double?> Data { get; set; }

        [JsonProperty("totalChange")]
        public double? TotalChange { get; set; }
    }
}
