using System;
using System.Configuration;
using WordGameClient.Utils;

namespace WordGameClient.Models
{
    public sealed class ClientSettings
    {
        public string ServerIp { get; }
        public int ServerPort { get; }
        public int ConnectTimeoutMs { get; }
        public int IoTimeoutMs { get; }

        private ClientSettings(string serverIp, int serverPort, int connectTimeoutMs, int ioTimeoutMs)
        {
            this.ServerIp = serverIp;
            this.ServerPort = serverPort;
            this.ConnectTimeoutMs = connectTimeoutMs;
            this.IoTimeoutMs = ioTimeoutMs;

            return;
        }

        public static bool TryLoad(out ClientSettings? settings, out string errorMessage)
        {
            bool isSuccess = false;

            settings = null;
            errorMessage = string.Empty;

            string? serverIpValue = ConfigurationManager.AppSettings[ConfigKeys.ServerIp];
            string? serverPortValue = ConfigurationManager.AppSettings[ConfigKeys.ServerPort];
            string? connectTimeoutValue = ConfigurationManager.AppSettings[ConfigKeys.ConnectTimeoutMs];
            string? ioTimeoutValue = ConfigurationManager.AppSettings[ConfigKeys.IoTimeoutMs];

            int parsedPort = 0;
            int parsedConnectTimeout = 0;
            int parsedIoTimeout = 0;

            if (string.IsNullOrWhiteSpace(serverIpValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ServerIp + "'.";
            }
            else if (string.IsNullOrWhiteSpace(serverPortValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ServerPort + "'.";
            }
            else if (string.IsNullOrWhiteSpace(connectTimeoutValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "'.";
            }
            else if (string.IsNullOrWhiteSpace(ioTimeoutValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "'.";
            }
            else if (int.TryParse(serverPortValue, out parsedPort) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ServerPort + "' must be an integer.";
            }
            else if ((parsedPort < 1) || (parsedPort > 65535))
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ServerPort + "' must be between 1 and 65535.";
            }
            else if (int.TryParse(connectTimeoutValue, out parsedConnectTimeout) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "' must be an integer.";
            }
            else if (parsedConnectTimeout <= 0)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "' must be > 0.";
            }
            else if (int.TryParse(ioTimeoutValue, out parsedIoTimeout) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "' must be an integer.";
            }
            else if (parsedIoTimeout <= 0)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "' must be > 0.";
            }
            else
            {
                settings = new ClientSettings(serverIpValue.Trim(), parsedPort, parsedConnectTimeout, parsedIoTimeout);
                isSuccess = true;
            }

            return (isSuccess);
        }
    }
}
