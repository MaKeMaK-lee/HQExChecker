using HQTestLib.Entities;

namespace HQExChecker.GUI.EntityExtensions
{
    public static class TradeExtensions
    {
        public static bool EqualProps(this Trade left, Trade right)
        {
            return (left.Id == right.Id &&
                left.Side == right.Side &&
                left.Time == right.Time &&
                left.Amount == right.Amount &&
                left.Pair == right.Pair &&
                left.Price == right.Price);
        }
    }
}
