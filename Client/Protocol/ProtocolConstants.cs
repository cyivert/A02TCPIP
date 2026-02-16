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
    //
    // CLASS : ProtocolConstants
    // DESCRIPTION : Static class containing protocol constants specific to the client side,
    //               such as delimiters used in protocol messages.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public static class ProtocolConstants
    {
        public const char Delimiter = '|';
    }
}
