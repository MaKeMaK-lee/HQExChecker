using HQExChecker.Entities;
using HQTestLib.Connectors;

namespace HQExChecker.Connectors
{
    public interface IBitfinexConnector : ITestConnector, IDisposable
    {
        public Task<Ticker> GetTicker(string numCurrency, string denomCurrency);
    }
}
