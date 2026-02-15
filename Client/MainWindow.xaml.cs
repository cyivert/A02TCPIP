using System.Windows;
using WordGameClient.Models;
using WordGameClient.ViewModels;

namespace WordGameClient
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ClientSettings? settings = null;
            string configError = string.Empty;
            bool loaded = false;

            InitializeComponent();

            loaded = ClientSettings.TryLoad(out settings, out configError);

            if ((loaded == true) && (settings != null))
            {
                this.DataContext = new MainWindowViewModel(settings, string.Empty);
            }
            else
            {
                this.DataContext = new MainWindowViewModel(null, configError);
            }

            return;
        }
    }
}
