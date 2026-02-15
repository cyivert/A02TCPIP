using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Utils
{
    public static class GameConstants
    {
        // Network Configuration
        public const int DefaultServerPort = 5000;                              // Default port number for the game server to listen on.
        public const int MinPortNumber = 1;                                     // Minimum valid port number for the game server to listen on.
        public const int MaxPortNumber = 65535;                                 // Maximum valid port number for the game server to listen on.
        public const int MaxConcurrentClients = 100;                            // Maximum number of concurrent client connections the server can handle.
        public const int ClientReadTimeoutMs = 30000;                           // Timeout duration in milliseconds for reading data from a client connection.

        // Game Timing
        public const int DefaultGameDurationSeconds = 120;                      // Default duration of a game session in seconds.
        public const int MinGameDuration = 30;                                  // Minimum allowed duration of a game session in seconds.
        public const int MaxGameDuration = 600;                                 // Maximum allowed duration of a game session in seconds.

        // Scoring
        public const int DefaultBasePointsPerWord = 100;                        // Default base points awarded for each valid word found by the player.
        public const int DefaultTimeBonusMultiplier = 2;                        // Default multiplier for calculating time-based bonus points, which encourages players to find words more quickly.
        public const int MinPointsPerWord = 10;                                 // Minimum points awarded for each valid word found by the player.
        public const int MinBonusMultiplier = 1;                                // Minimum multiplier for calculating time-based bonus points.

        // Puzzle Requirements
        public const int RequiredPuzzleLength = 30;                             // Required length of the puzzle string, which represents the letters available for forming words in the game.
        public const int MinimumWordsInPuzzle = 1;                              // Minimum number of valid words that must be present in the puzzle for it to be considered valid and playable.
        public const int MaximumWordsInPuzzle = 20;                             // Maximum number of valid words that can be present in the puzzle to ensure a manageable game experience for players.

        // File Validation
        public const int MinimumFileLines = 3;                                  // Minimum number of lines required in the puzzle file to ensure it contains the necessary information for a valid game session (puzzle string, word count, and at least one valid word).
        public const int PuzzleLineIndex = 0;                                   // Line index in the puzzle file where the puzzle string is expected to be located.
        public const int WordCountLineIndex = 1;                                // Line index in the puzzle file where the expected number of valid words is specified.
        public const int FirstWordLineIndex = 2;                                // Line index in the puzzle file where the first valid word is expected to be located, with subsequent valid words following on the next lines.

        // Protocol
        public const char MessageDelimiter = '|';
        public const string LineTerminator = "\n";

        // Messages
        public const string WelcomeMessage = "WELCOME....";
        public const string GoodbyeMessage = "GOODBYE Thanks for playing!";
    }
}
