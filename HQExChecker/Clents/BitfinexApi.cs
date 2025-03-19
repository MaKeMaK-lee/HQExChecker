using System.Text.RegularExpressions;

namespace HQExChecker.Clents
{
    public static class BitfinexApi
    {
        #region ApiDocs
        //Сведения получены от поставщика API https://docs.bitfinex.com/ для APIv2 в марте 2025 года.

        public static int _maxPublicChannalConnectionsPerMinute = 20;
        public static int _maxPublicChannalConnectionsPerTime = 25;
        public static string _publicWssConnectionUrl = "wss://api-pub.bitfinex.com/ws/2";
        public static string _getTickerUrl = "https://api-pub.bitfinex.com/v2/ticker";
        public static string _getTradesUrl = "https://api-pub.bitfinex.com/v2/trades";
        public static string _getCandelsUrl = "https://api-pub.bitfinex.com/v2/candles";

        public static string _subscribeEventName = "subscribe";
        public static string _unsubscribeEventName = "unsubscribe";
        public static string _subscribedEventName = "subscribed";
        public static string _unsubscribedEventName = "unsubscribed";

        public static string _tradesChannelName = "trades";
        public static string _candlesChannelName = "candles";

        public static string _getAcceptJsonHeaderNameString = "accept";
        public static string _getAcceptJsonHeaderValueString = "application/json";
        public static string _getSortPropertyNameString = "sort";
        public static string _getStartPropertyNameString = "start";
        public static string _getEndPropertyNameString = "end";
        public static string _getLimitPropertyNameString = "limit";
        public static string _getHistPropertyNameString = "hist";
        public static string _eventPropertyNameString = "event";
        public static string _channelPropertyNameString = "channel";
        public static string _channelIdPropertyNameString = "chanId";
        public static string _channelSymbolPropertyNameString = "symbol";
        public static string _channelKeyPropertyNameString = "key";

        public static string _tradeMessageTypeUpdateString = "tu";
        public static string _tradeMessageTypeExecutedString = "te";

        public static int _getTradesMaxLimit = 10000;
        //Дополнено/Отредактировано:
        public const string _candlesSubscriptionKeyTemplateString = "trade:{timeframe}:{symbol}";
        public static string _candlesSubscriptionKeyTemplateTimeframePropertyString = "{timeframe}";
        public static string _candlesSubscriptionKeyTemplateSymbolPropertyString = "{symbol}";
        public static readonly List<string> _candlesSubscriptionKeyTimeframeAcceptedValues = ["1m", "5m", "15m", "30m", "1h", "3h", "6h", "12h", "1D", "1W", "14D", "1M"];
        public static readonly List<int> _candlesSubscriptionKeyTimeframeAcceptedValuesSeconds = [60, 300, 900, 1800, 3600, 10800, 21600, 43200, 86400, 604800, 1209600, 2592000];

        #endregion

        public static string GetChannelSymbol(string key) => ExtractSymbol(key,
            _candlesSubscriptionKeyTemplateString,
            _candlesSubscriptionKeyTemplateSymbolPropertyString);

        public static string GetAcceptedKey(string symbol, int timeFrameInSeconds)
        {
            //Округляем вверх до ближайшего
            timeFrameInSeconds = GetAcceptedKeyTimeframe(timeFrameInSeconds);

            var index = _candlesSubscriptionKeyTimeframeAcceptedValuesSeconds.IndexOf(timeFrameInSeconds);
            var stringTimeFrame = _candlesSubscriptionKeyTimeframeAcceptedValues[index];

            var key = _candlesSubscriptionKeyTemplateString
                .Replace(_candlesSubscriptionKeyTemplateTimeframePropertyString, stringTimeFrame)
                .Replace(_candlesSubscriptionKeyTemplateSymbolPropertyString, symbol);
            return key;
        }

        public static int GetTimeframeFromCandleKey(string key)
        {
            var stringTimeframe = ExtractSymbol(key, _candlesSubscriptionKeyTemplateString, _candlesSubscriptionKeyTemplateTimeframePropertyString);
            var index = _candlesSubscriptionKeyTimeframeAcceptedValues.IndexOf(stringTimeframe);
            var timeframeInSeconds = _candlesSubscriptionKeyTimeframeAcceptedValuesSeconds[index];
            return timeframeInSeconds;
        }

        public static int GetAcceptedKeyTimeframe(int timeFrameInSeconds)
            => _candlesSubscriptionKeyTimeframeAcceptedValuesSeconds.FirstOrDefault(
                x => x >= timeFrameInSeconds,
                _candlesSubscriptionKeyTimeframeAcceptedValuesSeconds.Max());

        /// <summary>
        /// Gets its part from a string using a template (see ex. on params)
        /// </summary>
        /// <param name="source">ex. qweAAA555rty</param>
        /// <param name="template">ex. qwe{part1}rty</param>
        /// <param name="target">ex. {part1}</param>
        /// <returns>ex. "AAA555"</returns>
        /// <exception cref="ArgumentException"></exception>
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
