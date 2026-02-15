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
using System.Configuration;
using WordGameServer.Data;
using WordGameServer.Utils;

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

        private readonly GameData gameData;
        private readonly HashSet<string> foundWords;
        private readonly GameTimer gameTimer;
        private readonly object sessionLock;
        private int guessCount;
        private GameStatus status;

        //
        // PROPERTY : GuessCount
        // DESCRIPTION :
        // It allows external code to retrieve the current number of guesses made by the player while ensuring that the internal state is protected from concurrent modifications.
        // The getter method locks the sessionLock object to synchronize access to the guessCount variable.
        // PARAMETERS : 
        // RETURNS :
        //
        public int GuessCount
        {
            get
            {
                int count = 0;
                lock (this.sessionLock)
                {
                    count = this.guessCount;
                }
                return count;
            }
        }

        //
        // PROPERTY : Status
        // DESCRIPTION : 
        // The getter and setter methods both lock the sessionLock object to ensure that the status variable is accessed and modified in a thread-safe manner, preventing
        // race conditions.
        // PARAMETERS : 
        // RETURNS :
        //
        public GameStatus Status
        {
            get
            {
                GameStatus currentStatus = GameStatus.Active;
                lock (this.sessionLock)
                {
                    currentStatus = this.status;
                }
                return currentStatus;
            }
            set
            {
                lock (this.sessionLock)
                {
                    this.status = value;
                }
                return;
            }
        }
    }
}
