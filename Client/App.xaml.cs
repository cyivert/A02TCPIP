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
    //
    // CLASS : App
    // DESCRIPTION : WPF application class that handles startup and holds global application settings.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public partial class App : Application
    {
        public static ClientSettings? Settings { get; private set; }
        public static string ConfigurationErrorMessage { get; private set; }

        //
        // FUNCTION : App (constructor)
        // DESCRIPTION : Initializes the application by setting default values for static properties.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public App()
        {
            Settings = null;
            ConfigurationErrorMessage = string.Empty;

            return;
        }

        //
        // FUNCTION : OnStartup
        // DESCRIPTION : Overrides the application startup method. Attempts to load client settings from
        //               App.config via ClientSettings.TryLoad(). Stores the result in static properties
        //               for use throughout the application.
        // PARAMETERS : StartupEventArgs e - provides arguments passed on the command line (not used).
        // RETURNS : void
        //
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
