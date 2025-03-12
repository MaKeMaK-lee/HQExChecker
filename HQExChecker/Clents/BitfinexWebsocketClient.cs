using HQExChecker.Clents.Utilities;
using HQExChecker.Clents.Utilities.Entities;
using HQTestLib.Entities;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        const string _candlesChannelName = "candles";

        const string _eventPropertyNameString = "event";
        const string _channelPropertyNameString = "channel";
        const string _channelIdPropertyNameString = "chanId";
        const string _channelSymbolPropertyNameString = "symbol";
        const string _channelKeyPropertyNameString = "key";

        const string _candlesSubscriptionKeyTemplateString = "trade:{timeframe}{timeframeMultiplier}:{symbol}";
        const string _candlesSubscriptionKeyTemplateTimeframePropertyString = "{timeframe}";
        const string _candlesSubscriptionKeyTemplateTimeframeMultiplierPropertyString = "{timeframeMultiplier}";
        const string _candlesSubscriptionKeyTemplateSymbolPropertyString = "{symbol}";
        const string _candlesSubscriptionKeyTimeframeMinuteString = "m";

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

        /// <summary>
        /// Подключенные каналы 
        /// </summary>
        private readonly LimitedChannelsDictionary _channelsCollection;

        /// <summary>
        /// Запросы на подключение к каналам трейдов (key: channelSymbol, value: maxCount)
        /// </summary>
        private readonly Dictionary<string, int> _tradeChannelsSubRequests;

        /// <summary>
        /// Запросы на отключение от каналов трейдов (string symbol)
        /// </summary>
        private readonly List<string> _tradeChannelsUnsubRequests;

        /// <summary>
        /// Запросы на подключение к каналам свеч (key: channelSymbol)
        /// </summary>
        private readonly Dictionary<string, int> _candleChannelsSubRequests;

        /// <summary>
        /// Запросы на отключение от каналов свеч (string symbol)
        /// </summary>
        private readonly List<string> _candleChannelsUnsubRequests;

        private Action<Trade> newTradeAction;

        private Action<Candle> newCandleAction;

        public BitfinexWebsocketClient(Action<Trade> newTradeAction, Action<Candle> newCandleAction)
        {
            this.newTradeAction = newTradeAction;
            this.newCandleAction = newCandleAction;

            resubscribingTask = new Task(ResubscribingTaskActionAsync);

            _tradeChannelsSubRequests = [];
            _tradeChannelsUnsubRequests = [];
            _candleChannelsSubRequests = [];
            _candleChannelsUnsubRequests = [];
            _channelsCollection = new(_maxPublicChannalConnectionsPerTime);


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

            var channel = _channelsCollection.Channels
                .Where(ch => ch.Value.channel == _tradesChannelName)
                .FirstOrDefault(t => t.Value.symbol == symbol);

            if (channel.Key != 0)
            {
                _tradeChannelsUnsubRequests.Add(symbol);
                SendUnsubscribeRequest(channel.Key);
            }
        }

        /// <param name="minutes">
        /// Minutes only from this list:
        /// 1m: one minute,
        /// 5m : five minutes,
        /// 15m : 15 minutes,
        /// 30m : 30 minutes,
        /// 1h : one hour,
        /// 3h : 3 hours,
        /// 6h : 6 hours,
        /// 12h : 12 hours,
        /// 1D : one day,
        /// 1W : one week,
        /// 14D : two weeks,
        /// 1M : one month,
        /// </param>
        public void SubscribeCandles(string symbol, int minutes)
        {
            _candleChannelsSubRequests.Add(symbol, minutes);
            SendSubscribeCandlesRequest(symbol, minutes);
        }

        public void UnsubscribeCandles(string symbol)
        {
            _candleChannelsSubRequests.Remove(symbol);

            var channel = _channelsCollection.Channels
                .Where(ch => ch.Value.channel == _candlesChannelName)
               .FirstOrDefault(t => t.Value.symbol == symbol);

            if (channel.Key != 0)
            {
                _candleChannelsUnsubRequests.Add(symbol);
                SendUnsubscribeRequest(channel.Key);
            }
        }

        private void SendSubscribeCandlesRequest(string symbol, int minutes)
        {
            var jsonObject = new SubscribeCandlesObjectRequest()
            {
                eventName = _subscribeEventName,
                channel = _candlesChannelName,
                key = _candlesSubscriptionKeyTemplateString
                .Replace(_candlesSubscriptionKeyTemplateTimeframePropertyString, minutes.ToString())
                .Replace(_candlesSubscriptionKeyTemplateTimeframeMultiplierPropertyString, _candlesSubscriptionKeyTimeframeMinuteString)
                .Replace(_candlesSubscriptionKeyTemplateSymbolPropertyString, symbol)
            };
            SendMessage(jsonObject);
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
            IEnumerable<(string symbol, int maxCount)> tradeChannels = _channelsCollection.Channels
                .Where(ch => ch.Value.channel == _tradesChannelName)
                .Select(channel => (channel.Value.symbol, channel.Value.maxCount))
                .Concat(_tradeChannelsSubRequests
                .Select(request => (request.Key, request.Value)))
                .Where(ch => !_tradeChannelsUnsubRequests.Any(unsR => unsR == ch.Item1)).ToList();
            _tradeChannelsUnsubRequests.Clear();
            _tradeChannelsSubRequests.Clear();

            IEnumerable<(string symbol, int maxCount)> candlesChannels = _channelsCollection.Channels
                .Where(ch => ch.Value.channel == _candlesChannelName)
                .Select(channel => (channel.Value.symbol, channel.Value.maxCount))
                .Concat(_candleChannelsSubRequests
                .Select(request => (request.Key, request.Value)))
                .Where(ch => !_candleChannelsUnsubRequests.Any(unsR => unsR == ch.Item1)).ToList();
            _candleChannelsUnsubRequests.Clear();
            _candleChannelsSubRequests.Clear();

            _channelsCollection.Clear();

            resubscribeAsyncCancellationTokenSource = new CancellationTokenSource();
            await ResubscribeAsync(tradeChannels, candlesChannels, resubscribeAsyncCancellationTokenSource.Token);

            resubscribingTask = new Task(ResubscribingTaskActionAsync);
        }

        /// <summary>
        /// Внимание! Не тестировалось при количестве каналов большем, чем минутное ограничение у api.
        /// </summary>
        private async Task ResubscribeAsync(
            IEnumerable<(string symbol, int maxCount)> tradeChannels,
            IEnumerable<(string symbol, int maxCount)> candlesChannels,
            CancellationToken token)
        {
            IEnumerable<((string symbol, int maxCount) channel, int channelType)> channels =
                tradeChannels.Select(ch => (ch, 1))
                .Concat(candlesChannels.Select(ch => (ch, 2)));

            var chunks = channels.Chunk(_maxPublicChannalConnectionsPerMinute);
            foreach (var requestsChunk in chunks)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                foreach (var request in requestsChunk)
                {
                    if (request.channelType == 1)
                    {
                        SubscribeTrades(request.channel.symbol, request.channel.maxCount);
                    }
                    if (request.channelType == 2)
                    {
                        SubscribeCandles(request.channel.symbol, request.channel.maxCount);
                    }
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

                //Если это сообщение по подписке на канал
                var channelId = array[0].GetInt32();
                if (_channelsCollection.Channels.ContainsKey(channelId))
                {
                    var channelSymbol = _channelsCollection.Channels[channelId].symbol;

                    //Если это канал трейдов
                    if (_channelsCollection.Channels[channelId].channel == _tradesChannelName)
                    {
                        var channelMaxCount = _channelsCollection.Channels[channelId].maxCount;

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

                    //Если это канал свеч
                    if (_channelsCollection.Channels[channelId].channel == _candlesChannelName)
                    {
                        //Если это массив (исключаем heartbeat)
                        if (array[1].ValueKind == JsonValueKind.Array && array[1].GetArrayLength() > 0)
                        {
                            //Если это новая свеча 
                            if (array[1][0].ValueKind == JsonValueKind.Number)
                            {
                                newCandleAction(array[1].CreatePairCandle(channelSymbol));
                            }
                            //Если это снапшот 
                            if (array[1][0].ValueKind == JsonValueKind.Array)
                            {
                                var snapshotArray = array[1]
                                    .EnumerateArray()
                                    .Select(t => t.CreatePairCandle(channelSymbol));
                                foreach (var item in snapshotArray)
                                {
                                    newCandleAction(item);
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
                if (jsonObject.GetStringValueOf(_eventPropertyNameString) == _subscribedEventName)
                {
                    var channelName = jsonObject.GetStringValueOf(_channelPropertyNameString);
                    if (channelName == _tradesChannelName || channelName == _candlesChannelName)
                    {
                        HandleSubscribedChannelJsonEvent(jsonObject);
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
            if (_channelsCollection.Channels.ContainsKey(chanid))
            {
                var channel = _channelsCollection.Channels[chanid];
                _tradeChannelsSubRequests.Remove(channel.symbol);
            }
            _channelsCollection.Remove(chanid);
        }

        private void HandleSubscribedChannelJsonEvent(IEnumerable<JsonProperty> jsonObject)
        {
            var channel = jsonObject.GetStringValueOf(_channelPropertyNameString);

            int m = 0;//maxCount / minutes
            if (channel == _tradesChannelName)
            {
                var symbol = jsonObject.GetStringValueOf(_channelSymbolPropertyNameString);
                if (!_tradeChannelsSubRequests.TryGetValue(symbol, out int value))
                    return;
                m = value;

                _channelsCollection.Add(
                    jsonObject.GetIntValueOf(_channelIdPropertyNameString),
                    channel,
                    symbol,
                    m);
                _tradeChannelsSubRequests.Remove(symbol);
            }
            if (channel == _candlesChannelName)
            {
                var key = jsonObject.GetStringValueOf(_channelKeyPropertyNameString);
                var symbol = GetChannelSymbol(key);
                if (!_candleChannelsSubRequests.TryGetValue(symbol, out int value))
                    return;

                m = value;
                _channelsCollection.Add(
                    jsonObject.GetIntValueOf(_channelIdPropertyNameString),
                    channel,
                    symbol,
                    m);
                _candleChannelsSubRequests.Remove(symbol);
            }
        }

        private string GetChannelSymbol(string key) => ExtractSymbol(key,
            _candlesSubscriptionKeyTemplateString,
            _candlesSubscriptionKeyTemplateSymbolPropertyString);

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

        public static string ExtractSymbol(string source, string template, string target)
        {
            // Извлекаем имя целевой группы (например, "symbol" из "{symbol}")
            string groupName = target.Trim('{', '}');

            // Экранируем фигурные скобки в шаблоне для корректного разбиения
            string pattern = @"(\{[\w]+\})";
            string[] parts = Regex.Split(template, pattern);

            // Собираем regex-паттерн
            string regexPattern = "^";
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                // Если часть — плейсхолдер (например, "{timeframe}")
                if (Regex.IsMatch(part, @"^\{\w+\}$"))
                {
                    string placeholder = part.Trim('{', '}');
                    // Если это целевой плейсхолдер — создаём именованную группу
                    regexPattern += (placeholder == groupName)
                        ? $"(?<{groupName}>.*?)"
                        : ".*?";
                }
                else // Если статичный текст — экранируем
                {
                    regexPattern += Regex.Escape(part);
                }
            }
            regexPattern += "$";

            // Проверяем совпадение и извлекаем значение
            Match match = Regex.Match(source, regexPattern);
            if (!match.Success)
                throw new ArgumentException("Строка не соответствует шаблону.");

            return match.Groups[groupName].Value;
        }
    }
}
