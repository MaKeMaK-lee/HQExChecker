using HQExChecker.Clents.Utilities;
using HQExChecker.Clents.Utilities.Entities;
using HQExChecker.Entities.WebsocketChannels;
using HQTestLib.Entities;
using System.Net.WebSockets;
using System.Text.Json;
using Websocket.Client;

namespace HQExChecker.Clents
{
    public class BitfinexWebsocketClient : IBitfinexWebsocketClient, IDisposable
    {
        private bool disposed = false;

        private readonly WebsocketClient _wsclient;

        public event Action<Trade>? NewTradeAction;

        public event Action<Candle>? CandleProcessingAction;

        public event Action<int>? Connected;

        public event Action<int>? HandleUnsubscribedChannel;

        public event Action<string, int>? HandleSubscribedTradeChannel;

        public event Action<string, int, int>? HandleSubscribedCandleChannel;

        public Func<IReadOnlyDictionary<int, PairChannelOptions>>? GetActiveChannelsConnetcions { get; set; }

        public BitfinexWebsocketClient()
        {
            _wsclient = CreateWSClient();
            _wsclient.StartOrFail();
        }

        private WebsocketClient CreateWSClient()
        {
            var wsClient = new WebsocketClient(new Uri(BitfinexApi._publicWssConnectionUrl))
            {
                ReconnectTimeout = TimeSpan.FromSeconds(30)
            };
            wsClient.MessageReceived.Subscribe(HandleMessage);
            wsClient.ReconnectionHappened.Subscribe(OnReconnection);
            return wsClient;
        }

        public void SubscribeTrades(string symbol)
        {
            SendSubscribeTradesRequest(symbol);
        }

        public void UnsubscribeTrades(int channelId)
        {
            SendUnsubscribeRequest(channelId);
        }

        /// <param name="timeFrameInSeconds">Значение будет округлено вверх до ближайшего из доступных в api </param>
        public void SubscribeCandles(string symbol, int timeFrameInSeconds)
        {
            var key = BitfinexApi.GetAcceptedKey(symbol, timeFrameInSeconds);
            SendSubscribeCandlesRequest(key);
        }


        public void UnsubscribeCandles(int channelId)
        {
            SendUnsubscribeRequest(channelId);
        }

        private void SendSubscribeCandlesRequest(string key)
        {
            var jsonObject = new SubscribeCandlesObjectRequest()
            {
                eventName = BitfinexApi._subscribeEventName,
                channel = BitfinexApi._candlesChannelName,
                key = key
            };
            SendMessage(jsonObject);
        }

        private void SendSubscribeTradesRequest(string symbol)
        {
            var jsonObject = new SubscribeTradesObjectRequest()
            {
                eventName = BitfinexApi._subscribeEventName,
                channel = BitfinexApi._tradesChannelName,
                symbol = symbol
            };
            SendMessage(jsonObject);
        }

        private void SendUnsubscribeRequest(int id)
        {
            var jsonObject = new UnsubscribeObjectRequest()
            {
                eventName = BitfinexApi._unsubscribeEventName,
                chanId = id
            };
            SendMessage(jsonObject);
        }

        private void OnReconnection(ReconnectionInfo info)
        {
            Connected?.Invoke(BitfinexApi._maxPublicChannalConnectionsPerTime);
        }

