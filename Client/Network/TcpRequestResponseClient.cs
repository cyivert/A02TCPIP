/*
* FILE            : TcpRequestResponseClient.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Async TCP client that maintains a persistent connection to the server.
*   Supports connect, send/receive, and disconnect operations.
*/

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WordGameClient.Models;

namespace WordGameClient.Network
{

    //
    // CLASS : TcpRequestResponseClient
    // DESCRIPTION : Async TCP client that maintains a persistent connection to a server,
    //               handling connect, send/receive, and disconnect operations with timeout support.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class TcpRequestResponseClient : IDisposable
    {
        private TcpClient? tcpClient;
        private NetworkStream? stream;
        private StreamReader? reader;
        private StreamWriter? writer;
        private bool isConnected;
        private bool isDisposed;

        //
        // PROPERTY : IsConnected
        // DESCRIPTION : Returns the current connection status of the client.
        // PARAMETERS : n/a
        // RETURNS : bool - true if connected, false otherwise.
        //
        public bool IsConnected
        {
            get { return (this.isConnected); }
        }

        //
        // FUNCTION : TcpRequestResponseClient (constructor)
        // DESCRIPTION : Initializes a new instance of the client with default null values.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public TcpRequestResponseClient()
        {
            this.tcpClient = null;
            this.stream = null;
            this.reader = null;
            this.writer = null;
            this.isConnected = false;
            this.isDisposed = false;

            return;
        }

        //
        // FUNCTION : ConnectAsync
        // DESCRIPTION : Attempts to establish a TCP connection to the server using the provided settings.
        //               Implements both connection timeout and I/O timeout for the welcome message.
        //               On success, reads and returns the server's welcome message.
        // PARAMETERS : ClientSettings settings - validated client settings (IP, port, timeouts);
        //              CancellationToken cancellationToken - token to cancel the operation.
        // RETURNS : Task<NetworkResult> - result containing success flag, welcome line, or error message.
        //
        public async Task<NetworkResult> ConnectAsync(ClientSettings settings, CancellationToken cancellationToken)
        {
            bool isSuccess = false;
            string responseLine = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                this.Disconnect();

                this.tcpClient = new TcpClient();

                Task connectTask = this.tcpClient.ConnectAsync(settings.ServerIp, settings.ServerPort);
                Task connectTimeoutTask = Task.Delay(settings.ConnectTimeoutMs, cancellationToken);

                Task completedConnect = await Task.WhenAny(connectTask, connectTimeoutTask);

                if (completedConnect == connectTimeoutTask)
                {
                    errorMessage = "Connect timed out.";
                    this.Disconnect();
                }
                else
                {
                    await connectTask;

                    this.stream = this.tcpClient.GetStream();
                    this.reader = new StreamReader(this.stream, Encoding.UTF8);
                    this.writer = new StreamWriter(this.stream, Encoding.UTF8);
                    this.writer.AutoFlush = true;

                    // Read the WELCOME message from the server
                    Task<string?> readTask = this.reader.ReadLineAsync();
                    Task ioTimeoutTask = Task.Delay(settings.IoTimeoutMs, cancellationToken);

                    Task completedRead = await Task.WhenAny(readTask, ioTimeoutTask);

                    if (completedRead == ioTimeoutTask)
                    {
                        errorMessage = "Timed out waiting for welcome message.";
                        this.Disconnect();
                    }
                    else
                    {
                        string? welcomeLine = await readTask;

                        if (string.IsNullOrWhiteSpace(welcomeLine) == true)
                        {
                            errorMessage = "No welcome message received.";
                            this.Disconnect();
                        }
                        else
                        {
                            responseLine = welcomeLine.Trim();
                            this.isConnected = true;
                            isSuccess = true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                this.Disconnect();
            }

            NetworkResult result = new NetworkResult(isSuccess, responseLine, errorMessage);

            return (result);
        }

        //
        // FUNCTION : SendAndReceiveAsync
        // DESCRIPTION : Sends a request line to the server and waits for a response line.
        //               Uses the configured I/O timeout for the read operation.
        // PARAMETERS : ClientSettings settings - validated client settings (I/O timeout);
        //              string requestLine - the line to send to the server;
        //              CancellationToken cancellationToken - token to cancel the operation.
        // RETURNS : Task<NetworkResult> - result containing success flag, response line, or error message.
        //

        public async Task<NetworkResult> SendAndReceiveAsync(ClientSettings settings, string requestLine, CancellationToken cancellationToken)
        {
            bool isSuccess = false;
            string responseLine = string.Empty;
            string errorMessage = string.Empty;

            if ((this.isConnected == false) || (this.writer == null) || (this.reader == null))
            {
                errorMessage = "Not connected to server.";
                NetworkResult failResult = new NetworkResult(isSuccess, responseLine, errorMessage);
                return (failResult);
            }

            try
            {
                await this.writer.WriteLineAsync(requestLine);

                Task<string?> readTask = this.reader.ReadLineAsync();
                Task ioTimeoutTask = Task.Delay(settings.IoTimeoutMs, cancellationToken);

                Task completedRead = await Task.WhenAny(readTask, ioTimeoutTask);

                if (completedRead == ioTimeoutTask)
                {
                    errorMessage = "Read timed out.";
                    this.Disconnect();
                }
                else
                {
                    string? line = await readTask;

                    if (string.IsNullOrWhiteSpace(line) == true)
                    {
                        errorMessage = "No response received from server.";
                        this.Disconnect();
                    }
                    else
                    {
                        responseLine = line.Trim();
                        isSuccess = true;
                    }
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                this.Disconnect();
            }

            NetworkResult result = new NetworkResult(isSuccess, responseLine, errorMessage);

            return (result);
        }

        //
        // FUNCTION : Disconnect
        // DESCRIPTION : Closes the TCP connection and disposes of all associated resources.
        //               Sets isConnected to false and nulls out references.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        public void Disconnect()
        {
            this.isConnected = false;

            try
            {
                this.writer?.Dispose();
                this.reader?.Dispose();
                this.stream?.Dispose();
                this.tcpClient?.Close();
            }
            catch
            {
                // Ignore cleanup errors
            }

            this.writer = null;
            this.reader = null;
            this.stream = null;
            this.tcpClient = null;

            return;
        }

        //
        // FUNCTION : Dispose
        // DESCRIPTION : IDisposable implementation. Calls Disconnect and marks the object as disposed.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        public void Dispose()
        {
            if (this.isDisposed == false)
            {
                this.Disconnect();
                this.isDisposed = true;
            }

            return;
        }
    }
}
