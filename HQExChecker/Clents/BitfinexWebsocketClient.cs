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
        const string _unsubscribeEventName = "unsubscribe";
        const string _subscribedEventName = "subscribed";
        const string _unsubscribedEventName = "unsubscribed";

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

        /// <summary>
        /// Задача для выполнения метода ResubscribingTaskActionAsync
        /// </summary>
        private Task resubscribingTask;

        private readonly WebsocketClient _wsclient;

        private readonly LimitedChannelsDictionary _tradeChannelsCollection;

        /// <summary>
        /// Запросы на подключение к каналам (key: channelSymbol, value: maxCount)
        /// </summary>
        private readonly Dictionary<string, int> _tradeChannelsSubRequests;

        /// <summary>
        /// Запросы на отключение от каналов (string symbol)
        /// </summary>
        private readonly List<string> _tradeChannelsUnsubRequests;

        private Action<Trade> newTradeAction;

        public BitfinexWebsocketClient(Action<Trade> newTradeAction)
        {
            this.newTradeAction = newTradeAction;

            resubscribingTask = new Task(ResubscribingTaskActionAsync);

            _tradeChannelsSubRequests = [];
            _tradeChannelsUnsubRequests = [];
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
            _tradeChannelsSubRequests.Add(symbol, maxRecentCount);
            SendSubscribeTradesRequest(symbol);
        }

        public void UnsubscribeTrades(string symbol)
        {
            _tradeChannelsSubRequests.Remove(symbol);

            var channel = _tradeChannelsCollection.Channels
               .FirstOrDefault(t => t.Value.symbol == symbol);

            if (channel.Key != 0)
            {
                _tradeChannelsUnsubRequests.Add(symbol);
                SendUnsubscribeRequest(channel.Key);
            }
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

        private void SendUnsubscribeRequest(int id)
        {
            var jsonObject = new UnsubscribeObjectRequest()
            {
                eventName = _unsubscribeEventName,
                chanId = id
            };
            SendMessage(jsonObject);
        }

        private void OnReconnection(ReconnectionInfo info)
        {
            if (resubscribingTask.Status != TaskStatus.Created)
            {
                resubscribeAsyncCancellationTokenSource?.Cancel();
                resubscribingTask.Wait();
            }
            resubscribingTask.Start();
        }

        /// <summary>
        /// Действие выполнения ресаба на каналы.
        /// </summary>
        private async void ResubscribingTaskActionAsync()
        {
            IEnumerable<(string symbol, int maxCount)> channels = _tradeChannelsCollection.Channels
                .Select(channel => (channel.Value.symbol, channel.Value.maxCount))
                .Concat(_tradeChannelsSubRequests
                .Select(request => (request.Key, request.Value)))
                .Where(ch => !_tradeChannelsUnsubRequests.Any(unsR => unsR == ch.Item1)).ToList();

            _tradeChannelsUnsubRequests.Clear();
            _tradeChannelsSubRequests.Clear();
            _tradeChannelsCollection.Clear();

            resubscribeAsyncCancellationTokenSource = new CancellationTokenSource();
            await ResubscribeAsync(channels, resubscribeAsyncCancellationTokenSource.Token);

            resubscribingTask = new Task(ResubscribingTaskActionAsync);
        }

        /// <summary>
        /// Внимание! Не тестировалось при количестве каналов большем, чем минутное ограничение у api.
        /// </summary>
        private async Task ResubscribeAsync(IEnumerable<(string symbol, int maxCount)> channels, CancellationToken token)
        {
            var chunks = channels.Chunk(_maxPublicChannalConnectionsPerMinute);
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
                if (_tradeChannelsCollection.Channels.ContainsKey(channelId))
                {
                    var channelSymbol = _tradeChannelsCollection.Channels[channelId].symbol;
                    var channelMaxCount = _tradeChannelsCollection.Channels[channelId].maxCount;

                    //Если это строка
                    if (array[1].ValueKind == JsonValueKind.String)
                    {
                        //Если новый трейд
                        if (array[1].ToString() == _tradeMessageTypeExecutedString)
                        {
                            newTradeAction(array[2].CreatePairTrade(channelSymbol));
                        }
                    }
                    //Если это снапшот недавних трейдов
                    if (array[1].ValueKind == JsonValueKind.Array)
                    {
                        var snapshotArray = array[1]
                            .EnumerateArray()
                            .Select(t => t.CreatePairTrade(channelSymbol))
                            .Take(channelMaxCount)
                            .OrderBy(t => t.Time);
                        foreach (var item in snapshotArray)
                        {
                            newTradeAction(item);
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
                    var channelName = jsonObject.GetStringValueOf(_channelPropertyNameString);
                    if (channelName == _tradesChannelName)
                    {
                        HandleSubscribedTradeChannelJsonEvent(jsonObject);
                    }
                }
                //Если это сообщение об успешной отписке от канала
                if (jsonObject.GetStringValueOf(_eventPropertyNameString) == _unsubscribedEventName)
                {
                    HandleUnsubscribedChannelJsonEvent(jsonObject);
                }
            }


            var test = message.Text;
            return;
        }

        private void HandleUnsubscribedChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var chanid = jsonObject.GetIntValueOf(_channelIdPropertyNameString);
            if (_tradeChannelsCollection.Channels.ContainsKey(chanid))
            {
                var channel = _tradeChannelsCollection.Channels[chanid];
                _tradeChannelsSubRequests.Remove(channel.symbol);
            }
            _tradeChannelsCollection.Remove(chanid);
        }

        private void HandleSubscribedTradeChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var symbol = jsonObject.GetStringValueOf(_channelSymbolPropertyNameString);
            if (!_tradeChannelsSubRequests.ContainsKey(symbol))
                return;
            var maxCount = _tradeChannelsSubRequests[symbol];

            _tradeChannelsCollection.Add(
                jsonObject.GetIntValueOf(_channelIdPropertyNameString),
                jsonObject.GetStringValueOf(_channelPropertyNameString),
                symbol,
                maxCount);

            _tradeChannelsSubRequests.Remove(symbol);
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