        private void HandleMessage(ResponseMessage message)
        {
            if (message.MessageType != WebSocketMessageType.Text)
                return;

            var activeChannels = GetActiveChannelsConnetcions();

            var rootElement = JsonSerializer.Deserialize<JsonElement>(message.Text!)!;

            //В виде массивов здесь обычно данные
            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                var array = rootElement.EnumerateArray().ToArray();

                //Если это сообщение по подписке на канал
                var channelId = array[0].GetInt32();
                if (activeChannels.TryGetValue(channelId, out PairChannelOptions? channel))
                {
                    //Если это канал трейдов
                    if (channel is TradeChannelOptions tradesChannel)
                    {
                        //Если это строка
                        if (array[1].ValueKind == JsonValueKind.String)
                        {
                            //Если новый трейд
                            if (array[1].ToString() == BitfinexApi._tradeMessageTypeExecutedString)
                            {
                                NewTradeAction?.Invoke(array[2].CreatePairTrade(tradesChannel.Pair));
                            }
                        }
                        //Если это снапшот недавних трейдов
                        if (array[1].ValueKind == JsonValueKind.Array)
                        {
                            var snapshotArray = array[1]
                                .EnumerateArray()
                                .Select(t => t.CreatePairTrade(tradesChannel.Pair))
                                .OrderBy(t => t.Time);

                            foreach (var item in snapshotArray)
                            {
                                NewTradeAction?.Invoke(item);
                            }
                        }
                    }

                    //Если это канал свеч
                    if (channel is CandleChannelOptions candlesChannel)
                    {
                        //Если это массив (исключаем heartbeat)
                        if (array[1].ValueKind == JsonValueKind.Array && array[1].GetArrayLength() > 0)
                        {
                            //Если это новая свеча 
                            if (array[1][0].ValueKind == JsonValueKind.Number)
                            {
                                CandleProcessingAction?.Invoke(array[1].CreatePairCandle(candlesChannel.Pair));
                            }
                            //Если это снапшот
                            if (array[1][0].ValueKind == JsonValueKind.Array)
                            {
                                var snapshotArray = array[1]
                                    .EnumerateArray()
                                    .Select(t => t.CreatePairCandle(candlesChannel.Pair))
                                    .OrderBy(t => t.OpenTime);

                                foreach (var item in snapshotArray)
                                {
                                    CandleProcessingAction?.Invoke(item);
                                }
                            }
                        }
                    }
                }
            }

            //В виде объектов здесь обычно информационные сообщения и ответы
            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                var jsonObject = rootElement.EnumerateObject().ToArray();

                //Если это сообщение об успешной подписке на канал
                if (jsonObject.GetStringValueOf(BitfinexApi._eventPropertyNameString) == BitfinexApi._subscribedEventName)
                {
                    var channelName = jsonObject.GetStringValueOf(BitfinexApi._channelPropertyNameString);
                    if (channelName == BitfinexApi._tradesChannelName || channelName == BitfinexApi._candlesChannelName)
                    {
                        HandleSubscribedChannelJsonEvent(jsonObject);
                    }
                }
                //Если это сообщение об успешной отписке от канала
                if (jsonObject.GetStringValueOf(BitfinexApi._eventPropertyNameString) == BitfinexApi._unsubscribedEventName)
                {
                    HandleUnsubscribedChannelJsonEvent(jsonObject);
                }
            }

            var test = message.Text;
            return;
        }

        private void HandleUnsubscribedChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var chanid = jsonObject.GetIntValueOf(BitfinexApi._channelIdPropertyNameString);
            HandleUnsubscribedChannel?.Invoke(chanid);
        }

        private void HandleSubscribedChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var channel = jsonObject.GetStringValueOf(BitfinexApi._channelPropertyNameString);

            if (channel == BitfinexApi._tradesChannelName)
            {
                var id = jsonObject.GetIntValueOf(BitfinexApi._channelIdPropertyNameString);
                var symbol = jsonObject.GetStringValueOf(BitfinexApi._channelSymbolPropertyNameString);
                HandleSubscribedTradeChannel?.Invoke(symbol, id);
            }
            if (channel == BitfinexApi._candlesChannelName)
            {
                var id = jsonObject.GetIntValueOf(BitfinexApi._channelIdPropertyNameString);
                var key = jsonObject.GetStringValueOf(BitfinexApi._channelKeyPropertyNameString);
                var symbol = BitfinexApi.GetChannelSymbol(key);
                var acceptedTimeFrame = BitfinexApi.GetTimeframeFromCandleKey(key);
                HandleSubscribedCandleChannel?.Invoke(symbol, id, acceptedTimeFrame);
            }
        }

        private void SendMessage(object message)
        {
            var text = JsonSerializer.Serialize(message);
            _wsclient.Send(text);
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