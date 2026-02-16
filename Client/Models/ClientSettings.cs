/*
* FILE            : ClientSettings.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Loads and validates client configuration from App.config.
*/

using System;
using System.Configuration;
using WordGameClient.Utils;

namespace WordGameClient.Models
{

    //
    // CLASS : ClientSettings
    // DESCRIPTION : Immutable container for validated client configuration settings (server IP, port, timeouts).
    //               Settings are loaded from App.config via the static TryLoad method.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class ClientSettings
    {
        // Minimum and maximum allowed port numbers (valid TCP port range).
        private const int kMinPort = 1;
        private const int kMaxPort = 65535;

        // Minimum and maximum allowed timeout values in milliseconds (1 ms to 120 seconds).
        private const int kMinTimeoutMs = 1;
        private const int kMaxTimeoutMs = 120000;

        // Public read‑only properties exposing the validated configuration.
        public string ServerIp { get; }
        public int ServerPort { get; }
        public int ConnectTimeoutMs { get; }
        public int IoTimeoutMs { get; }

        //
        // FUNCTION : ClientSettings (constructor)
        // DESCRIPTION : Private constructor used internally by TryLoad to create an immutable settings instance.
        //               All parameters are assumed to be already validated.
        // PARAMETERS : string serverIp - validated server IP address or hostname;
        //              int serverPort - validated port number;
        //              int connectTimeoutMs - validated connection timeout in ms;
        //              int ioTimeoutMs - validated I/O timeout in ms.
        // RETURNS : n/a (constructor)
        //

        private ClientSettings(string serverIp, int serverPort, int connectTimeoutMs, int ioTimeoutMs)
        {
            this.ServerIp = serverIp;
            this.ServerPort = serverPort;
            this.ConnectTimeoutMs = connectTimeoutMs;
            this.IoTimeoutMs = ioTimeoutMs;

            return;
        }

        //
        // FUNCTION : TryLoad
        // DESCRIPTION : Attempts to read and validate all required client settings from App.config
        //               Returns true and populates 'settings' if all values are present and valid;
        //               otherwise returns false and provides an error message.
        // PARAMETERS : out ClientSettings? settings - the constructed settings object if successful (null otherwise);
        //              out string errorMessage - descriptive error message if validation fails
        // RETURNS : bool - true if settings were successfully loaded; false otherwise
        //
        public static bool TryLoad(out ClientSettings? settings, out string errorMessage)
        {
            bool isSuccess = false;

            settings = null;
            errorMessage = string.Empty;

            // Retrieve raw string values from the application configuration file
            string? serverIpValue = ConfigurationManager.AppSettings[ConfigKeys.ServerIp];
            string? serverPortValue = ConfigurationManager.AppSettings[ConfigKeys.ServerPort];
            string? connectTimeoutValue = ConfigurationManager.AppSettings[ConfigKeys.ConnectTimeoutMs];
            string? ioTimeoutValue = ConfigurationManager.AppSettings[ConfigKeys.IoTimeoutMs];

            int parsedPort = 0;
            int parsedConnectTimeout = 0;
            int parsedIoTimeout = 0;

            // Validate each setting step‑by‑step, producing a specific error message for the first failure.
            // Check for missing IP address.
            if (string.IsNullOrWhiteSpace(serverIpValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ServerIp + "'.";
            }
            // Check for missing port value.
            else if (string.IsNullOrWhiteSpace(serverPortValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ServerPort + "'.";
            }
            // Verify that the port string is a valid integer.
            else if (int.TryParse(serverPortValue, out parsedPort) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ServerPort + "' must be an integer.";
            }
            // Ensure the port falls within the allowed TCP range.
            else if ((parsedPort < kMinPort) || (parsedPort > kMaxPort))
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ServerPort + "' must be between 1 and 65535.";
            }
            // Check for missing connection timeout.
            else if (string.IsNullOrWhiteSpace(connectTimeoutValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "'.";
            }
            // Verify that the connection timeout is a valid integer.
            else if (int.TryParse(connectTimeoutValue, out parsedConnectTimeout) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "' must be an integer.";
            }
            // Enforce connection timeout range.
            else if ((parsedConnectTimeout < kMinTimeoutMs) || (parsedConnectTimeout > kMaxTimeoutMs))
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.ConnectTimeoutMs + "' must be between 1 and 120000.";
            }
            // Check for missing I/O timeout.
            else if (string.IsNullOrWhiteSpace(ioTimeoutValue) == true)
            {
                errorMessage = "Missing App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "'.";
            }
            // Verify that the I/O timeout is a valid integer.
            else if (int.TryParse(ioTimeoutValue, out parsedIoTimeout) == false)
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "' must be an integer.";
            }
            else if ((parsedIoTimeout < kMinTimeoutMs) || (parsedIoTimeout > kMaxTimeoutMs))
            {
                errorMessage = "Invalid App.config appSetting: '" + ConfigKeys.IoTimeoutMs + "' must be between 1 and 120000.";
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
