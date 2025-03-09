using HQExChecker.Clents.Utilities;
using HQExChecker.Clents.Utilities.Entities;
using HQTestLib.Entities;
using System.Net.WebSockets;
using System.Text.Json;
using Websocket.Client;

namespace HQExChecker.Clents
{
    public class BitfinexWebsocketClient : IDisposable
    {
        #region ApiDocs
        //Сведения получены от поставщика API https://docs.bitfinex.com/ для APIv2 в марте 2025 года.

        const int _maxPublicChannalConnectionsPerMinute = 20;
        const int _maxPublicChannalConnectionsPerTime = 25;
        const string _publicWssConnectionUrl = "wss://api-pub.bitfinex.com/ws/2";

        const string _subscribeEventName = "subscribe";
        const string _subscribedEventName = "subscribed";

        const string _tradesChannelName = "trades";

        const string _eventPropertyNameString = "event";
        const string _channelPropertyNameString = "channel";
        const string _channelIdPropertyNameString = "chanId";
        const string _channelSymbolPropertyNameString = "symbol";

        const string _tradeMessageTypeUpdateString = "tu";
        const string _tradeMessageTypeExecutedString = "te";
        #endregion

        private bool disposed = false;
        private CancellationTokenSource? resubscribeAsyncCancellationTokenSource;
        private Task resubscribingTask;

        private readonly WebsocketClient _wsclient;

        private LimitedChannelsDictionary _tradeChannelsCollection;

        public Action<Trade> NewTradeAction;

        /// <summary>
        /// Запросы на подключение к каналам (key: channelSymbol, value: maxCount)
        /// </summary>
        private Dictionary<string, int> _tradeChannelRequests;

        public BitfinexWebsocketClient(Action<Trade> newTradeAction)
        {
            NewTradeAction = newTradeAction;

            resubscribingTask = new Task(ResubscribingTaskActionAsync);

            _tradeChannelRequests = new Dictionary<string, int>();
            _tradeChannelsCollection = new(_maxPublicChannalConnectionsPerTime);


            _wsclient = CreateWSClient();
            _wsclient.StartOrFail();
        }

        private WebsocketClient CreateWSClient()
        {
            var wsClient = new WebsocketClient(new Uri(_publicWssConnectionUrl))
            {
                ReconnectTimeout = TimeSpan.FromSeconds(30)
            };
            wsClient.MessageReceived.Subscribe(HandleMessage);
            wsClient.ReconnectionHappened.Subscribe(OnReconnection);
            return wsClient;
        }

        public void SubscribeTrades(string symbol, int maxRecentCount)
        {
            _tradeChannelRequests.Add(symbol, maxRecentCount);
            SendSubscribeTradesRequest(symbol);
        }

        private void SendSubscribeTradesRequest(string symbol)
        {
            var jsonObject = new SubscribeTradesObjectRequest()
            {
                eventName = _subscribeEventName,
                channel = _tradesChannelName,
                symbol = symbol
            };
            SendMessage(jsonObject);
        }

        private void OnReconnection(ReconnectionInfo info)
        {
            resubscribingTask.Start();
        }

        private async void ResubscribingTaskActionAsync()
        {
            if (resubscribingTask.Status != TaskStatus.Created)
            {
                resubscribeAsyncCancellationTokenSource?.Cancel();
                resubscribingTask.Wait();
            }

            resubscribeAsyncCancellationTokenSource = new CancellationTokenSource();
            await ResubscribeAsync(resubscribeAsyncCancellationTokenSource.Token);

            resubscribingTask = new Task(ResubscribingTaskActionAsync);
        }

        /// <summary>
        /// Внимание! Не тестировалось при количестве каналов большем, чем минутное ограничение у api.
        /// </summary>
        /// <returns></returns>
        private async Task ResubscribeAsync(CancellationToken token)
        {
            IEnumerable<(string symbol, int maxCount)> targets = _tradeChannelsCollection.Channels
                .Select(channel => (channel.Value.symbol, channel.Value.maxCount))
                .Concat(_tradeChannelRequests
                .Select(request => (request.Key, request.Value)));

            var chunks = targets.Chunk(_maxPublicChannalConnectionsPerMinute);
            foreach (var requestsChunk in chunks)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                foreach (var request in requestsChunk)
                {
                    SubscribeTrades(request.symbol, request.maxCount);
                }
                if (requestsChunk == chunks.Last())
                {
                    await Task.Delay(61 * 1000, token);
                }
            }
        }

        private void HandleMessage(ResponseMessage message)
        {
            if (message.MessageType != WebSocketMessageType.Text)
                return;

            var rootElement = JsonSerializer.Deserialize<JsonElement>(message.Text!)!;

            //В виде массивов здесь обычно данные
            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                var array = rootElement.EnumerateArray().ToArray();

                //Если это канал трейдов
                var channelId = array[0].GetInt32();
                var channelSymbol = _tradeChannelsCollection.Channels[channelId].symbol;
                if (_tradeChannelsCollection.Channels.ContainsKey(channelId))
                {
                    //Если это строка
                    if (array[1].ValueKind == JsonValueKind.String)
                    {
                        //Если новый трейд
                        if (array[1].ToString() == _tradeMessageTypeExecutedString)
                        {
                            NewTradeAction(array[2].CreatePairTrade(channelSymbol));
                        }
                    }
                    //Если это снапшот недавних трейдов
                    if (array[1].ValueKind == JsonValueKind.Array)
                    {
                        var snapshotArray = array[1].EnumerateArray();
                        foreach (var jsonElement in snapshotArray)
                        {
                            NewTradeAction(jsonElement.CreatePairTrade(channelSymbol));
                        }
                    }

                }
            }

            //В виде объектов здесь обычно информационные сообщения и ответы
            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                var jsonObject = rootElement.EnumerateObject().ToArray();

                //Если это сообщение об успешной подписке на канал
                if (jsonObject.GetStringValueOf(_eventPropertyNameString) == _subscribedEventName)
                {
                    var channel = jsonObject.GetStringValueOf(_channelPropertyNameString);

                    if (channel == _tradesChannelName)
                    {
                        HandleSubscribedTradeChannelJsonEvent(jsonObject);
                    }
                }
            }


            var test = message.Text;
            return;
        }



        private void HandleSubscribedTradeChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var symbol = jsonObject.GetStringValueOf(_channelSymbolPropertyNameString);
            var maxCount = _tradeChannelRequests[symbol];

            _tradeChannelsCollection.Add(
                jsonObject.GetIntValueOf(_channelIdPropertyNameString),
                jsonObject.GetStringValueOf(_channelPropertyNameString),
                symbol,
                maxCount);

            _tradeChannelRequests.Remove(symbol);
        }

        private void SendMessage(object message)
        {
            var text = JsonSerializer.Serialize(message);
            _wsclient.Send(text);
        }

        private void SendMessage(string message)
        {
            _wsclient.Send(message);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            _wsclient.Dispose();

            GC.SuppressFinalize(this);
            disposed = true;
        }


    }
}
