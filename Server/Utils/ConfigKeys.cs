/*
* FILE : ConfigKeys.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Configuration initialized for App.config file access.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Utils
{
    //
    // CLASS : ConfigKeys
    // DESCRIPTION : This static class defines constant string keys for accessing configuration values from the App.config file.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public static class ConfigKeys
    {
        public const string ServerPort = "ServerPort";
        public const string GameDataDirectory = "GameDataDirectory";
        public const string GameDurationSeconds = "GameDurationSeconds";
        public const string BasePointsPerWord = "BasePointsPerWord";
        public const string TimeBonusMultiplier = "TimeBonusMultiplier";
    }
}
