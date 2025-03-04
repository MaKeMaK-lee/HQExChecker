using HQExChecker.Entities;
using HQTestLib.Entities;
using System.Globalization;
using System.Text.Json;

namespace HQExChecker.Clents.Utilities
{
    public static class EntitiyCreatingExtensions
    {
        private static decimal DecimalFromInt(JsonElement element) => decimal.Parse(element.ToString()!);
        private static decimal Decimal(JsonElement element) => decimal.Parse(element.ToString()!, CultureInfo.InvariantCulture);
        private static DateTimeOffset DateTimeOffsetFromInt(JsonElement element) => DateTimeOffset.FromUnixTimeMilliseconds(element.GetInt64());

        public static Ticker CreateTicker(this JsonElement innerArray)
        {
            return new Ticker
            {
                Bid = Decimal(innerArray[0]),
                BidSize = Decimal(innerArray[1]),
                Ask = Decimal(innerArray[2]),
                AskSize = Decimal(innerArray[3]),
                DailyChange = Decimal(innerArray[4]),
                DailyChangeRelative = Decimal(innerArray[5]),
                LastPrice = Decimal(innerArray[6]),
                Volume = Decimal(innerArray[7]),
                High = Decimal(innerArray[8]),
                Low = Decimal(innerArray[9])
            };
        }

        public static Trade CreatePairTrade(this JsonElement innerArray, string pair)
        {
            decimal amount = Decimal(innerArray[2]);
            string side = amount < 0 ? "sell" : "buy";
            amount = Math.Abs(amount);

            return new Trade()
            {
                Id = innerArray[0].ToString()!,
                Time = DateTimeOffsetFromInt(innerArray[1]),
                Amount = amount,
                Side = side,
                Price = Decimal(innerArray[3]),
                Pair = pair
            };
        }

        public static Candle CreatePairCandle(this JsonElement innerArray, string pair)
        {
            decimal openPrice = DecimalFromInt(innerArray[1]);
            decimal closePrice = DecimalFromInt(innerArray[2]);
            decimal highPrice = DecimalFromInt(innerArray[3]);
            decimal lowPrice = DecimalFromInt(innerArray[4]);
            decimal totalVolume = Decimal(innerArray[5]);
            decimal totalPrice = totalVolume * ((openPrice + closePrice + lowPrice + highPrice) / 4);

            return new Candle()
            {
                Pair = pair,
                OpenTime = DateTimeOffsetFromInt(innerArray[0]),
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
