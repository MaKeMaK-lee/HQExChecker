using HQExChecker.Clents;
using HQExChecker.Connectors;
using HQExChecker.GUI.Core;
using HQExChecker.GUI.MVVM_Main_Components.ViewModel;
using HQExChecker.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HQExChecker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<IBitfinexRestClient, BitfinexRestClient>();
            services.AddSingleton<IBitfinexWebsocketClient, BitfinexWebsocketClient>();
            services.AddSingleton<IBitfinexConnector, BitfinexConnector>();

            services.AddSingleton<ICurrencyConverter, CurrencyConverter>();

            services.AddSingleton<MainWindow>(provider => new MainWindow(provider.GetRequiredService<MainViewModel>()));
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<TradesViewModel>(provider => new TradesViewModel(
                provider.GetRequiredService<IBitfinexConnector>(),
                Dispatcher));
            services.AddSingleton<CandlesViewModel>(provider => new CandlesViewModel(
                provider.GetRequiredService<IBitfinexConnector>(),
                Dispatcher));
            services.AddSingleton<WalletViewModel>(provider => new WalletViewModel(
                provider.GetRequiredService<ICurrencyConverter>(),
                Dispatcher));

            services.AddSingleton<Func<Type, ViewModel>>
                (serviceProvider => viewModelType => (ViewModel)serviceProvider.GetRequiredService(viewModelType));
            services.AddSingleton<INavigation, Navigation>();


            _serviceProvider = services.BuildServiceProvider();




            Startup += Application_Startup;
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }
    }

}
