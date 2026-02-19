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
    // This static class is responsible for loading and validating game data files from the executable directory.
    // It provides methods to validate the files, load valid game data into memory, and retrieve a random game for gameplay.
    // The loader ensures that all game data files adhere to the required format and logs any errors encountered during the loading process for easier debugging and maintenance.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public static class GameDataLoader
    {
        private static readonly Random random = new Random();
        private static readonly string gameDataFilePattern;
        private static readonly List<GameData> loadedGames;
        private static readonly object loaderLock = new object();

        //
        // CONSTRUCTOR : GameDataLoader
        // DESCRIPTION : This static constructor initializes the GameDataLoader class by reading the
        // game data file pattern from the application configuration and setting up the list to hold loaded game data.
        // It ensures that the loader is ready to validate and load game files when requested.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        static GameDataLoader()
        {
            string? configPattern = ConfigurationManager.AppSettings[ConfigKeys.GameDataFilePattern];
            gameDataFilePattern = string.IsNullOrWhiteSpace(configPattern) ? "game*.txt" : configPattern;
            loadedGames = new List<GameData>();
            
            return;
        }

        //
        // METHOD : ValidateAndLoadFiles
        // DESCRIPTION :
        // This method validates and loads all game data files matching the configured pattern from the executable directory.
        // It checks for matching files, reads and validates each one,
        // and adds valid GameData objects to the loadedGames list. Any errors encountered during loading are logged with detailed messages.
        // The method returns the count of valid game files successfully loaded.
        // PARAMETERS : 
        // Logger logger - The Logger instance used for logging messages, warnings, and errors during the validation and loading process.
        // RETURNS :
        // validCount - The number of valid game files that were successfully loaded into memory.
        //
        public static int ValidateAndLoadFiles(Logger logger)
        {
            int validCount = 0;
            string[]? matchingFiles = null;
            GameData? gameData = null;

            // Lock the loader to ensure thread safety while accessing the loadedGames list and performing file operations
            lock (loaderLock)
            {
                loadedGames.Clear();    // Clear any previously loaded games before loading new ones

                // Search for files matching the configured pattern and handle potential exceptions during file access
                try
                {
                    matchingFiles = Directory.GetFiles(".", gameDataFilePattern);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to search for game data files: {ex.Message}");
                    validCount = 0;
                    return validCount;
                }

                // Check if any matching files were found and log an error if none are found
                if (matchingFiles.Length == 0)
                {
                    logger.LogError($"No game data files found matching pattern: {gameDataFilePattern}");
                    validCount = 0;
                    return validCount;
                }

                // Iterate through each matching file, attempt to load and validate it, and log the results
                foreach (string filePath in matchingFiles)
                {
                    try
                    {
                        gameData = LoadAndValidateGameFile(filePath, logger);           // Attempt to load and validate the game data file
                        loadedGames.Add(gameData);                                      // If successful, add the GameData object to the loadedGames list and increment the valid count
                        validCount++;                                                   // Log a message indicating that the file was successfully loaded
                        logger.LogMessage($"Loaded: {Path.GetFileName(filePath)}");     // Log a message indicating that the file was successfully loaded
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Rejected: {Path.GetFileName(filePath)} - {ex.Message}");
                    }
                }
            }

            return validCount;
        }

        //
        // METHOD : LoadRandomGame
        // DESCRIPTION : This method retrieves a random GameData object from the list of loaded games.
        // It ensures thread safety by locking access to the loadedGames list while selecting a random game.
        // If no games are loaded, it throws an InvalidOperationException.
        // PARAMETERS : n/a
        // RETURNS : 
        // GameData - A randomly selected GameData object from the loaded games list.
        //
        public static GameData LoadRandomGame()
        {
            // Check if there are any loaded games and select one at random
            GameData? selectedGame = null;
            int randomIndex = 0;

            // Lock the loader to ensure thread safety while accessing the loadedGames list
            lock (loaderLock)
            {
                if (loadedGames.Count == 0)
                {
                    throw new InvalidOperationException("No games loaded");
                }

                // Generate a random index to select a game from the loadedGames list
                randomIndex = random.Next(loadedGames.Count);
                selectedGame = loadedGames[randomIndex];
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

            // Read all lines from the file and handle potential IO exceptions
            try
            {
                fileLines = File.ReadAllLines(filePath);
            }
            catch (IOException ioEx)
            {
                throw new InvalidDataException($"Cannot read file: {ioEx.Message}");
            }

            // Validate that the file has the minimum required number of lines (puzzle + word count + at least one word)
            if (fileLines == null || fileLines.Length < GameConstants.MinimumFileLines)
            {
                throw new InvalidDataException($"File must contain at least {GameConstants.MinimumFileLines} lines");
            }

            // Validate the puzzle string length
            puzzleString = fileLines[GameConstants.PuzzleLineIndex].Trim();
            if (puzzleString.Length != GameConstants.RequiredPuzzleLength)
            {
                throw new InvalidDataException($"Puzzle string must be exactly {GameConstants.RequiredPuzzleLength} characters (found {puzzleString.Length})");
            }

            // Validate the word count line and ensure it is a positive integer
            if (!int.TryParse(fileLines[GameConstants.WordCountLineIndex].Trim(), out wordCount))
            {
                throw new InvalidDataException("Line 2 must contain a valid integer");
            }

            // Validate that the word count is positive
            if (wordCount <= 0)
            {
                throw new InvalidDataException($"Word count must be positive (found {wordCount})");
            }

            // Validate that the file contains enough lines for the declared word count (puzzle + word count + words)
            words = new List<string>();
            for (int i = GameConstants.FirstWordLineIndex; i < fileLines.Length; i++)
            {
                string word = fileLines[i].Trim();
                if (!string.IsNullOrWhiteSpace(word))
                {
                    words.Add(word);
                }
            }

            // Validate that the actual number of words matches the declared word count
            actualWordCount = words.Count;
            if (actualWordCount != wordCount)
            {
                throw new InvalidDataException($"Word count mismatch: header says {wordCount}, found {actualWordCount} words");
            }

            // Validate that each word exists in the puzzle string (either forward or backward)
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

            // Convert both the word and the puzzle string to uppercase for case-insensitive comparison
            upperWord = word.ToUpper();
            upperPuzzle = puzzleString.ToUpper();

            // Check if the word exists in the puzzle string in its original form
            if (upperPuzzle.Contains(upperWord))
            {
                exists = true;
                return exists;
            }

            // Check if the reversed word exists in the puzzle string
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
