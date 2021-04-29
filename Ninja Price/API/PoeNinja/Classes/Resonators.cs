using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ninja_Price.API.PoeNinja.Classes
{
    public class Resonators
    {
        public partial class RootObject
        {
            [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)]
            public List<Line> Lines { get; set; }

            [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
            public Language Language { get; set; }
        }

        public partial class Language
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("translations")]
            public Translations Translations { get; set; }
        }

        public partial class Translations
        {
        }

        public partial class Line
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("icon")]
            public Uri Icon { get; set; }

            [JsonProperty("mapTier")]
            public long MapTier { get; set; }

            [JsonProperty("levelRequired")]
            public long LevelRequired { get; set; }

            [JsonProperty("baseType")]
            public object BaseType { get; set; }

            [JsonProperty("stackSize")]
            public long StackSize { get; set; }

            [JsonProperty("variant")]
            public object Variant { get; set; }

            [JsonProperty("prophecyText")]
            public object ProphecyText { get; set; }

            [JsonProperty("artFilename")]
            public object ArtFilename { get; set; }

            [JsonProperty("links")]
            public long Links { get; set; }

            [JsonProperty("itemClass")]
            public long ItemClass { get; set; }

            [JsonProperty("sparkline")]
            public Sparkline Sparkline { get; set; }

            [JsonProperty("lowConfidenceSparkline")]
            public Sparkline LowConfidenceSparkline { get; set; }

            [JsonProperty("implicitModifiers")]
            public object[] ImplicitModifiers { get; set; }

            [JsonProperty("explicitModifiers")]
            public object[] ExplicitModifiers { get; set; }

            [JsonProperty("flavourText")]
            public string FlavourText { get; set; }

            [JsonProperty("corrupted")]
            public bool Corrupted { get; set; }

            [JsonProperty("gemLevel")]
            public long GemLevel { get; set; }

            [JsonProperty("gemQuality")]
            public long GemQuality { get; set; }

            [JsonProperty("itemType")]
            public string ItemType { get; set; }

            [JsonProperty("chaosValue")]
            public double ChaosValue { get; set; }

            [JsonProperty("exaltedValue")]
            public double ExaltedValue { get; set; }

            [JsonProperty("count")]
            public long Count { get; set; }

            [JsonProperty("detailsId")]
            public string DetailsId { get; set; }

            [JsonProperty("tradeInfo")]
            public object TradeInfo { get; set; }

            [JsonProperty("mapRegion")]
            public object MapRegion { get; set; }

            [JsonProperty("listingCount")]
            public long ListingCount { get; set; }
        }

        public partial class Sparkline
        {
            [JsonProperty("data")]
            public double?[] Data { get; set; }

            [JsonProperty("totalChange")]
            public double TotalChange { get; set; }
        }
    }
}