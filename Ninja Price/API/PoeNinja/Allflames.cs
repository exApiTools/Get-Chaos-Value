namespace Ninja_Price.API.PoeNinja;

public class Allflames
{
    public class RootObject
    {
        public Line[] lines { get; set; }
    }

    public class Line
    {
        public int id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public int levelRequired { get; set; }
        public string baseType { get; set; }
        public int? itemClass { get; set; }
        public Sparkline sparkline { get; set; }
        public Lowconfidencesparkline lowConfidenceSparkline { get; set; }
        public Implicitmodifier[] implicitModifiers { get; set; }
        public string flavourText { get; set; }
        public float? chaosValue { get; set; }
        public float? exaltedValue { get; set; }
        public float? divineValue { get; set; }
        public float? count { get; set; }
        public string detailsId { get; set; }
        public float? listingCount { get; set; }
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

    public class Implicitmodifier
    {
        public string text { get; set; }
        public bool optional { get; set; }
    }
}