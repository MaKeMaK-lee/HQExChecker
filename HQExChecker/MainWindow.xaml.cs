using HQExChecker.GUI.Core;
using System.Windows;

namespace HQExChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(ViewModel mainDataContext)
        {
            InitializeComponent();
            MainView.DataContext = mainDataContext;
        }
    }
}