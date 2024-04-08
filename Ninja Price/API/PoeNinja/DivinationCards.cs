using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class DivinationCards
{
    public class RootObject
    {
        [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)]
        public List<LineElement> Lines { get; set; }

        [JsonProperty("currencyDetails", NullValueHandling = NullValueHandling.Ignore)]
        public List<CurrencyDetail> CurrencyDetails { get; set; }
    }

    public class CurrencyDetail
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Icon { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("poeTradeId", NullValueHandling = NullValueHandling.Ignore)]
        public long? PoeTradeId { get; set; }
    }

    public class LineElement
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Icon { get; set; }

        [JsonProperty("mapTier", NullValueHandling = NullValueHandling.Ignore)]
        public long? MapTier { get; set; }

        [JsonProperty("levelRequired", NullValueHandling = NullValueHandling.Ignore)]
        public long? LevelRequired { get; set; }

        [JsonProperty("baseType")]
        public object BaseType { get; set; }

        [JsonProperty("stackSize", NullValueHandling = NullValueHandling.Ignore)]
        public long? StackSize { get; set; }

        [JsonProperty("variant")]
        public object Variant { get; set; }

        [JsonProperty("prophecyText")]
        public object ProphecyText { get; set; }

        [JsonProperty("artFilename", NullValueHandling = NullValueHandling.Ignore)]
        public string ArtFilename { get; set; }

        [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
        public long? Links { get; set; }

        [JsonProperty("itemClass", NullValueHandling = NullValueHandling.Ignore)]
        public long? ItemClass { get; set; }

        [JsonProperty("sparkline", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidencePaySparkLineClass Sparkline { get; set; }

        [JsonProperty("lowConfidenceSparkline", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidencePaySparkLineClass LowConfidenceSparkline { get; set; }

        [JsonProperty("implicitModifiers", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> ImplicitModifiers { get; set; }

        [JsonProperty("explicitModifiers", NullValueHandling = NullValueHandling.Ignore)]
        public List<ExplicitModifier> ExplicitModifiers { get; set; }

        [JsonProperty("flavourText", NullValueHandling = NullValueHandling.Ignore)]
        public string FlavourText { get; set; }

        [JsonProperty("corrupted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Corrupted { get; set; }

        [JsonProperty("gemLevel", NullValueHandling = NullValueHandling.Ignore)]
        public long? GemLevel { get; set; }

        [JsonProperty("gemQuality", NullValueHandling = NullValueHandling.Ignore)]
        public long? GemQuality { get; set; }


        [JsonProperty("chaosValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? ChaosValue { get; set; }

        [JsonProperty("exaltedValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? ExaltedValue { get; set; }

        [JsonProperty("divineValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? DivineValue { get; set; }

        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public long? Count { get; set; }

        [JsonProperty("detailsId", NullValueHandling = NullValueHandling.Ignore)]
        public string DetailsId { get; set; }

        [JsonProperty("currencyTypeName", NullValueHandling = NullValueHandling.Ignore)]
        public string CurrencyTypeName { get; set; }

        [JsonProperty("pay")]
        public Receive Pay { get; set; }

        [JsonProperty("receive", NullValueHandling = NullValueHandling.Ignore)]
        public Receive Receive { get; set; }

        [JsonProperty("paySparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidencePaySparkLineClass PaySparkLine { get; set; }

        [JsonProperty("receiveSparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidencePaySparkLineClass ReceiveSparkLine { get; set; }

        [JsonProperty("chaosEquivalent", NullValueHandling = NullValueHandling.Ignore)]
        public double? ChaosEquivalent { get; set; }

        [JsonProperty("lowConfidencePaySparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidencePaySparkLineClass LowConfidencePaySparkLine { get; set; }

        [JsonProperty("lowConfidenceReceiveSparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public LowConfidenceReceiveSparkLine LowConfidenceReceiveSparkLine { get; set; }
    }

    public class ExplicitModifier
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("optional", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Optional { get; set; }
    }

    public class LowConfidencePaySparkLineClass
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public List<double?> Data { get; set; }

        [JsonProperty("totalChange", NullValueHandling = NullValueHandling.Ignore)]
        public double? TotalChange { get; set; }
    }

    public class LowConfidenceReceiveSparkLine
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> Data { get; set; }

        [JsonProperty("totalChange", NullValueHandling = NullValueHandling.Ignore)]
        public double? TotalChange { get; set; }
    }

    public class Receive
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("league_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? LeagueId { get; set; }

        [JsonProperty("pay_currency_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? PayCurrencyId { get; set; }

        [JsonProperty("get_currency_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? GetCurrencyId { get; set; }

        [JsonProperty("sample_time_utc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? SampleTimeUtc { get; set; }

        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public long? Count { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public double? Value { get; set; }

        [JsonProperty("data_point_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? DataPointCount { get; set; }

        [JsonProperty("includes_secondary", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludesSecondary { get; set; }
    }

}