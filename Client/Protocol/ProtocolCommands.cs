/*
* FILE            : ProtocolCommands.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-13
* DESCRIPTION     :
*   Client-side protocol command names used in request/response messages.
*/

namespace WordGameClient.Protocol
{
    public static class ProtocolCommands
    {
        public const string LoginInfo = "LOGIN_INFO";
        public const string WordValidation = "WORD_VALIDATION";
        public const string TimeLeft = "TIME_LEFT";
        public const string ChoicesLeft = "CHOICES_LEFT";
        public const string CheckEndGame = "CHECK_END_GAME";
        public const string CheckNewGame = "CHECK_NEW_GAME";
        public const string CheckNewPlayer = "CHECK_NEW_PLAYER";
        public const string GetStringFromServer = "GET_STRING_FROM_SERVER";
    }
}
