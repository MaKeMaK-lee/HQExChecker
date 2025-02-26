namespace HQExChecker.GUI.Core
{
    public interface INavigation
    {
        ViewModel CurrentView { get; }

        void NavigateTo<T>() where T : ViewModel;

        public void AddNavigationChangedHandler(EventHandler handler);
    }
}
