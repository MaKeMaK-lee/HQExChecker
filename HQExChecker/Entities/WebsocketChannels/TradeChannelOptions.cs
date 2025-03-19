namespace HQExChecker.Entities.WebsocketChannels
{
    public class TradeChannelOptions : PairChannelOptions
    {
        public required int MaxCount { get; set; }
        public int CurrentCount { get; set; } = 0;
    }
}
