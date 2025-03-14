using HQExChecker.Entities.WebsocketChannels;
using HQTestLib.Entities;

namespace HQExChecker.Clents
{
    public interface IBitfinexWebsocketClient
    {
        public event Action<Trade>? NewTradeAction;

        public event Action<Candle>? CandleProcessingAction;

        public event Action<int>? Connected;

        public event Action<int>? HandleUnsubscribedChannel;

        public event Action<string, int>? HandleSubscribedTradeChannel;

        public event Action<string, int, int>? HandleSubscribedCandleChannel;

        public Func<IReadOnlyDictionary<int, PairChannelOptions>>? GetActiveChannelsConnetcions { get; set; }

        public void SubscribeTrades(string symbol);

        public void UnsubscribeTrades(int channelId);

        /// <param name="timeFrameInSeconds">Значение будет округлено вверх до ближайшего из доступных в api </param>
        public void SubscribeCandles(string symbol, int timeFrameInSeconds);

        public void UnsubscribeCandles(int channelId);
    }
}
