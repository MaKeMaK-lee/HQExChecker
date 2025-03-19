using HQExChecker.Entities;

namespace HQExChecker.GUI.Entities
{
    public class ConvertedWallet : Wallet
    {
        public required string TargetCurrency { get; set; }
        public required decimal Sum { get; set; }

        public ConvertedWallet(Wallet wallet)
        {
            var type = GetType();
            foreach (var item in wallet.GetType().GetProperties())
            {
                type.GetProperty(item.Name)?.SetValue(this, item.GetValue(wallet));
            }
        }

        //public ConvertedWallet(IDictionary<string, decimal> walletDictionary)
        //{
        //    var type = GetType();
        //    foreach (var item in walletDictionary)
        //    {
        //        type.GetProperty(item.Key)?.SetValue(this, item.Value);
        //    }
        //}
    }
}
