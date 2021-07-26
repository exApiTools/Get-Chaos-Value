using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja.Classes
{
    public class Invitations
    {
        public partial class RootObject
        {
            [JsonProperty("lines", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public List<Line> Lines { get; set; }

            [JsonProperty("language", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public Language Language { get; set; }
        }

        public partial class Language
        {
            [JsonProperty("name", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            [JsonProperty("translations", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public Translations Translations { get; set; }
        }

        public partial class Translations
        {
        }

        public partial class Line
        {
            [JsonProperty("id", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public long? Id { get; set; }

            [JsonProperty("name", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            [JsonProperty("icon", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public Uri Icon { get; set; }

            [JsonProperty("itemClass", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public long? ItemClass { get; set; }

            [JsonProperty("sparkline", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public Sparkline Sparkline { get; set; }

            [JsonProperty("lowConfidenceSparkline", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public Sparkline LowConfidenceSparkline { get; set; }

            [JsonProperty("implicitModifiers", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public List<object> ImplicitModifiers { get; set; }

            [JsonProperty("explicitModifiers", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public List<object> ExplicitModifiers { get; set; }

            [JsonProperty("flavourText", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public string FlavourText { get; set; }

            [JsonProperty("chaosValue", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public double? ChaosValue { get; set; }

            [JsonProperty("exaltedValue", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public double? ExaltedValue { get; set; }

            [JsonProperty("count", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public long? Count { get; set; }

            [JsonProperty("detailsId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public string DetailsId { get; set; }

            [JsonProperty("listingCount", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public long? ListingCount { get; set; }
        }

        public partial class Sparkline
        {
            [JsonProperty("data", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public List<double> Data { get; set; }

            [JsonProperty("totalChange", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public double? TotalChange { get; set; }
        }
    }
}