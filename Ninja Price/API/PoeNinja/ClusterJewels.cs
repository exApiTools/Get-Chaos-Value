namespace Ninja_Price.API.PoeNinja;

public class ClusterJewelNinjaData
{
    public Line[] Lines { get; set; }

    public class Line
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int LevelRequired { get; set; }
        public string BaseType { get; set; }
        public string Variant { get; set; }
        public int ItemClass { get; set; }
        public Sparkline Sparkline { get; set; }
        public Lowconfidencesparkline LowConfidenceSparkline { get; set; }
        public object[] ImplicitModifiers { get; set; }
        public object[] ExplicitModifiers { get; set; }
        public string FlavourText { get; set; }
        public string ItemType { get; set; }
        public double ChaosValue { get; set; }
        public double ExaltedValue { get; set; }
        public double DivineValue { get; set; }
        public int Count { get; set; }
        public string DetailsId { get; set; }
        public Tradeinfo[] TradeInfo { get; set; }
        public int ListingCount { get; set; }
    }

    public class Sparkline
    {
        public double?[] Data { get; set; }
        public double TotalChange { get; set; }
    }

    public class Lowconfidencesparkline
    {
        public double?[] Data { get; set; }
        public double TotalChange { get; set; }
    }

    public class Tradeinfo
    {
        public string Mod { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public string Option { get; set; }
    }
}