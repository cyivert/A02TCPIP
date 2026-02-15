/*
* FILE : GameServer.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Manages the state of a single game session with thread-safe operations.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Game
{
    //
    // FUNCTION : GameSession
    // DESCRIPTION :
    // PARAMETERS : 
    // RETURNS :
    //
    public class GameSession
    {
        public static readonly int GameDurationSeconds;
        private static readonly int BasePointsPerWord;
        private static readonly int TimeBonusMultiplier;
    }
}
