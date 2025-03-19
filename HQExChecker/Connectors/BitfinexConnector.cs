using HQExChecker.Clents;
using HQExChecker.Entities.WebsocketChannels;
using HQTestLib.Connectors;
using HQTestLib.Entities;

namespace HQExChecker.Connectors
{
    public class BitfinexConnector : ITestConnector, IDisposable
    {
        private IBitfinexRestClient _restClient;

        private IBitfinexWebsocketClient _websocketClient;

        /// <summary>
        /// Keys - channel ids
        /// </summary>
        private readonly Dictionary<int, PairChannelOptions> _activeChannels;

        /// <summary>
        /// Keys - channel pairs
        /// </summary>
        private readonly Dictionary<string, TradeChannelOptions> _tradeChannelsSubRequests;

        /// <summary>
        /// Keys - channel pairs
        /// </summary>
        private readonly Dictionary<string, CandleChannelOptions> _candleChannelsSubRequests;

        /// <summary>
        /// Channel pairs
        /// </summary>
        private readonly HashSet<string> _tradeChannelsUnsubRequests;

        /// <summary>
        /// Channel pairs
        /// </summary>
        private readonly HashSet<string> _candleChannelsUnsubRequests;

        private bool disposed;

        private int bitfinexWebsocketClientApiMaxConnectionsPerMinute;

        /// <summary>
        /// Задача для выполнения метода ResubscribingTaskActionAsync
        /// </summary>
        private Task resubscribingTask;

        private CancellationTokenSource? resubscribeAsyncCancellationTokenSource;

        public BitfinexConnector(IBitfinexRestClient bitfinexRestClient, IBitfinexWebsocketClient bitfinexWebsocketClient)
        {
            _tradeChannelsSubRequests = [];
            _candleChannelsSubRequests = [];
            _tradeChannelsUnsubRequests = [];
            _candleChannelsUnsubRequests = [];
            _activeChannels = [];

            resubscribingTask = new Task(ResubscribingTaskActionAsync);

            _restClient = bitfinexRestClient;
            _websocketClient = bitfinexWebsocketClient;
            _websocketClient.GetActiveChannelsConnetcions = () => _activeChannels.AsReadOnly();
            _websocketClient.NewTradeAction += OnNewTrade;
            _websocketClient.CandleProcessingAction += OnNewCandle;
            _websocketClient.Connected += OnWebsocketConnect;
            _websocketClient.HandleUnsubscribedChannel += OnChannelUnsubscribed;
            _websocketClient.HandleSubscribedTradeChannel += OnTradeChannelSubscribed;
            _websocketClient.HandleSubscribedCandleChannel += OnCandleChannelSubscribed;
        }

        private void OnTradeChannelSubscribed(string pair, int id)
        {
            if (!_tradeChannelsSubRequests.TryGetValue(pair, out var channel))
                return;
            _activeChannels.Add(id, channel);
            _tradeChannelsSubRequests.Remove(pair);
        }

        private void OnCandleChannelSubscribed(string pair, int id, int acceptedTimeFrame)
        {
            if (!_candleChannelsSubRequests.TryGetValue(pair, out var channel))
                return;
            channel.AcceptedPeriodInSec = acceptedTimeFrame;
            _activeChannels.Add(id, channel);
            _candleChannelsSubRequests.Remove(pair);
        }

        private void OnChannelUnsubscribed(int id)
        {
            var active = _activeChannels.TryGetValue(id, out PairChannelOptions? channel);
            if (active)
            {
                _tradeChannelsSubRequests.Remove(channel!.Pair);
            }
            _activeChannels.Remove(id);
        }

        private void OnNewTrade(Trade newTrade)
        {
            var channel = _activeChannels.Values
                .Where(ch => ch.Pair == newTrade.Pair)
                .Select(ch => ch as TradeChannelOptions)
                .FirstOrDefault(ch => ch != null);

            if (channel == null)
                return;
            if (channel.MaxCount > 0 && channel.CurrentCount >= channel.MaxCount)
            {
                UnsubscribeTrades(channel.Pair);
                return;
            }

            if (newTrade.Side == "buy")
                NewBuyTrade?.Invoke(newTrade);
            if (newTrade.Side == "sell")
                NewSellTrade?.Invoke(newTrade);

            channel!.CurrentCount++;
        }

        private void OnNewCandle(Candle candle)
        {
            var channel = _activeChannels.Values
                .Where(ch => ch.Pair == candle.Pair)
                .Select(ch => ch as CandleChannelOptions)
                .FirstOrDefault(ch => ch != null);

            if (channel == null)
                return;
            if (channel.MaxCount > 0 && channel.CurrentCount >= channel.MaxCount)
                return;
            if (channel.From != null && !(channel.From <= candle.OpenTime.AddSeconds((double)channel.AcceptedPeriodInSec!)))
                return;
            if (channel.To != null && !(candle.OpenTime <= channel.To))
                return;

            CandleSeriesProcessing?.Invoke(candle);

            if (channel.CurrentProcessedFromDateTimes.Contains(candle.OpenTime))
                return;
            channel.CurrentProcessedFromDateTimes.Add(candle.OpenTime);
            channel!.CurrentCount++;
        }

