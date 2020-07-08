using MufflonBrowser.Model;
using MufflonBrowser.ViewModel;
using System.Windows;

namespace MufflonBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Models models;
        private ViewModels viewModels;
        public MainWindow()
        {
            InitializeComponent();

            models = new Models();
            viewModels = new ViewModels(models);
            DataContext = viewModels;
        }
    }
}
