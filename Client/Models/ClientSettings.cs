/*
* FILE            : ClientSettings.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-12
* DESCRIPTION     :
*   Loads and validates client configuration from App.config.
*/

using System;
using System.Configuration;
using WordGameClient.Utils;

namespace WordGameClient.Models
{
    public sealed class ClientSettings
    {
        public string ServerIp { get; }
        public int ServerPort { get; }

        private ClientSettings(string serverIp, int serverPort)
        {
            this.ServerIp = serverIp;
            this.ServerPort = serverPort;

            return;
        }

        public static bool TryLoad(out ClientSettings? settings, out string errorMessage)
        {
            bool isSuccess = false;

            settings = null;
            errorMessage = string.Empty;

            string? serverIpValue = ConfigurationManager.AppSettings[ConfigKeys.ServerIp];
            string? serverPortValue = ConfigurationManager.AppSettings[ConfigKeys.ServerPort];

            int parsedPort = 0;

            if (string.IsNullOrWhiteSpace(serverIpValue) == true)
            {
                errorMessage = $"Missing App.config appSetting: '{ConfigKeys.ServerIp}'.";
            }
            else if (string.IsNullOrWhiteSpace(serverPortValue) == true)
            {
                errorMessage = $"Missing App.config appSetting: '{ConfigKeys.ServerPort}'.";
            }

            else if (int.TryParse(serverPortValue, out parsedPort) == false)
            {
                errorMessage = "Invalid App.config appSetting: 'serverPort' must be an integer.";
            }
            else if ((parsedPort < 1) || (parsedPort > 65535))
            {
                errorMessage = "Invalid App.config appSetting: 'serverPort' must be between 1 and 65535.";
            }
            else
            {
                settings = new ClientSettings(serverIpValue.Trim(), parsedPort);
                isSuccess = true;
            }

            return (isSuccess);
        }
    }
}
