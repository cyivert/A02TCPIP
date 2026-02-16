/*
* FILE :GameDataLoader.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Loads and validates game data files with comprehensive error logging.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordGameServer.Utils;

namespace WordGameServer.Data
{
    //
    // CLASS : GameDataLoader
    // DESCRIPTION :
    // This static class is responsible for loading and validating the game data file.
    // It provides methods to validate the file, load valid game data into memory, and retrieve the game for gameplay.
    // The loader ensures that the game data file adheres to the required format and logs any errors encountered during the loading process for easier debugging and maintenance.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public static class GameDataLoader
    {
        private static readonly string gameDataFile;
        private static GameData? loadedGame;
        private static readonly object loaderLock = new object();

        //
        // CONSTRUCTOR : GameDataLoader
        // DESCRIPTION : This static constructor initializes the GameDataLoader class by reading the
        // game data file path from the application configuration.
        // It ensures that the loader is ready to validate and load the game file when requested.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        static GameDataLoader()
        {
            string? configFile = ConfigurationManager.AppSettings[ConfigKeys.GameDataFile];
            gameDataFile = string.IsNullOrWhiteSpace(configFile) ? "game.txt" : configFile;
            loadedGame = null;
            
            return;
        }

        //
        // METHOD : ValidateAndLoadFiles
        // DESCRIPTION :
        // This method validates and loads the game data file specified in the configuration.
        // It checks for the existence of the file, reads and validates its contents,
        // and loads it as a GameData object. If any errors are encountered, they are logged with detailed messages.
        // The method returns 1 if the file was successfully loaded, 0 otherwise.
        // PARAMETERS : 
        // Logger logger - The Logger instance used for logging messages, warnings, and errors during the validation and loading process.
        // RETURNS :
        // validCount - The number of valid game files that were successfully loaded into memory (0 or 1).
        //
        public static int ValidateAndLoadFiles(Logger logger)
        {
            int validCount = 0;
            
            lock (loaderLock)
            {
                loadedGame = null;

                if (!File.Exists(gameDataFile))
                {
                    logger.LogError($"Game data file not found: {gameDataFile}");
                    validCount = 0;
                    return validCount;
                }

                try
                {
                    loadedGame = LoadAndValidateGameFile(gameDataFile, logger);
                    validCount = 1;
                    logger.LogMessage($"Loaded: {Path.GetFileName(gameDataFile)}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Rejected: {Path.GetFileName(gameDataFile)} - {ex.Message}");
                    validCount = 0;
                }
            }

            return validCount;
        }

        //
        // METHOD : LoadRandomGame
        // DESCRIPTION : This method retrieves the loaded GameData object.
        // It ensures thread safety by locking access while returning the game. If no game is loaded, it throws an InvalidOperationException.
        // PARAMETERS : n/a
        // RETURNS : 
        // GameData - The loaded GameData object for use in gameplay.
        //
        public static GameData LoadRandomGame()
        {
            GameData? selectedGame = null;
            
            lock (loaderLock)
            {
                if (loadedGame == null)
                {
                    throw new InvalidOperationException("No game loaded");
                }

                selectedGame = loadedGame;
            }
            
            return selectedGame!;
        }

        //
        // METHOD : LoadAndValidateGameFile
        // DESCRIPTION :
        // This private method loads and validates a single game data file.
        // It reads the file contents, checks for the correct format (puzzle string length, word count, and word validity),
        // and constructs a GameData object if the file is valid. If any validation step fails, it throws an InvalidDataException
        // with a descriptive error message. The method ensures that all aspects of the game data file adhere to the expected structure before returning a GameData object.
        // PARAMETERS : 
        // string filePath - The path to the game data file to be loaded and validated.
        // Logger logger - The Logger instance used for logging any errors encountered during the loading and validation process.
        // RETURNS :
        // GameData - A GameData object constructed from the valid game data file.
        //
        private static GameData LoadAndValidateGameFile(string filePath, Logger logger)
        {
            GameData? gameData = null;
            string[]? fileLines = null;
            string puzzleString = string.Empty;
            int wordCount = 0;
            List<string>? words = null;
            int actualWordCount = 0;

            try
            {
                fileLines = File.ReadAllLines(filePath);
            }
            catch (IOException ioEx)
            {
                throw new InvalidDataException($"Cannot read file: {ioEx.Message}");
            }

            if (fileLines == null || fileLines.Length < GameConstants.MinimumFileLines)
            {
                throw new InvalidDataException($"File must contain at least {GameConstants.MinimumFileLines} lines");
            }

            puzzleString = fileLines[GameConstants.PuzzleLineIndex].Trim();
            if (puzzleString.Length != GameConstants.RequiredPuzzleLength)
            {
                throw new InvalidDataException($"Puzzle string must be exactly {GameConstants.RequiredPuzzleLength} characters (found {puzzleString.Length})");
            }

            if (!int.TryParse(fileLines[GameConstants.WordCountLineIndex].Trim(), out wordCount))
            {
                throw new InvalidDataException("Line 2 must contain a valid integer");
            }

            if (wordCount <= 0)
            {
                throw new InvalidDataException($"Word count must be positive (found {wordCount})");
            }

            words = new List<string>();
            for (int i = GameConstants.FirstWordLineIndex; i < fileLines.Length; i++)
            {
                string word = fileLines[i].Trim();
                if (!string.IsNullOrWhiteSpace(word))
                {
                    words.Add(word);
                }
            }

            actualWordCount = words.Count;
            if (actualWordCount != wordCount)
            {
                throw new InvalidDataException($"Word count mismatch: header says {wordCount}, found {actualWordCount} words");
            }

            foreach (string word in words)
            {
                if (!WordExistsInPuzzle(word, puzzleString))
                {
                    throw new InvalidDataException($"Word '{word}' not found in puzzle (forward or backward)");
                }
            }

            gameData = new GameData(puzzleString, wordCount, words);
            
            return gameData!;
        }

        //
        // METHOD : WordExistsInPuzzle
        // DESCRIPTION :
        // This private method checks if a given word exists in the puzzle string, either
        // in its original form or reversed. It performs a case-insensitive search by converting both the
        // word and the puzzle string to uppercase before checking for containment. The method returns true if
        // the word is found in either orientation, and false otherwise.
        // PARAMETERS :
        // string word - The word to be checked for existence in the puzzle string.
        // puzzleString - The string representing the letters available for forming words in the game, which is checked for the presence of the specified word.
        // RETURNS :
        // exists - A boolean value indicating whether the specified word exists in the puzzle string, either in its original form or reversed. True if the word is found, false otherwise.
        //
        private static bool WordExistsInPuzzle(string word, string puzzleString)
        {
            bool exists = false;
            string upperWord = string.Empty;
            string upperPuzzle = string.Empty;
            string reversedWord = string.Empty;
            
            upperWord = word.ToUpper();
            upperPuzzle = puzzleString.ToUpper();
            
            if (upperPuzzle.Contains(upperWord))
            {
                exists = true;
                return exists;
            }

            reversedWord = new string(upperWord.Reverse().ToArray());
            if (upperPuzzle.Contains(reversedWord))
            {
                exists = true;
                return exists;
            }

            exists = false;
            return exists;
        }
    }
}
