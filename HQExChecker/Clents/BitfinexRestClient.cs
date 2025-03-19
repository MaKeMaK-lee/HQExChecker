using Flurl;
using Flurl.Http;
using HQExChecker.Clents.Utilities;
using HQExChecker.Entities;
using HQTestLib.Entities;
using System.Text.Json;

namespace HQExChecker.Clents
{
    public class BitfinexRestClient : IBitfinexRestClient
    {
        private void SetSelectionQueryParams(IFlurlRequest request, int? limit = null, int? sort = null, long? start = null, long? end = null)
        {
            if (limit != null && limit > 0)
                request = request.SetQueryParam(BitfinexApi._getLimitPropertyNameString, limit);
            if (sort != null)
                request = request.SetQueryParam(BitfinexApi._getSortPropertyNameString, sort);
            if (start != null)
                request = request.SetQueryParam(BitfinexApi._getStartPropertyNameString, start);
            if (end != null)
                request = request.SetQueryParam(BitfinexApi._getEndPropertyNameString, end);
        }

        public async Task<IEnumerable<Candle>> GetCandles(string pair, int periodInSec, int? limit = null, int? sort = null, long? start = null, long? end = null, string? section = null)
        {
            section ??= BitfinexApi._getHistPropertyNameString;
            var key = BitfinexApi.GetAcceptedKey(pair, periodInSec);

            var request = BitfinexApi._getCandelsUrl
                .AppendPathSegment(key)
                .AppendPathSegment(section)
                .WithHeader(BitfinexApi._getAcceptJsonHeaderNameString, BitfinexApi._getAcceptJsonHeaderValueString);
            SetSelectionQueryParams(request, limit, sort, start, end);

            var jsonRootArray = await request.GetJsonAsync<JsonElement>();
            var entities = jsonRootArray.EnumerateArray()
                .Select(innerArray => innerArray.CreatePairCandle(pair));

            return entities;
        }

        public async Task<IEnumerable<Trade>> GetTrades(string pair, int? limit = null, int? sort = null, long? start = null, long? end = null)
        {
            if (limit > BitfinexApi._getTradesMaxLimit)
                limit = BitfinexApi._getTradesMaxLimit;

            var request = BitfinexApi._getTradesUrl
                .AppendPathSegment(pair)
                .AppendPathSegment(BitfinexApi._getHistPropertyNameString)
                .WithHeader(BitfinexApi._getAcceptJsonHeaderNameString, BitfinexApi._getAcceptJsonHeaderValueString);
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
            var request = BitfinexApi._getTickerUrl
                .AppendPathSegment(pair)
                .WithHeader(BitfinexApi._getAcceptJsonHeaderNameString, BitfinexApi._getAcceptJsonHeaderValueString);

            var jsonRootElement = await request.GetJsonAsync<JsonElement>();
            var entitiy = jsonRootElement.CreateTicker();

            return entitiy;
        }
    }
}
