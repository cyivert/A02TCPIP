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
    // DESCRIPTION : This class manages the state of a single game session, including tracking found words, guess count, and game status.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public class GameSession
    {
        public static readonly int GameDurationSeconds;
        private static readonly int BasePointsPerWord;
        private static readonly int TimeBonusMultiplier;
        public static readonly int MaxGuesses;
        private static readonly int WrongGuessPenalty;

        private readonly GameData gameData;
        private readonly HashSet<string> foundWords;
        private readonly GameTimer gameTimer;
        private readonly object sessionLock;
        private int guessCount;
        private int foundCount;
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

        //
        // CONSTRUCTOR : GameSession
        // DESCRIPTION :
        // The static constructor is responsible for initializing the static configuration values for the GameSession class.
        // It reads the game duration, base points per word, and time bonus multiplier from the application configuration settings.
        // If the values are not valid (e.g., not integers or below minimum thresholds), it falls back to default values defined in the GameConstants class.
        // This ensures that the game session operates with valid configuration parameters while allowing for flexibility through external configuration.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        static GameSession()
        {
            string? durationValue = ConfigurationManager.AppSettings[ConfigKeys.GameDurationSeconds];
            string? basePointsValue = ConfigurationManager.AppSettings[ConfigKeys.BasePointsPerWord];
            string? bonusMultiplierValue = ConfigurationManager.AppSettings[ConfigKeys.TimeBonusMultiplier];
            string? maxGuessesValue = ConfigurationManager.AppSettings[ConfigKeys.MaxGuesses];
            string? penaltyValue = ConfigurationManager.AppSettings[ConfigKeys.WrongGuessPenalty];
            int parsedDuration = 0;
            int parsedPoints = 0;
            int parsedMultiplier = 0;
            int parsedMaxGuesses = 0;
            int parsedPenalty = 0;

            if (int.TryParse(durationValue, out parsedDuration) && parsedDuration >= GameConstants.MinGameDuration)
            {
                GameDurationSeconds = parsedDuration;
            }
            else
            {
                GameDurationSeconds = GameConstants.DefaultGameDurationSeconds;
            }

            if (int.TryParse(basePointsValue, out parsedPoints) && parsedPoints >= GameConstants.MinPointsPerWord)
            {
                BasePointsPerWord = parsedPoints;
            }
            else
            {
                BasePointsPerWord = GameConstants.DefaultBasePointsPerWord;
            }

            if (int.TryParse(bonusMultiplierValue, out parsedMultiplier) && parsedMultiplier >= GameConstants.MinBonusMultiplier)
            {
                TimeBonusMultiplier = parsedMultiplier;
            }
            else
            {
                TimeBonusMultiplier = GameConstants.DefaultTimeBonusMultiplier;
            }

            if (int.TryParse(maxGuessesValue, out parsedMaxGuesses) && parsedMaxGuesses >= GameConstants.MinMaxGuesses)
            {
                MaxGuesses = parsedMaxGuesses;
            }
            else
            {
                MaxGuesses = GameConstants.DefaultMaxGuesses;
            }

            if (int.TryParse(penaltyValue, out parsedPenalty) && parsedPenalty >= GameConstants.MinWrongGuessPenalty)
            {
                WrongGuessPenalty = parsedPenalty;
            }
            else
            {
                WrongGuessPenalty = GameConstants.DefaultWrongGuessPenalty;
            }

            return;
        }

        //
        // CONSTRUCTOR : GameSession
        // DESCRIPTION :
        // This constructor initializes a new instance of the GameSession class with the provided GameData.
        // It sets up the necessary data structures and state for managing a game session, including:
        // PARAMETERS :
        // GameData gameData - The GameData object contains the puzzle and valid words for the game session. This data is essential for processing player guesses and determining game outcomes.
        // RETURNS : n/a
        //
        public GameSession(GameData gameData)
        {
            this.gameData = gameData;
            this.foundWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.gameTimer = new GameTimer(GameDurationSeconds);
            this.sessionLock = new object();
            this.guessCount = 0;
            this.foundCount = 0;
            this.status = GameStatus.Active;

            return;
        }

        //
        // METHOD : ProcessGuess
        // DESCRIPTION : Checks if the provided word is a valid guess in the current game session.
        // PARAMETERS : 
        // string word - The player's guess to be processed. This is the word that the player is attempting to find in the game session.
        // RETURNS :
        // GuessResult - An enumeration value indicating the result of processing the guess, which can be:
        //
        public GuessResult ProcessGuess(string word)
        {
            GuessResult result = GuessResult.NotFound;
            string normalizedWord = string.Empty;

            lock (this.sessionLock)
            {
                if (this.gameTimer.IsExpired())
                {
                    result = GuessResult.TimeExpired;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(word))
                {
                    this.guessCount++;
                    result = GuessResult.NotFound;
                    return result;
                }

                normalizedWord = word.Trim().ToUpper();

                if (this.foundWords.Contains(normalizedWord))
                {
                    result = GuessResult.AlreadyFound;
                    return result;
                }

                if (this.gameData.IsValidWord(normalizedWord))
                {
                    this.foundWords.Add(normalizedWord);
                    this.foundCount++;
                    result = GuessResult.Found;
                    return result;
                }

                // Only count incorrect guesses toward the tries limit
                this.guessCount++;
                result = GuessResult.NotFound;
            }

            return result;
        }

        //
        // METHOD : IsGameOver
        // DESCRIPTION : This method checks if the game session has ended due to the timer expiring. It does this by calling the IsExpired method on the GameTimer instance.
        // PARAMETERS : n/a
        // RETURNS :
        // isOver - A boolean value indicating whether the game session is over (true if the timer has expired, false otherwise).
        public bool IsGameOver()
        {
            bool isOver = false;

            isOver = this.gameTimer.IsExpired();

            return isOver;
        }

        public bool IsGameComplete()
        {
            bool isComplete = false;

            lock (this.sessionLock)
            {
                isComplete = this.foundCount >= this.gameData.WordCount;
            }

            return isComplete;
        }

        //
        // METHOD : GetFoundCount
        // DESCRIPTION :
        // This method retrieves the current count of valid words that the player has found during the game session.
        // It locks the sessionLock to ensure thread-safe access to the foundWords collection and returns the count of found words.
        // PARAMETERS : n/a
        // RETURNS :
        // count - An integer representing the number of valid words that the player has found so far in the game session.
        //
        public int GetFoundCount()
        {
            int count = 0;

            lock (this.sessionLock)
            {
                count = this.foundCount;
            }

            return count;
        }

        //
        // METHOD : GetTotalWords
        // DESCRIPTION : This method retrieves the total number of valid words that are present in the game session's puzzle.
        // PARAMETERS : n/a
        // RETURNS :
        // count - An integer representing the total number of valid words that are available in the game session's puzzle, which is determined by the GameData associated with the session.
        //
        public int GetTotalWords()
        {
            int count = 0;

            count = this.gameData.WordCount;

            return count;
        }

        //
        // METHOD : GetScore
        // DESCRIPTION : This method calculates and returns the player's current score based on the number of valid words found and any applicable time bonuses.
        // PARAMETERS : n/a
        // RETURNS :
        // totalScore - An integer representing the player's current score, which is calculated by multiplying the number of valid words found by
        // the base points per word and adding any time bonus if the game is complete and not over.
        //
        public int GetScore()
        {
            int totalScore = 0;
            int baseScore = 0;
            int timeBonus = 0;
            int wrongPenalty = 0;
            int remainingSeconds = 0;

            lock (this.sessionLock)
            {
                baseScore = this.foundCount * BasePointsPerWord;
                wrongPenalty = this.guessCount * WrongGuessPenalty;

                if (this.IsGameComplete() && !this.IsGameOver())
                {
                    remainingSeconds = this.gameTimer.GetRemainingSeconds();
                    timeBonus = remainingSeconds * TimeBonusMultiplier;
                }

                totalScore = baseScore + timeBonus - wrongPenalty;

                if (totalScore < 0)
                {
                    totalScore = 0;
                }
            }

            return totalScore;
        }

        //
        // METHOD : GetRemainingSeconds
        // DESCRIPTION : This method retrieves the number of seconds remaining before the game timer expires. It calls the GetRemainingSeconds method on the GameTimer instance to obtain this information.
        // PARAMETERS : n/a
        // RETURNS :
        // remaining - An integer representing the number of seconds remaining before the game timer expires.
        public int GetRemainingSeconds()
        {
            int remaining = 0;

            remaining = this.gameTimer.GetRemainingSeconds();

            return remaining;
        }

        public bool IsOutOfTries()
        {
            bool outOfTries = false;

            lock (this.sessionLock)
            {
                outOfTries = this.guessCount >= MaxGuesses;
            }

            return outOfTries;
        }
    }
}
