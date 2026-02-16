/*
* FILE            : ConfigKeys.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-12
* DESCRIPTION     :
*   Defines App.config key names used by the WordGameClient application.
*/

namespace WordGameClient.Utils
{

    //
    // CLASS : ConfigKeys
    // DESCRIPTION : Static class containing constant string keys for accessing application configuration settings in App.config.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public static class ConfigKeys
    {
        public const string ServerIp = "serverIp";
        public const string ServerPort = "serverPort";

        public const string ConnectTimeoutMs = "connectTimeoutMs";
        public const string IoTimeoutMs = "ioTimeoutMs";
    }

}
