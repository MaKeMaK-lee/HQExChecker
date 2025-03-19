using HQExChecker.GUI.Core;

namespace HQExChecker.GUI.MVVM_Main_Components.ViewModel
{
    public class MainViewModel : Core.ViewModel
    {
        private INavigation _navigation;
        public INavigation Navigation
        {
            get => _navigation;
            set
            {
                _navigation = value;
                OnPropertyChanged();
            }
        }
        public bool IsCheckedNavigateToTradesRadio => Navigation?.CurrentView?.GetType() == typeof(TradesViewModel);
        public bool IsCheckedNavigateToCandlesRadio => Navigation?.CurrentView?.GetType() == typeof(CandlesViewModel);

        public RelayCommand NavigateToTradesCommand { get; set; }
        public RelayCommand NavigateToCandlesCommand { get; set; }

        public MainViewModel(INavigation navigationService)
        {
            Navigation = navigationService;

            Navigation.AddNavigationChangedHandler((o, e) =>
            {
                OnPropertyChanged(nameof(IsCheckedNavigateToTradesRadio));

            });

            NavigateToTradesCommand = new RelayCommand(o =>
            {
                Navigation.NavigateTo<TradesViewModel>();
            }, o => true);
            NavigateToCandlesCommand = new RelayCommand(o =>
            {
                Navigation.NavigateTo<CandlesViewModel>();
            }, o => true);

            Navigation.NavigateTo<TradesViewModel>();
        }
    }
}
