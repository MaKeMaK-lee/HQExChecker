using Flurl.Http;
using HQExChecker.Connectors;

namespace HQExChecker.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        IBitfinexConnector _connector;

        public CurrencyConverter(IBitfinexConnector connector)
        {
            _connector = connector;
        }

        public async Task<(IDictionary<string, decimal> walletValuesInTargetCurrancy, decimal Sum, string targetCurrancy)> ConvertWallet(
            IDictionary<string, decimal> wallet, string targetCurrancy)
        {
            Dictionary<string, decimal> walletValuesInTargetCurrancy = [];
            decimal sum = 0;

            foreach (var currencyItem in wallet)
            {
                bool tryingInverted = false;
                bool invertedFaults = false;
                do
                {
                    try
                    {
                        if (invertedFaults && !(currencyItem.Key == "USD" || targetCurrancy == "USD"))
                        {
                            var tickerConvToUSD = await _connector.GetTicker(currencyItem.Key, "USD");
                            var tickerTargetToUSD = await _connector.GetTicker(targetCurrancy, "USD");
                            var convToTarget = tickerConvToUSD.LastPrice / tickerTargetToUSD.LastPrice;

                            var value = currencyItem.Value * convToTarget;

                            walletValuesInTargetCurrancy[currencyItem.Key] = value;
                            sum += value;
                            break;
                        }

                        if (currencyItem.Key == targetCurrancy)
                        {
                            walletValuesInTargetCurrancy[currencyItem.Key] = currencyItem.Value;
                            sum += currencyItem.Value;
                            break;
                        }
                        var ticker = await _connector.GetTicker(
                            tryingInverted ? targetCurrancy : currencyItem.Key,
                            tryingInverted ? currencyItem.Key : targetCurrancy);

                        var valueInTargetCurrancy = currencyItem.Value * (tryingInverted ? 1 / ticker.LastPrice : ticker.LastPrice);

                        walletValuesInTargetCurrancy[currencyItem.Key] = valueInTargetCurrancy;
                        sum += valueInTargetCurrancy;
                        break;
                    }
                    //Вообще данные эксепшены нужно бы ловить в коннекторе, но тогда пришлось бы передавать обратно сюда и соответствующую информацию.
                    //И, так как до нормальной их реализации дело не дошло (см.ниже), обработка происходит здесь.
                    //
                    //Данную проблему следовало бы решать обращением к конфигу апи и универсальным определением маршрута конвертаций,
                    // ну или я изначально выбрал хреновый метод рассчёта через тикер (вполне вероятно).
                    //Так или иначе, уже прошло слишком много времени, я просто выполню это через инверсию пары или конвертацию через USD.
                    catch (FlurlHttpException ex)
                    {
                        if (ex.StatusCode == 500)
                        {
                            if (invertedFaults)
                                throw new Exception("Request faulted.", innerException: ex);
                            if (tryingInverted)
                            {
                                invertedFaults = true;
                                continue;
                            }
                            tryingInverted = true;
                            continue;
                        }
                    }
                } while (true);
            }
            return (walletValuesInTargetCurrancy, sum, targetCurrancy);
        }
    }
}
