using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class Essences
{
    public class RootObject
    {
        [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)]
        public List<Line> Lines { get; set; }
    }

    public class Line
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Icon { get; set; }

        [JsonProperty("mapTier", NullValueHandling = NullValueHandling.Ignore)]
        public long? MapTier { get; set; }

        [JsonProperty("levelRequired", NullValueHandling = NullValueHandling.Ignore)]
        public long? LevelRequired { get; set; }

        [JsonProperty("baseType", NullValueHandling = NullValueHandling.Ignore)]
        public string BaseType { get; set; }

        [JsonProperty("stackSize", NullValueHandling = NullValueHandling.Ignore)]
        public long? StackSize { get; set; }

        [JsonProperty("variant")]
        public object Variant { get; set; }

        [JsonProperty("prophecyText")]
        public object ProphecyText { get; set; }

        [JsonProperty("artFilename")]
        public object ArtFilename { get; set; }

        [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
        public long? Links { get; set; }

        [JsonProperty("itemClass", NullValueHandling = NullValueHandling.Ignore)]
        public long? ItemClass { get; set; }

        [JsonProperty("sparkline", NullValueHandling = NullValueHandling.Ignore)]
        public Sparkline Sparkline { get; set; }

        [JsonProperty("lowConfidenceSparkline", NullValueHandling = NullValueHandling.Ignore)]
        public Sparkline LowConfidenceSparkline { get; set; }

        [JsonProperty("implicitModifiers", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> ImplicitModifiers { get; set; }

        [JsonProperty("explicitModifiers", NullValueHandling = NullValueHandling.Ignore)]
        public List<ExplicitModifier> ExplicitModifiers { get; set; }

        [JsonProperty("flavourText", NullValueHandling = NullValueHandling.Ignore)]
        public string FlavourText { get; set; }

        [JsonProperty("corrupted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Corrupted { get; set; }

        [JsonProperty("gemLevel", NullValueHandling = NullValueHandling.Ignore)]
        public long? GemLevel { get; set; }

        [JsonProperty("gemQuality", NullValueHandling = NullValueHandling.Ignore)]
        public long? GemQuality { get; set; }


        [JsonProperty("chaosValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? ChaosValue { get; set; }

        [JsonProperty("exaltedValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? ExaltedValue { get; set; }

        [JsonProperty("divineValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? DivineValue { get; set; }

        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public long? Count { get; set; }

        [JsonProperty("detailsId", NullValueHandling = NullValueHandling.Ignore)]
        public string DetailsId { get; set; }
    }

    public class ExplicitModifier
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("optional", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Optional { get; set; }
    }

    public class Sparkline
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public List<double?> Data { get; set; }

        [JsonProperty("totalChange", NullValueHandling = NullValueHandling.Ignore)]
        public double? TotalChange { get; set; }
    }

}