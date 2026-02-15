/*
* FILE            : MainWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Main window code-behind. Wires MVVM DataContext.
*/

using System.Configuration;
using System.Windows;
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
            Application.Current.Shutdown();

            return;
        }

        private void UserSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string playerName = string.Empty;

            MainWindowViewModel? viewModel = this.DataContext as MainWindowViewModel;

            if (viewModel != null)
            {
                playerName = viewModel.PlayerName;
            }

            string message = "Player Name: " + (string.IsNullOrWhiteSpace(playerName) ? "(not set)" : playerName);

            MessageBox.Show(message, "User Settings", MessageBoxButton.OK, MessageBoxImage.Information);

            return;
        }

        private void ConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = ConfigurationManager.AppSettings["serverIp"] ?? "(not set)";
            string serverPort = ConfigurationManager.AppSettings["serverPort"] ?? "(not set)";
            string connectTimeout = ConfigurationManager.AppSettings["connectTimeoutMs"] ?? "(not set)";
            string ioTimeout = ConfigurationManager.AppSettings["ioTimeoutMs"] ?? "(not set)";

            string message = "Server IP: " + serverIp + "\n"
                           + "Server Port: " + serverPort + "\n"
                           + "Connect Timeout (ms): " + connectTimeout + "\n"
                           + "I/O Timeout (ms): " + ioTimeout;

            MessageBox.Show(message, "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Information);

            return;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string message = "Word Game Client\n"
                           + "Version 1.0\n\n"
                           + "A02TCPIP Project\n"
                           + "Programmer: Cy, Thanh, Ritik";

            MessageBox.Show(message, "About Word Game", MessageBoxButton.OK, MessageBoxImage.Information);

            return;
        }
    }
}
