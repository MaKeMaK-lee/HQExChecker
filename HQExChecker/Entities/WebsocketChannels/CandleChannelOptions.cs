namespace HQExChecker.Entities.WebsocketChannels
{
    public class CandleChannelOptions : PairChannelOptions
    {
        public required int PeriodInSec { get; set; }
        public required DateTimeOffset? From { get; set; }
        public required DateTimeOffset? To { get; set; }
        public required long? MaxCount { get; set; }
        public int CurrentCount { get; set; } = 0;
        /// <summary>
        /// Accepted by client timeframe
        /// </summary>
        public int? AcceptedPeriodInSec { get; set; }
        public HashSet<DateTimeOffset> CurrentProcessedFromDateTimes { get; set; } = [];
    }
}
