using HQExChecker.GUI.Core;
using HQExChecker.GUI.Extensions;
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

        private string pair;
        public string Pair
        {
            get => pair;
            set
            {
                pair = value;
                OnPropertyChanged(nameof(Pair));
            }
        }

        private int newTradesMaxCount;
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
                Task.Run(GetNewTradesAsync);
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
            _currentDispatcher.Invoke(() => trades.AddOrReplace(trade, t => t.Id == trade.Id));
        }

        private async Task GetNewTradesAsync()
        {
            IsGetNewTradesActive = true;

            var getRequestTask = _connector.GetNewTradesAsync(Pair, NewTradesMaxCount);

            var firstTask = await Task.WhenAny(getRequestTask, Task.Delay(TimeSpan.FromSeconds(5)));
            if (firstTask == getRequestTask && firstTask.IsFaulted == false)
            {
                foreach (var item in getRequestTask.Result)
                {
                    _currentDispatcher.Invoke(() => trades.AddOrReplace(item, t => t.Id == item.Id));
                }
            }

            IsGetNewTradesActive = false;
        }
    }
}