        private void OnWebsocketConnect(int maxConnectionsPerMinute = int.MaxValue)
        {
            if (resubscribingTask.Status != TaskStatus.Created)
            {
                resubscribeAsyncCancellationTokenSource?.Cancel();
                resubscribingTask.Wait();
            }
            bitfinexWebsocketClientApiMaxConnectionsPerMinute = maxConnectionsPerMinute;
            resubscribingTask.Start();
        }

        /// <summary>
        /// Действие выполнения ресаба на каналы.
        /// </summary>
        private async void ResubscribingTaskActionAsync()
        {
            var channels = _activeChannels.Values
                .Concat(_candleChannelsSubRequests.Values)
                .Concat(_tradeChannelsSubRequests.Values)
                .Where(ch => !_tradeChannelsUnsubRequests.Any(unsubPair => unsubPair == ch.Pair))
                .Where(ch => !_candleChannelsUnsubRequests.Any(unsubPair => unsubPair == ch.Pair))
                .ToList();
            _tradeChannelsUnsubRequests.Clear();
            _tradeChannelsSubRequests.Clear();
            _candleChannelsUnsubRequests.Clear();
            _candleChannelsSubRequests.Clear();
            _activeChannels.Clear();

            resubscribeAsyncCancellationTokenSource = new CancellationTokenSource();
            await ResubscribeAsync(channels, resubscribeAsyncCancellationTokenSource.Token);

            resubscribingTask = new Task(ResubscribingTaskActionAsync);
        }

        private async Task ResubscribeAsync(IEnumerable<PairChannelOptions> channels, CancellationToken token)
        {

            var chunks = channels.Chunk(bitfinexWebsocketClientApiMaxConnectionsPerMinute);
            foreach (var requestsChunk in chunks)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                foreach (var request in requestsChunk)
                {
                    SubscribeChannel(request);
                }
                if (requestsChunk == chunks.Last())
                {
                    await Task.Delay(61 * 1000, token);
                }
            }
        }

        #region ITestConnector REST

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
            => await _restClient.GetTrades(pair, maxCount);

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
            => await _restClient.GetCandles(pair, periodInSec, limit: (int?)count, start: from?.ToUnixTimeMilliseconds(), end: to?.ToUnixTimeMilliseconds());

        #endregion

        #region ITestConnector REST WebSocket

        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            var request = new TradeChannelOptions() { Pair = pair, MaxCount = maxCount };
            _tradeChannelsSubRequests[pair] = request;
            _websocketClient.SubscribeTrades(pair);
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            var request = new CandleChannelOptions() { Pair = pair, PeriodInSec = periodInSec, From = from, To = to, MaxCount = count };
            _candleChannelsSubRequests[pair] = request;
            _websocketClient.SubscribeCandles(pair, periodInSec);
        }

        public void UnsubscribeTrades(string pair)
        {
            _tradeChannelsSubRequests.Remove(pair);

            var channel = _activeChannels.FirstOrDefault(ch => (ch.Value as TradeChannelOptions)?.Pair == pair);
            //Если канал не найден
            if (channel.Key == 0)
                return;

            _tradeChannelsUnsubRequests.Add(pair);
            _websocketClient.UnsubscribeTrades(channel.Key);
        }

        public void UnsubscribeCandles(string pair)
        {
            _candleChannelsSubRequests.Remove(pair);

            var channel = _activeChannels.FirstOrDefault(ch => (ch.Value as CandleChannelOptions)?.Pair == pair);
            //Если канал не найден
            if (channel.Key == 0)
                return;

            _candleChannelsUnsubRequests.Add(pair);
            _websocketClient.UnsubscribeCandles(channel.Key);
        }

        #endregion

        public void SubscribeChannel(PairChannelOptions request)
        {
            switch (request)
            {
                case TradeChannelOptions tradeRequest:
                    _tradeChannelsSubRequests[tradeRequest.Pair] = tradeRequest;
                    _websocketClient.SubscribeTrades(tradeRequest.Pair);
                    break;
                case CandleChannelOptions candleRequest:
                    _candleChannelsSubRequests[request.Pair] = candleRequest;
                    _websocketClient.SubscribeCandles(request.Pair, candleRequest.PeriodInSec);
                    break;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            (_websocketClient as IDisposable)?.Dispose();

            GC.SuppressFinalize(this);
            disposed = true;
        }
    }
}
