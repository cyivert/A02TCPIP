/*
* FILE            : App.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Application startup for the Word Game client.
*   Loads App.config settings and stops the app if configuration is invalid.
*/

using System.Windows;
using WordGameClient.Models;

namespace WordGameClient
{
    /*
    * CLASS           : App
    * DESCRIPTION     :
    *   WPF application entry point. Validates configuration on startup.
    */
    public partial class App : Application
    {
        public static ClientSettings? Settings { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isLoaded = ClientSettings.TryLoad(out ClientSettings? loadedSettings, out string errorMessage);

            if ((isLoaded == false) || (loadedSettings == null))
            {
                MessageBox.Show(
                    errorMessage,
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                this.Shutdown();
            }
            else
            {
                Settings = loadedSettings;
            }

            return;
        }
    }
}
