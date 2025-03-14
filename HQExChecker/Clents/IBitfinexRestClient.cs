using HQExChecker.Entities;
using HQTestLib.Entities;

namespace HQExChecker.Clents
{
    public interface IBitfinexRestClient
    {
        /// <param name="periodInSec">Значение будет округлено вверх до ближайшего из доступных в api </param>
        public Task<IEnumerable<Candle>> GetCandles(
            string pair,
            int periodInSec,
            int? limit = null,
            int? sort = null,
            long? start = null,
            long? end = null,
            string section = null!);

        public Task<IEnumerable<Trade>> GetTrades(
            string pair,
            int? limit = null,
            int? sort = null,
            long? start = null,
            long? end = null);

        public Task<Ticker> GetTicker(string pair);

    }
}
