using HQExChecker.GUI.Core;
using HQExChecker.GUI.EntityExtensions;
using HQTestLib.Connectors;
using HQTestLib.Entities;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace HQExChecker.GUI.MVVM_Main_Components.ViewModel
{
    public class TradesViewModel : Core.ViewModel
    {
        ITestConnector _connector;
        Dispatcher _currentDispatcher;

        ObservableCollection<Trade> trades;
        public IEnumerable<Trade> Trades => trades;

        private void AddOrReplaceTrade(Trade trade)
        {
            var existsTrade = trades.FirstOrDefault(t => t.Id == trade.Id);
            if (existsTrade != null)
            {
                if (existsTrade.EqualProps(trade))
                    return;
                trades.Remove(existsTrade);
            }
            trades.Add(trade);
        }

        public bool IsEnabledButton_GetNewTrades => !IsGetNewTradesActive;

        private bool isGetNewTradesActive;
        public bool IsGetNewTradesActive
        {
            get => isGetNewTradesActive;
            set
            {
                isGetNewTradesActive = value;
                OnPropertyChanged(nameof(IsGetNewTradesActive));
                OnPropertyChanged(nameof(IsEnabledButton_GetNewTrades));
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

        public int newTradesMaxCount;
        public int NewTradesMaxCount
        {
            get => newTradesMaxCount;
            set
            {
                newTradesMaxCount = value;
                OnPropertyChanged(nameof(NewTradesMaxCount));
            }
        }

        public RelayCommand GetNewTradesCommand { get; set; }
        public RelayCommand SubscribeTradesCommand { get; set; }
        public RelayCommand UnsubscribeTradesCommand { get; set; }

        private void SetCommands()
        {
            GetNewTradesCommand = new RelayCommand(o =>
            {
                GetNewTrades();
            }, o => true);
            SubscribeTradesCommand = new RelayCommand(o =>
            {
                SubscribeTrades();
            }, o => true);
            UnsubscribeTradesCommand = new RelayCommand(o =>
            {
                UnsubscribeTrades();
            }, o => true);
        }

        public TradesViewModel(ITestConnector testConnector, Dispatcher dispatcher)
        {
            _currentDispatcher = dispatcher;
            Pair = string.Empty;
            trades = [];

            _connector = testConnector;

            SetCommands();

        }

        private void SubscribeTrades()
        {
            _connector.SubscribeTrades(Pair, NewTradesMaxCount);
            _connector.NewSellTrade += NewTradeAction;
            _connector.NewBuyTrade += NewTradeAction;
        }

        private void UnsubscribeTrades()
        {
            _connector.UnsubscribeTrades(Pair);
            _connector.NewSellTrade -= NewTradeAction;
            _connector.NewBuyTrade -= NewTradeAction;
        }

        private void NewTradeAction(Trade trade)
        {
            _currentDispatcher.Invoke(() => AddOrReplaceTrade(trade));
        }

        private async void GetNewTrades()
        {
            IsGetNewTradesActive = true;

            var getRequestTask = _connector.GetNewTradesAsync(Pair, NewTradesMaxCount);

            var firstTask = await Task.WhenAny(getRequestTask, Task.Delay(TimeSpan.FromSeconds(5)));
            if (firstTask == getRequestTask)
            {
                foreach (var item in getRequestTask.Result)
                {
                    AddOrReplaceTrade(item);
                }
            }

            IsGetNewTradesActive = false;
        }
    }
}
