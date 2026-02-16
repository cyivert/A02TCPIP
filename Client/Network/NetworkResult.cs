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
    //
    // CLASS : NetworkResult
    // DESCRIPTION : Immutable result object returned by network operations, containing success status,
    //               the response line (if successful), and an error message (if failure).
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class NetworkResult
    {
        public bool IsSuccess { get; }
        public string ResponseLine { get; }
        public string ErrorMessage { get; }

        //
        // FUNCTION : NetworkResult (constructor)
        // DESCRIPTION : Initializes a new immutable NetworkResult instance with the given values.
        // PARAMETERS : bool isSuccess - true if the operation succeeded, false otherwise;
        //              string responseLine - the server's response line (ignored on failure, but may be empty);
        //              string errorMessage - a description of the error (ignored on success, but may be empty).
        // RETURNS : n/a (constructor)
        //

        public NetworkResult(bool isSuccess, string responseLine, string errorMessage)
        {
            this.IsSuccess = isSuccess;
            this.ResponseLine = responseLine;
            this.ErrorMessage = errorMessage;

            return;
        }
    }
}
