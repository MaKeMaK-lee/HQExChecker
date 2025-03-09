namespace HQExChecker.Clents.Utilities
{
    public class LimitedChannelsDictionary(int maxCount)
    {
        private readonly Dictionary<int, (string name, string symbol, int maxCount)> channels = [];

        public IReadOnlyDictionary<int, (string name, string symbol, int maxCount)> Channels => channels.AsReadOnly();

        public int MaxCount { get; } = maxCount;

        public void Add(int id, string name, string symbol, int maxCount)
        {
            if (channels.Count < MaxCount)
                channels.Add(id, (name, symbol, maxCount));
            else
                throw new InvalidOperationException($"The collection cannot accept more items. Maximum count: {MaxCount}.");
        }

        public void Remove(int id, string name)
        {
            channels.Remove(id);
        }
    }
}
