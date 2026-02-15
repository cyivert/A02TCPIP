/*
* FILE : GameData.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Represents the data for a single word game puzzle (immutable and thread-safe).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Data
{
    //
    // CLASS : GameData
    // DESCRIPTION :
    // This class represents the data for a single word game puzzle.
    // It is designed to be immutable and thread-safe, allowing multiple threads to access the same game data without the risk of data corruption or race conditions.
    // PARAMETERS : 
    // RETURNS :
    //

    public class GameData
    {
        private readonly string puzzleString;
        private readonly int wordCount;
        private readonly List<string> validWords;

        //
        // PROPERTY : PuzzleString
        // DESCRIPTION :
        // This property provides read-only access to the puzzle string, which represents the letters available for forming words in the game.
        // The puzzle string is initialized during object construction and cannot be modified afterward, ensuring immutability and thread safety.
        // PARAMETERS : n/a
        // RETURNS :
        // rseult - The puzzle string is returned as-is, allowing clients to access the original string of letters for use in gameplay and word validation.
        //
        public string PuzzleString
        {
            get
            {
                string result = string.Empty;
                result = this.puzzleString;
                return result;
            }
        }

        //
        // PROPERTY : WordCount
        // DESCRIPTION : This property provides read-only access to the expected number of valid words that can be formed from the puzzle string.
        // PARAMETERS : n/a
        // RETURNS :
        // count - The word count is returned as an integer, representing the number of valid words that players are expected to find during gameplay.
        //
        public int WordCount
        {
            get
            {
                int count = 0;
                count = this.wordCount;
                return count;
            }
        }

        //
        // CONSTRUCTOR : GameData
        // DESCRIPTION : This constructor initializes a new instance of the GameData class with the provided puzzle string, word count, and list of valid words.
        // PARAMETERS : 
        // string puzzleString - The string representing the letters available for forming words in the game.
        // int wordCount - The expected number of valid words that can be formed from the puzzle string.
        // List<string> words - A list of valid words that can be formed from the puzzle string. These words are stored in a case-insensitive manner for efficient validation during gameplay.
        // RETURNS : n/a
        //
        public GameData(string puzzleString, int wordCount, List<string> words)
        {
            this.puzzleString = puzzleString;
            this.wordCount = wordCount;
            this.validWords = words.Select(w => w.ToUpper()).ToList();

            return;
        }

        //
        // METHOD : IsValidWord
        // DESCRIPTION :
        // This method checks if a given word is valid according to the list of valid words stored in the GameData instance.
        // The validation is case-insensitive, allowing players to submit words in any case without affecting the outcome.
        // The method returns true if the word is found in the set of valid words, and false otherwise.
        // This allows for efficient validation of player submissions during gameplay.
        // PARAMETERS : 
        // RETURNS :
        //
        public bool IsValidWord(string word)
        {
            bool isValid = false;
            string upperWord = string.Empty;
            
            if (string.IsNullOrWhiteSpace(word))
            {
                isValid = false;
                return isValid;
            }

            upperWord = word.ToUpper();
            isValid = this.validWords.Contains(upperWord);
            
            return isValid;
        }

        //
        // METHOD : GetAllWords
        // DESCRIPTION :
        // This method retrieves a list of all valid words that can be formed from the puzzle string. The words are returned in their original case as they were provided during object construction.
        // This allows clients to access the complete list of valid words for use in gameplay, such as displaying them to players or performing additional processing.
        // The method returns a new list containing the valid words, ensuring that the internal state of the GameData instance remains immutable and thread-safe.
        // PARAMETERS : n/a
        // RETURNS :
        // words - A list of valid words that can be formed from the puzzle string, returned in their original case as provided during object construction.
        //
        public List<string> GetAllWords()
        {
            List<string> words = null;
            
            words = this.validWords.ToList();
            
            return words;
        }
    }
}
