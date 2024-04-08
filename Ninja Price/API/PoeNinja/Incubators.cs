using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class Incubators
{
    public class RootObject
    {
        public List<Line> Lines { get; set; }
        public Language Language { get; set; }
    }

    public class Language
    {
        public string Name { get; set; }
        public Translations Translations { get; set; }
    }

    public class Translations
    {
    }

    public class Line
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public Uri Icon { get; set; }
        public long? MapTier { get; set; }
        public long? LevelRequired { get; set; }
        public object BaseType { get; set; }
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
        public ItemType? ItemType { get; set; }
        public long? ChaosValue { get; set; }
        public double? ExaltedValue { get; set; }
        public double? DivineValue { get; set; }
        public long? Count { get; set; }
        public string DetailsId { get; set; }
        public object TradeInfo { get; set; }
        public object MapRegion { get; set; }
    }

    public class ExplicitModifier
    {
        public string Text { get; set; }
        public bool? Optional { get; set; }
    }

    public class Sparkline
    {
        public List<double?> Data { get; set; }
        public double? TotalChange { get; set; }
    }

    public enum ItemType
    {
        Unknown
    };
}