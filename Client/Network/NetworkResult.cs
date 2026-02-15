/*
* FILE            : NetworkResult.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Result wrapper for network operations.
*/

namespace WordGameClient.Network
{
    public sealed class NetworkResult
    {
        public bool IsSuccess { get; }
        public string ResponseLine { get; }
        public string ErrorMessage { get; }

        public NetworkResult(bool isSuccess, string responseLine, string errorMessage)
        {
            this.IsSuccess = isSuccess;
            this.ResponseLine = responseLine;
            this.ErrorMessage = errorMessage;

            return;
        }
    }
}
