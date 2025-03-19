using HQExChecker.Entities;
using HQTestLib.Entities;

namespace HQExChecker.Clents
{
    public interface IBitfinexRestClient
    {
        /// <summary>
        /// The Candles endpoint provides OCHL (Open, Close, High, Low) and volume data for the specified funding currency or trading pair.
        /// <br/>The endpoint provides the last 100 candles by default, but a limit and a start and/or end timestamp can be specified.
        /// <br/>Rate Limit:	30 reqs/min
        /// </summary>
        /// <param name="pair">Trading pair</param>
        /// <param name="periodInSec">Candle period in seconds. The value will be rounded up to the nearest one available in the api.</param>
        /// <param name="limit">Number of records in response (max. 10000).</param>
        /// <param name="sort">+1: sort in ascending order | -1: sort in descending order (by time).</param>
        /// <param name="start">If start is given, only records with time at least start (milliseconds) will be given as response.</param>
        /// <param name="end">If end is given, only records with time at least end (milliseconds) will be given as response.</param>
        /// <param name="section">Available values: "last", "hist".</param>
        public Task<IEnumerable<Candle>> GetCandles(
            string pair,
            int periodInSec,
            int? limit = null,
            int? sort = null,
            long? start = null,
            long? end = null,
            string? section = null);

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
        public Task<IEnumerable<Trade>> GetTrades(
            string pair,
            int? limit = null,
            int? sort = null,
            long? start = null,
            long? end = null);

        public Task<Ticker> GetTicker(string pair);

    }
}
