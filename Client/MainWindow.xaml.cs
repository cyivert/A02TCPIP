/*
* FILE            : MainWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Main window code-behind. Wires MVVM DataContext.
*/

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WordGameClient.ViewModels;

namespace WordGameClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel(App.Settings, App.ConfigurationErrorMessage);

            return;
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            return;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MainWindowViewModel? viewModel = this.DataContext as MainWindowViewModel;

            if (viewModel != null && viewModel.ConnectionStatus == "CONNECTED")
            {
                MessageBoxResult result = MessageBox.Show(
                    "You are still connected to the server.\nPlease disconnect first using File > Disconnect before exiting.\n\nDisconnect now and exit?",
                    "Still Connected",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // User chose yes - disconnect and exit
                viewModel.DisconnectCommand.Execute(null);
            }

            return;
        }

        private void ConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConnectionSettingsWindow settingsWindow = new ConnectionSettingsWindow();
            settingsWindow.Owner = this;
            bool? result = settingsWindow.ShowDialog();

            if (result == true && settingsWindow.SettingsChanged)
            {
                MainWindowViewModel? viewModel = this.DataContext as MainWindowViewModel;

                if (viewModel != null)
                {
                    viewModel.ServerEndpointLabel = "Server: " + settingsWindow.ServerIp + ":" + settingsWindow.ServerPort;
                    viewModel.LogMessages.Add("Settings saved: " + settingsWindow.ServerIp + ":" + settingsWindow.ServerPort);
                }
            }

            return;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();

            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();

            return;
        }

        private void GuessWordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MainWindowViewModel? viewModel = this.DataContext as MainWindowViewModel;

                if ((viewModel != null) && (viewModel.SubmitGuessCommand.CanExecute(null) == true))
                {
                    viewModel.SubmitGuessCommand.Execute(null);
                }
            }

            return;
        }
    }
}
