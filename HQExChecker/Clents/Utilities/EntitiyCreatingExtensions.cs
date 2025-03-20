using HQExChecker.Entities;
using HQTestLib.Entities;
using System.Globalization;
using System.Text.Json;

namespace HQExChecker.Clents.Utilities
{
    public static class EntitiyCreatingExtensions
    {
        private static decimal Decimal(JsonElement element) => decimal.Parse(element.ToString()!, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture);
        private static DateTimeOffset DateTimeOffsetFromInt(JsonElement element) => DateTimeOffset.FromUnixTimeMilliseconds(element.GetInt64());

        public static Ticker CreateTicker(this JsonElement jsonArray)
        {
            return new Ticker
            {
                Bid = Decimal(jsonArray[0]),
                BidSize = Decimal(jsonArray[1]),
                Ask = Decimal(jsonArray[2]),
                AskSize = Decimal(jsonArray[3]),
                DailyChange = Decimal(jsonArray[4]),
                DailyChangeRelative = Decimal(jsonArray[5]),
                LastPrice = Decimal(jsonArray[6]),
                Volume = Decimal(jsonArray[7]),
                High = Decimal(jsonArray[8]),
                Low = Decimal(jsonArray[9])
            };
        }

        public static Trade CreatePairTrade(this JsonElement jsonArray, string pair)
        {
            decimal amount = Decimal(jsonArray[2]);
            string side = amount < 0 ? "sell" : "buy";
            amount = Math.Abs(amount);

            return new Trade()
            {
                Id = jsonArray[0].ToString()!,
                Time = DateTimeOffsetFromInt(jsonArray[1]),
                Amount = amount,
                Side = side,
                Price = Decimal(jsonArray[3]),
                Pair = pair
            };
        }

        public static Candle CreatePairCandle(this JsonElement jsonArray, string pair)
        {
            decimal openPrice = Decimal(jsonArray[1]);
            decimal closePrice = Decimal(jsonArray[2]);
            decimal highPrice = Decimal(jsonArray[3]);
            decimal lowPrice = Decimal(jsonArray[4]);
            decimal totalVolume = Decimal(jsonArray[5]);
            decimal totalPrice = totalVolume * ((openPrice + closePrice + lowPrice + highPrice) / 4);

            return new Candle()
            {
                Pair = pair,
                OpenTime = DateTimeOffsetFromInt(jsonArray[0]),
                OpenPrice = openPrice,
                ClosePrice = closePrice,
                HighPrice = highPrice,
                LowPrice = lowPrice,
                TotalVolume = totalVolume,
                TotalPrice = totalPrice
            };
        }
    }
}
