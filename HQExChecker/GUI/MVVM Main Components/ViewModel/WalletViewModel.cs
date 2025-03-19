using HQExChecker.Entities;
using HQExChecker.GUI.Core;
using HQExChecker.GUI.Entities;
using HQExChecker.GUI.Extensions;
using HQExChecker.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace HQExChecker.GUI.MVVM_Main_Components.ViewModel
{
    public class WalletViewModel : Core.ViewModel
    {
        ICurrencyConverter _currencyConverter;
        Dispatcher _currentDispatcher;


        ObservableCollection<ConvertedWallet> convertedWallets;
        public IEnumerable<ConvertedWallet> ConvertedWallets => convertedWallets;

        public static string CurrencyBTC => "BTC";
        public static string CurrencyXRP => "XRP";
        public static string CurrencyXMR => "XMR";
        public static string CurrencyDASH => "DSH";

        private decimal amountBTC;
        public decimal AmountBTC
        {
            get => amountBTC;
            set
            {
                amountBTC = value;
                OnPropertyChanged(nameof(AmountBTC));
            }
        }

        private decimal amountXRP;
        public decimal AmountXRP
        {
            get => amountXRP;
            set
            {
                amountXRP = value;
                OnPropertyChanged(nameof(AmountXRP));
            }
        }

        private decimal amountXMR;
        public decimal AmountXMR
        {
            get => amountXMR;
            set
            {
                amountXMR = value;
                OnPropertyChanged(nameof(AmountXMR));
            }
        }

        private decimal amountDASH;
        public decimal AmountDASH
        {
            get => amountDASH;
            set
            {
                amountDASH = value;
                OnPropertyChanged(nameof(AmountDASH));
            }
        }

        public bool IsEnabledButton_ConvertWallet => !IsConvertWalletActive;

        private bool isConvertWalletActive;
        public bool IsConvertWalletActive
        {
            get => isConvertWalletActive;
            set
            {
                isConvertWalletActive = value;
                OnPropertyChanged(nameof(IsConvertWalletActive));
                OnPropertyChanged(nameof(IsEnabledButton_ConvertWallet));
            }
        }

        private string targetCurrency;
        public string TargetCurrency
        {
            get => targetCurrency;
            set
            {
                targetCurrency = value;
                OnPropertyChanged(nameof(TargetCurrency));
            }
        }

        public RelayCommand ConvertWalletCommand { get; set; }

        private void SetCommands()
        {
            ConvertWalletCommand = new RelayCommand(o =>
            {
                Task.Run(ConvertWalletAsync);
            }, o => true);
        }

        public WalletViewModel(ICurrencyConverter currencyConverter, Dispatcher dispatcher)
        {
            _currencyConverter = currencyConverter;
            _currentDispatcher = dispatcher;

            convertedWallets = [];

            IsConvertWalletActive = false;
            AmountBTC = 1;
            AmountXRP = 15000;
            AmountXMR = 50;
            AmountDASH = 30;

            SetCommands();

        }

        private Wallet GetWallet() => new()
        {
            BTC = AmountBTC,
            XRP = AmountXRP,
            XMR = AmountXMR,
            DSH = AmountDASH,
        };

        private Dictionary<string, decimal> GetWalletDictionary() =>
            new()
            {
                [CurrencyBTC] = AmountBTC,
                [CurrencyXRP] = AmountXRP,
                [CurrencyXMR] = AmountXMR,
                [CurrencyDASH] = AmountDASH,
            };

        private async Task ConvertWalletAsync()
        {
            IsConvertWalletActive = true;

            var convertRequestTask = _currencyConverter.ConvertWallet(GetWalletDictionary(), TargetCurrency);

            var firstTask = await Task.WhenAny(convertRequestTask, Task.Delay(TimeSpan.FromSeconds(10)));
            if (firstTask == convertRequestTask && firstTask.IsFaulted == false)
            {
                var wallet = new Wallet(convertRequestTask.Result.walletValuesInTargetCurrancy);
                var sum = convertRequestTask.Result.Sum;
                var targetCurrancy = convertRequestTask.Result.targetCurrancy;

                _currentDispatcher.Invoke(() =>
                {
                    convertedWallets.AddOrReplace(
                        new(wallet) { Sum = sum, TargetCurrency = targetCurrancy },
                        (i) => i.TargetCurrency == targetCurrancy);
                });
            }

            IsConvertWalletActive = false;
        }
    }
}
