namespace HQExChecker.Clents.Utilities
{
    public class LimitedChannelsDictionary(int maxCount)
    {
        private readonly Dictionary<int, (string channel, string symbol, int maxCount)> channels = [];

        /// <summary>
        /// int id, (channel - "trades" etc, symbol - "tBTCUSD" etc, maxCount - max count on snapshot)
        /// </summary>
        public IReadOnlyDictionary<int, (string channel, string symbol, int maxCount)> Channels => channels.AsReadOnly();

        public int MaxCount { get; } = maxCount;

        public void Add(int id, string channel, string symbol, int maxCount)
        {
            if (channels.Count < MaxCount)
                channels.Add(id, (channel, symbol, maxCount));
            else
                throw new InvalidOperationException($"The collection cannot accept more items. Maximum count: {MaxCount}.");
        }

        public void Remove(int id)
        {
            channels.Remove(id);
        }

        public void Clear()
        {
            channels.Clear();
        }
    }
}
