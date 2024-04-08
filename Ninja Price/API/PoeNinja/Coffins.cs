using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class Coffins
{
    public class RootObject
    {
        public List<Line> lines { get; set; }
    }

    public class Line
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public int? levelRequired { get; set; }
        public string baseType { get; set; }
        public int? itemClass { get; set; }
        public Sparkline sparkline { get; set; }
        public Lowconfidencesparkline lowConfidenceSparkline { get; set; }
        public Implicitmodifier[] implicitModifiers { get; set; }
        public object[] explicitModifiers { get; set; }
        public string flavourText { get; set; }
        public float? chaosValue { get; set; }
        public float? exaltedValue { get; set; }
        public float? divineValue { get; set; }
        public int? count { get; set; }
        public string detailsId { get; set; }
        public object[] tradeInfo { get; set; }
        public Tradefilter tradeFilter { get; set; }
        public int? listingCount { get; set; }
    }

    public class Sparkline
    {
        public float?[] data { get; set; }
        public float? totalChange { get; set; }
    }

    public class Lowconfidencesparkline
    {
        public float?[] data { get; set; }
        public float? totalChange { get; set; }
    }

    public class Tradefilter
    {
        public Query query { get; set; }
    }

    public class Query
    {
        public Type type { get; set; }
        public Stat[] stats { get; set; }
    }

    public class Type
    {
        public string option { get; set; }
    }

    public class Stat
    {
        public string type { get; set; }
        public Filter[] filters { get; set; }
    }

    public class Filter
    {
        public string id { get; set; }
        public Value value { get; set; }
    }

    public class Value
    {
        public int min { get; set; }
        public int max { get; set; }
    }

    public class Implicitmodifier
    {
        public string text { get; set; }
        public bool optional { get; set; }
    }
}