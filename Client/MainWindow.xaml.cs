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
    //
    // CLASS : MainWindow
    // DESCRIPTION : The main application window. Sets up the ViewModel as DataContext and handles
    //               menu events (Exit, Connection Settings, About) and the Enter key for guess submission.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public partial class MainWindow : Window
    {
        //
        // FUNCTION : MainWindow (constructor)
        // DESCRIPTION : Initializes the main window, loads the XAML, and sets the DataContext to a new MainWindowViewModel.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel(App.Settings, App.ConfigurationErrorMessage);

            return;
        }

        //
        // FUNCTION : ExitMenuItem_Click
        // DESCRIPTION : Event handler for File > Exit menu. Closes the main window.
        // PARAMETERS : object sender - the source of the event;
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            return;
        }

        //
        // FUNCTION : Window_Closing
        // DESCRIPTION : Handles the Window.Closing event. If still connected to the server,
        //               prompts the user to disconnect first. Gives option to disconnect and exit.
        // PARAMETERS : object sender - the source of the event;
        //              CancelEventArgs e - allows canceling the close.
        // RETURNS : void
        //
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

        //
        // FUNCTION : ConnectionMenuItem_Click
        // DESCRIPTION : Opens the ConnectionSettingsWindow modal dialog. If settings are saved,
        //               updates the ViewModel's server endpoint label and adds a log message.
        // PARAMETERS : object sender - the source of the event;
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
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

        //
        // FUNCTION : AboutMenuItem_Click
        // DESCRIPTION : Opens the AboutWindow modal dialog.
        // PARAMETERS : object sender - the source of the event;
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();

            return;
        }

        //
        // FUNCTION : GuessWordTextBox_KeyDown
        // DESCRIPTION : Handles the KeyDown event on the guess word text box.
        //               If the Enter key is pressed and the SubmitGuess command can execute,
        //               executes the command.
        // PARAMETERS : object sender - the source of the event (the text box);
        //              KeyEventArgs e - key event data.
        // RETURNS : void
        //
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
