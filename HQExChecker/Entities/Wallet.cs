namespace HQExChecker.Entities
{
    /// <summary>
    /// Представляет набор валют с указанием их количества. Наименования свойств (валют) соответствуют обозначениям этих валют в API биржы Bitfinex.
    /// </summary>
    public class Wallet
    {
        public decimal BTC { get; set; }
        public decimal XRP { get; set; }
        public decimal XMR { get; set; }
        public decimal DSH { get; set; }

        //public IDictionary<string, decimal> ToDictionary()
        //=> GetType().GetProperties().ToDictionary(x => x.Name,
        //        x => (decimal)x.GetValue(this)!);

        public Wallet()
        {

        }

        public Wallet(IDictionary<string, decimal> dictionary)
        {
            var type = GetType();
            foreach (var item in dictionary)
            {
                type.GetProperty(item.Key)?.SetValue(this, item.Value);
            }
        }
    }
}
