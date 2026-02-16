/*
* FILE            : ProtocolCommands.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Client-side protocol command names matching the server protocol.
*/

namespace WordGameClient.Protocol
{
    public static class ProtocolCommands
    {
        // Client to Server Commands
        public const string Start = "START";
        public const string Guess = "GUESS";
        public const string Quit = "QUIT";
        public const string EndGame = "ENDGAME";
        public const string Progress = "PROGRESS";

        // Server to Client Responses
        public const string Welcome = "WELCOME";
        public const string GameStart = "GAMESTART";
        public const string WordFound = "WORD FOUND";
        public const string WordDoesNotExist = "WORD DOES NOT EXIST";
        public const string WordAlreadyExist = "WORD ALREADY EXIST";
        public const string GameOver = "GAMEOVER";
        public const string GameEnded = "GAMEENDED";
        public const string ProgressUpdate = "PROGRESS";
        public const string Goodbye = "GOODBYE";
        public const string Error = "ERROR";
    }
}
