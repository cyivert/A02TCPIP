/*
* FILE            : ConnectionSettingsWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Dialog window for editing connection settings (Server IP, Port).
*   Tests the connection on save and reports success or failure.
*/

using System;
using System.Configuration;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;
using WordGameClient.Utils;
using System.IO;

namespace WordGameClient
{
    //
    // CLASS : ConnectionSettingsWindow
    // DESCRIPTION : Modal dialog that allows the user to view and modify the server IP and port settings.
    //               Changes are saved to the application configuration file (App.config) upon confirmation.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public partial class ConnectionSettingsWindow : Window
    {
        public string ServerIp { get; private set; }
        public int ServerPort { get; private set; }
        public bool SettingsChanged { get; private set; }
        public bool ConnectionSucceeded { get; private set; }

        //
        // FUNCTION : ConnectionSettingsWindow (constructor)
        // DESCRIPTION : Initializes the dialog, loads current values from App.config,
        //               and populates the text boxes.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public ConnectionSettingsWindow()
        {
            InitializeComponent();

            this.ServerIp = ConfigurationManager.AppSettings[ConfigKeys.ServerIp] ?? "127.0.0.1";
            this.ServerPort = 5000;
            this.SettingsChanged = false;
            this.ConnectionSucceeded = false;

            int parsedPort = 0;
            if (int.TryParse(ConfigurationManager.AppSettings[ConfigKeys.ServerPort], out parsedPort))
            {
                this.ServerPort = parsedPort;
            }

            this.ServerIpTextBox.Text = this.ServerIp;
            this.ServerPortTextBox.Text = this.ServerPort.ToString();

            return;
        }

        //
        // FUNCTION : SaveButton_Click
        // DESCRIPTION : Event handler for the Save button. Validates the input,
        //               saves the new settings to App.config, and closes the dialog.
        // PARAMETERS : object sender - the source of the event (the Save button);
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = this.ServerIpTextBox.Text.Trim();
            int port = 0;

            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("Server IP cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(this.ServerPortTextBox.Text.Trim(), out port) == false || port < 1 || port > 65535)
            {
                MessageBox.Show("Server Port must be between 1 and 65535.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save settings
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[ConfigKeys.ServerIp].Value = ip;
                config.AppSettings.Settings[ConfigKeys.ServerPort].Value = port.ToString();
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                this.ServerIp = ip;
                this.ServerPort = port;
                this.SettingsChanged = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.StatusTextBlock.Text = "Settings saved.";
            this.StatusTextBlock.Foreground = Brushes.Green;

            MessageBox.Show("Settings saved. Use 'Start Game' to connect to " + ip + ":" + port + ".", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();

            return;
        }

        //
        // FUNCTION : CancelButton_Click
        // DESCRIPTION : Event handler for the Cancel button. Closes the dialog without saving.
        // PARAMETERS : object sender - the source of the event (the Cancel button);
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();

            return;
        }
    }
}
