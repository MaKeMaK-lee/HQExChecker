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
        //public bool IsCheckedNavigateToHomeRadio => Navigation?.CurrentView?.GetType() == typeof(HomeViewModel) ? true : false;

        public RelayCommand NavigateToHomeCommand { get; set; }

        public MainViewModel(INavigation navigationService)
        {
            Navigation = navigationService;

            Navigation.AddNavigationChangedHandler((o, e) =>
            {
                //OnPropertyChanged(nameof(IsCheckedNavigateToHomeRadio));

            });

            //Navigation.NavigateTo<HomeViewModel>();
        }
    }
}
