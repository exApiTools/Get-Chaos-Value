using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class AllflameEmbers
{
    public class Rootobject
    {
        public List<Line> lines { get; set; }

        public class Line
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string icon { get; set; }
            public string baseType { get; set; }
            public int? stackSize { get; set; }
            public int? itemClass { get; set; }
            public Sparkline sparkline { get; set; }
            public Lowconfidencesparkline lowConfidenceSparkline { get; set; }
            public string flavourText { get; set; }
            public float? chaosValue { get; set; }
            public float? exaltedValue { get; set; }
            public float? divineValue { get; set; }
            public int? count { get; set; }
            public string detailsId { get; set; }
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
    }
}