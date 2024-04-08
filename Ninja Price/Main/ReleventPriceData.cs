using System.Collections.Generic;
using Ninja_Price.Enums;

namespace Ninja_Price.Main;

public partial class Main
{
    public class RelevantPriceData // store data that was got from checking the item against the poe.ninja data
    {
        public double MinChaosValue { get; set; }
        public double MaxChaosValue { get; set; }
        public double ChangeInLast7Days { get; set; }
        public ItemTypes ItemType { get; set; }
        public List<double> ItemBasePrices { get; set; } = new List<double>();
        public string DetailsId { get; set; }

        public override string ToString()
        {
            return $"MinChaosValue: {MinChaosValue}, MaxChaosValue: {MaxChaosValue}, ChangeInLast7Days: {ChangeInLast7Days}, ItemType: {ItemType}, DetailsId: {DetailsId}";
        }
    }
}