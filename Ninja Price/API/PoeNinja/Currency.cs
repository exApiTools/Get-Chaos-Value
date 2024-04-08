using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ninja_Price.API.PoeNinja;

public class Currency
{
    public record SearchResult(double ChaosEquivalent, double PriceChange, string DetailsId);

    public class RootObject
    {
        [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)]
        public List<Line> Lines { get; set; }

        [JsonProperty("currencyDetails", NullValueHandling = NullValueHandling.Ignore)]
        public List<CurrencyDetail> CurrencyDetails { get; set; }

        public SearchResult FindLine(string name, bool useChaosEquivalent)
        {
            var line = Lines.Find(x => x.CurrencyTypeName == name);
            if (line == null)
            {
                return null;
            }

            var value = line.ChaosEquivalent;
            if (!useChaosEquivalent &&
                line.Receive?.Value is { } receiveValue &&
                CurrencyDetails.Find(x => x.Id == line.Receive.PayCurrencyId)?.TradeId == "chaos")
            {
                value = receiveValue;
            }

            return new SearchResult(value ?? 0, line.ReceiveSparkLine.TotalChange ?? 0, line.DetailsId);
        }
    }

    public class CurrencyDetail
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Icon { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("tradeId", NullValueHandling = NullValueHandling.Ignore)]
        public string TradeId { get; set; }
    }

    public class Line
    {
        [JsonProperty("currencyTypeName", NullValueHandling = NullValueHandling.Ignore)]
        public string CurrencyTypeName { get; set; }

        [JsonProperty("pay")]
        public Receive Pay { get; set; }

        [JsonProperty("receive", NullValueHandling = NullValueHandling.Ignore)]
        public Receive Receive { get; set; }

        [JsonProperty("paySparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public SparkLine PaySparkLine { get; set; }

        [JsonProperty("receiveSparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public SparkLine ReceiveSparkLine { get; set; }

        [JsonProperty("chaosEquivalent", NullValueHandling = NullValueHandling.Ignore)]
        public double? ChaosEquivalent { get; set; }

        [JsonProperty("lowConfidencePaySparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public SparkLine LowConfidencePaySparkLine { get; set; }

        [JsonProperty("lowConfidenceReceiveSparkLine", NullValueHandling = NullValueHandling.Ignore)]
        public SparkLine LowConfidenceReceiveSparkLine { get; set; }

        [JsonProperty("detailsId", NullValueHandling = NullValueHandling.Ignore)]
        public string DetailsId { get; set; }
    }

    public class SparkLine
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public List<double?> Data { get; set; }

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