using Flurl;
using Flurl.Http;
using HQExChecker.Clents.Utilities;
using HQExChecker.Entities;
using HQTestLib.Entities;
using System.Text.Json;

namespace HQExChecker.Clents
{

    public class BitfinexRestClient
    {
        private void SetSelectionQueryParams(IFlurlRequest request, int? limit = null, int? sort = null, long? start = null, long? end = null)
        {
            if (limit != null)
                request = request.SetQueryParam("limit", limit);
            if (sort != null)
                request = request.SetQueryParam("sort", sort);
            if (start != null)
                request = request.SetQueryParam("start", start);
            if (end != null)
                request = request.SetQueryParam("end", end);
        }

        /// <summary>
        /// The Candles endpoint provides OCHL (Open, Close, High, Low) and volume data for the specified funding currency or trading pair.
        /// The endpoint provides the last 100 candles by default, but a limit and a start and/or end timestamp can be specified.
        /// Rate Limit:	30 reqs/min
        /// </summary>
        /// <param name="pair">Trading pair</param>
        /// <param name="limit">Number of records in response (max. 10000).</param>
        /// <param name="sort">+1: sort in ascending order | -1: sort in descending order (by time).</param>
        /// <param name="start">If start is given, only records with time at least start (milliseconds) will be given as response.</param>
        /// <param name="end">If end is given, only records with time at least end (milliseconds) will be given as response.</param>
        /// <param name="section">Available values: "last", "hist".</param>
        public async Task<IEnumerable<Candle>> GetCandles(string pair, int? limit = null, int? sort = null, long? start = null, long? end = null, string section = "hist")
        {
            var request = "https://api-pub.bitfinex.com/v2/candles"
                .AppendPathSegment("trade:1m:" + pair)
                .AppendPathSegment(section)
                .WithHeader("accept", "application/json");
            SetSelectionQueryParams(request, limit, sort, start, end);

            var jsonRootArray = await request.GetJsonAsync<JsonElement>();
            var entities = jsonRootArray.EnumerateArray()
                .Select(innerArray => innerArray.CreatePairCandle(pair));

            return entities;
        }

        /// <summary>
        /// The trades endpoint allows the retrieval of past public trades and includes details such as price, size, and time.
        /// Optional parameters can be used to limit the number of results; you can specify a start and end timestamp, a limit, and a sorting method.
        /// Rate Limit:	15 reqs/min
        /// </summary>
        /// <param name="pair">Trading pair</param>
        /// <param name="limit">Number of records in response (max. 10000).</param>
        /// <param name="sort">+1: sort in ascending order | -1: sort in descending order (by time).</param>
        /// <param name="start">If start is given, only records with time at least start (milliseconds) will be given as response.</param>
        /// <param name="end">If end is given, only records with time at least end (milliseconds) will be given as response.</param>
        public async Task<IEnumerable<Trade>> GetTrades(string pair, int? limit = null, int? sort = null, long? start = null, long? end = null)
        {
            var request = "https://api-pub.bitfinex.com/v2/trades"
                .AppendPathSegment(pair)
                .AppendPathSegment("hist")
                .WithHeader("accept", "application/json");
            SetSelectionQueryParams(request, limit, sort, start, end);

            var jsonRootArray = await request.GetJsonAsync<JsonElement>();
            var entities = jsonRootArray.EnumerateArray()
                .Select(innerArray => innerArray.CreatePairTrade(pair));

            return entities;
        }

        /// <summary>
        /// The ticker endpoint provides a high level overview of the state of the market for a specified pair.
        /// It shows the current best bid and ask, the last traded price, as well as information on the daily volume and price movement over the last day.
        /// Rate Limit:	90 reqs/min
        /// </summary>
        /// <param name="pair">Trading pair</param>
        public async Task<Ticker> GetTicker(string pair)
        {
            var request = "https://api-pub.bitfinex.com/v2/ticker"
                .AppendPathSegment(pair)
                .WithHeader("accept", "application/json");

            var jsonRootElement = await request.GetJsonAsync<JsonElement>();
            var entitiy = jsonRootElement.CreateTicker();

            return entitiy;
        }
    }
}
