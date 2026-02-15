/*
* FILE            : ProtocolConstants.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Protocol constants used only by the client.
*/

namespace WordGameClient.Protocol
{
    public static class ProtocolConstants
    {
        public const char Delimiter = '|';
        public const string Ok = "OK";
        public const string Err = "ERR";
    }
}
