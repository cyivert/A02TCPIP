/*
* FILE            : App.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   WPF application startup. Loads settings from App.config.
*/

using System.Windows;
using WordGameClient.Models;

namespace WordGameClient
{
    public partial class App : Application
    {
        public static ClientSettings? Settings { get; private set; }
        public static string ConfigurationErrorMessage { get; private set; }

        public App()
        {
            Settings = null;
            ConfigurationErrorMessage = string.Empty;

            return;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool loaded = false;
            ClientSettings? settings = null;
            string errorMessage = string.Empty;

            base.OnStartup(e);

            loaded = ClientSettings.TryLoad(out settings, out errorMessage);

            if ((loaded == true) && (settings != null))
            {
                Settings = settings;
                ConfigurationErrorMessage = string.Empty;
            }
            else
            {
                Settings = null;
                ConfigurationErrorMessage = errorMessage;
            }

            return;
        }
    }
}
