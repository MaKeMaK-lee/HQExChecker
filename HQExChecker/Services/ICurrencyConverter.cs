namespace HQExChecker.Services
{
    public interface ICurrencyConverter
    {
        /// <param name="wallet">key: currency, value: amount of currency</param>
        /// <param name="targetCurrancy">currency to convert to</param> 
        public Task<(IDictionary<string, decimal> walletValuesInTargetCurrancy, decimal Sum, string targetCurrancy)> ConvertWallet(IDictionary<string, decimal> wallet, string targetCurrancy);


    }
}
