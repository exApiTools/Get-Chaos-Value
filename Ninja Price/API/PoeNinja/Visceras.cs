using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class Visceras
{
    public class RootObject
    {
        public List<Line> Lines { get; set; }
    }

    public class Line
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public Uri Icon { get; set; }
        public string Variant { get; set; }
        public Sparkline Sparkline { get; set; }
        public List<ImplicitModifier> ImplicitModifiers { get; set; }
        public string ItemType { get; set; }
        public double? ChaosValue { get; set; }
        public double? ExaltedValue { get; set; }
        public double? DivineValue { get; set; }
        public string DetailsId { get; set; }
    }

    public class ImplicitModifier
    {
        public string Text { get; set; }
        public bool? Optional { get; set; }
    }

    public class Sparkline
    {
        public List<double?> Data { get; set; }
        public double? TotalChange { get; set; }
    }
}