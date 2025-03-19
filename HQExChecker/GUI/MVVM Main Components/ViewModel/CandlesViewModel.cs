using HQExChecker.GUI.Core;
using HQExChecker.GUI.EntityExtensions;
using HQTestLib.Connectors;
using HQTestLib.Entities;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace HQExChecker.GUI.MVVM_Main_Components.ViewModel
{
    public class CandlesViewModel : Core.ViewModel
    {
        ITestConnector _connector;
        Dispatcher _currentDispatcher;

        ObservableCollection<Candle> candles;
        public IEnumerable<Candle> Candles => candles;

        private void AddOrReplaceCandle(Candle candle)
        {
            var existsCandle = candles.FirstOrDefault(c => c.OpenTime == candle.OpenTime);
            if (existsCandle != null)
            {
                if (existsCandle.EqualProps(candle))
                    return;
                candles.Remove(existsCandle);
            }
            candles.Add(candle);
        }

        public bool IsEnabledButton_GetNewCandles => !IsGetNewCandlesActive;

        private bool isGetNewCandlesActive;
        public bool IsGetNewCandlesActive
        {
            get => isGetNewCandlesActive;
            set
            {
                isGetNewCandlesActive = value;
                OnPropertyChanged(nameof(IsGetNewCandlesActive));
                OnPropertyChanged(nameof(IsEnabledButton_GetNewCandles));
            }
        }

        public string pair;
        public string Pair
        {
            get => pair;
            set
            {
                pair = value;
                OnPropertyChanged(nameof(Pair));
            }
        }

        public int candlePeriod;
        public int CandlePeriod
        {
            get => candlePeriod;
            set
            {
                candlePeriod = value;
                OnPropertyChanged(nameof(CandlePeriod));
            }
        }

        public DateTimeOffset candleFrom;
        public DateTimeOffset CandleFrom
        {
            get => candleFrom;
            set
            {
                candleFrom = value;
                OnPropertyChanged(nameof(CandleFrom));
            }
        }

        public DateTimeOffset? candleTo;
        public DateTimeOffset? CandleTo
        {
            get
            {
                if (candleTo == default(DateTimeOffset))
                    return null;
                return candleTo;
            }
            set
            {
                candleTo = value;
                OnPropertyChanged(nameof(CandleTo));
            }
        }

        public int сandlesMaxCount;
        public int СandlesMaxCount
        {
            get => сandlesMaxCount;
            set
            {
                сandlesMaxCount = value;
                OnPropertyChanged(nameof(СandlesMaxCount));
            }
        }

        public RelayCommand GetCandlesCommand { get; set; }
        public RelayCommand SubscribeCandlesCommand { get; set; }
        public RelayCommand UnsubscribeCandlesCommand { get; set; }

        private void SetCommands()
        {
            GetCandlesCommand = new RelayCommand(o =>
            {
                GetNewCandles();
            }, o => true);
            SubscribeCandlesCommand = new RelayCommand(o =>
            {
                SubscribeCandles();
                _connector.CandleSeriesProcessing += CandleProcessingAction;
            }, o => true);
            UnsubscribeCandlesCommand = new RelayCommand(o =>
            {
                UnsubscribeCandles();
                _connector.CandleSeriesProcessing -= CandleProcessingAction;
            }, o => true);
        }

        public CandlesViewModel(ITestConnector testConnector, Dispatcher dispatcher)
        {
            _currentDispatcher = dispatcher;
            candles = [];

            Pair = string.Empty;
            CandlePeriod = 60;
            CandleFrom = DateTimeOffset.Now - TimeSpan.FromDays(1);
            CandleTo = DateTimeOffset.Now;

            _connector = testConnector;

            SetCommands();

        }

        private void SubscribeCandles()
        {
            _connector.SubscribeCandles(Pair, CandlePeriod, CandleFrom, CandleTo, СandlesMaxCount);
            _connector.CandleSeriesProcessing += CandleProcessingAction;
        }

        private void UnsubscribeCandles()
        {
            _connector.UnsubscribeCandles(Pair);
            _connector.CandleSeriesProcessing -= CandleProcessingAction;
        }

        private void CandleProcessingAction(Candle candle)
        {
            _currentDispatcher.Invoke(() => AddOrReplaceCandle(candle));
        }

        private async void GetNewCandles()
        {
            IsGetNewCandlesActive = true;

            var getRequestTask = _connector.GetCandleSeriesAsync(Pair, CandlePeriod, CandleFrom, CandleTo, СandlesMaxCount);

            var firstTask = await Task.WhenAny(getRequestTask, Task.Delay(TimeSpan.FromSeconds(5)));
            if (firstTask == getRequestTask)
            {
                foreach (var item in getRequestTask.Result)
                {
                    AddOrReplaceCandle(item);
                }
            }

            IsGetNewCandlesActive = false;
        }
    }

}
