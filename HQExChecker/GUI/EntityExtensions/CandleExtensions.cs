using HQTestLib.Entities;

namespace HQExChecker.GUI.EntityExtensions
{
    public static class CandleExtensions
    {
        public static bool EqualProps(this Candle left, Candle right)
        {
            return (left.Pair == right.Pair &&
                left.OpenPrice == right.OpenPrice &&
                left.LowPrice == right.LowPrice &&
                left.HighPrice == right.HighPrice &&
                left.ClosePrice == right.ClosePrice &&
                left.TotalPrice == right.TotalPrice &&
                left.TotalVolume == right.TotalVolume);
        }
    }
}
