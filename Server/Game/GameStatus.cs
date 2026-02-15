/*
* FILE : GameResult.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Enumeration for tracking game session status.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Game
{

    //
    // FUNCTION : GameStatus
    // DESCRIPTION : Enum for tracking game session status.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public enum GameStatus
    {
        Active,                 // The game is currently in progress and has not yet been won or lost.
        Won,                    // The game has been won by the player, indicating that they have successfully completed the game objectives.
        Lost,                   // The game has been lost by the player, indicating that they have failed to meet the game objectives within the allowed conditions (e.g., time limit, number of guesses).
        Ended                   // The game session has been ended, either by the player quitting or by the server terminating the session. This status indicates that the game is no longer active and cannot be continued.
    }
}
