/*
* FILE            : ProtocolCommands.cs(Client)
* PROJECT         : A02TCPIP
* PROGRAMMER      :
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Client-side protocol command names.
*/

namespace WordGameClient.Utils
{
    public static class ProtocolCommands
    {
        public const string LoginInfo = "login_info";
        public const string WordValidation = "word_validation";
        public const string TimeLeft = "time_left";
        public const string ChoicesLeft = "choices_left";
        public const string CheckEndGame = "check_end_game";
        public const string CheckNewGame = "check_new_game";
        public const string CheckForNewPlayer = "check_for_new_player";
        public const string GetStringFromServer = "get_string_from_server";
    }
}
