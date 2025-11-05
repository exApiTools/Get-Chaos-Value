using System.Collections.Generic;
using System.Linq;

namespace Ninja_Price.API.PoeNinja;

public class CurrencyOverviewData
{
    public class RootObject
    {
        public Core core { get; set; }
        public Line[] lines { get; set; }
        public Item[] items { get; set; }

        private Dictionary<string, (Line l, Item i, float ChaosPrice)> _linesByName;

        public Dictionary<string, (Line Line, Item Item, float ChaosEquivalent)> LinesByName
        {
            get
            {
                if (_linesByName == null)
                {
                    var primaryChaosRate = core.primary switch { "divine" => core.rates.chaos.Value, "chaos" => 1 };
                    _linesByName = items.Join(lines, i => i.id, l => l.id, (i, l) => (i, l))
                        .ToDictionary(p => p.i.name, p => (p.l, p.i, (p.l.primaryValue ?? 0) * primaryChaosRate));
                }

                return _linesByName;
            }
        }
    }

    public class Core
    {
        public Item[] items { get; set; }
        public Rates rates { get; set; }
        public string primary { get; set; }
        public string secondary { get; set; }
    }

    public class Rates
    {
        public float? exalted { get; set; }
        public float? chaos { get; set; }
        public float? divine { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string category { get; set; }
        public string detailsId { get; set; }
    }

    public class Line
    {
        public string id { get; set; }
        public float? primaryValue { get; set; }
        public float? volumePrimaryValue { get; set; }
        public string maxVolumeCurrency { get; set; }
        public float? maxVolumeRate { get; set; }
        public Sparkline sparkline { get; set; }
    }

    public class Sparkline
    {
        public float? totalChange { get; set; }
        public float?[] data { get; set; }
    }
}