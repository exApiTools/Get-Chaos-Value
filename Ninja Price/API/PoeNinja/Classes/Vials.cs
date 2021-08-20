using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ninja_Price.API.PoeNinja.Classes
{
    public class Vials
    {
        public partial class RootObject
        {
            public List<Line> Lines { get; set; }
            public Language Language { get; set; }
        }

        public partial class Language
        {
            public string Name { get; set; }
            public Translations Translations { get; set; }
        }

        public partial class Translations
        {
        }

        public partial class Line
        {
            public long? Id { get; set; }
            public string Name { get; set; }
            public Uri Icon { get; set; }
            public long? MapTier { get; set; }
            public long? LevelRequired { get; set; }
            public string BaseType { get; set; }
            public long? StackSize { get; set; }
            public object Variant { get; set; }
            public object ProphecyText { get; set; }
            public object ArtFilename { get; set; }
            public long? Links { get; set; }
            public long? ItemClass { get; set; }
            public Sparkline Sparkline { get; set; }
            public Sparkline LowConfidenceSparkline { get; set; }
            public List<object> ImplicitModifiers { get; set; }
            public List<ExplicitModifier> ExplicitModifiers { get; set; }
            public string FlavourText { get; set; }
            public bool? Corrupted { get; set; }
            public long? GemLevel { get; set; }
            public long? GemQuality { get; set; }
            public string ItemType { get; set; }
            public double? ChaosValue { get; set; }
            public double? ExaltedValue { get; set; }
            public long? Count { get; set; }
            public string DetailsId { get; set; }
            public object TradeInfo { get; set; }
            public object MapRegion { get; set; }
        }

        public partial class ExplicitModifier
        {
            public string Text { get; set; }
            public bool? Optional { get; set; }
        }

        public partial class Sparkline
        {
            public List<double?> Data { get; set; }
            public double? TotalChange { get; set; }
        }
    }
}
