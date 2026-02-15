/*
* FILE : GameResult.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Enumeration for word guess results.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Game
{
    //
    // ENUM : GuessResult
    // DESCRIPTION :
    // Enum for tracking the result of a player's guess in the word game.
    // This enumeration provides a standardized way to represent the outcome of a guess, allowing the game logic to easily determine how to respond to the player's input.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public enum GuessResult
    {
        Found,                              // This indicates a successful guess by the player.
        NotFound,                           // This indicates an unsuccessful guess, and the player may need to try again.
        AlreadyFound,                       // This indicates that the player has already successfully guessed this word, and it should not be counted again.
        TimeExpired,                        // This indicates that the player took too long to make a guess, and the game may need to end or penalize the player accordingly.
        InvalidGame                         // This indicates that the player's guess cannot be processed because the game session is no longer active.
    }
}
