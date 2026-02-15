/*
* FILE            : WordGameTcpClient.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Async TCP client wrapper that supports a strict request/response pattern.
*   Connects to server, sends a single-line request, reads a single-line response.
*/

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WordGameClient.Network
{
    public sealed class WordGameTcpClient : IDisposable
    {
        private TcpClient? tcpClient;
        private NetworkStream? stream;
        private StreamReader? reader;
        private StreamWriter? writer;

        public bool IsConnected
        {
            get
            {
                bool connected = false;

                if (this.tcpClient != null)
                {
                    connected = this.tcpClient.Connected;
                }

                return (connected);
            }
        }

        public WordGameTcpClient()
        {
            this.tcpClient = null;
            this.stream = null;
            this.reader = null;
            this.writer = null;

            return;
        }

        public async Task<NetworkResult> ConnectAsync(string serverIp, int serverPort, int connectTimeoutMs, int ioTimeoutMs, CancellationToken cancellationToken)
        {
            bool isSuccess = false;
            string message = string.Empty;
            string errorMessage = string.Empty;

            Task? connectTask = null;
            Task? timeoutTask = null;

            try
            {
                this.tcpClient = new TcpClient();

                connectTask = this.tcpClient.ConnectAsync(serverIp, serverPort);
                timeoutTask = Task.Delay(connectTimeoutMs, cancellationToken);

                Task completed = await Task.WhenAny(connectTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    errorMessage = "Connect timed out.";
                }
                else
                {
                    this.stream = this.tcpClient.GetStream();
                    this.reader = new StreamReader(this.stream, Encoding.UTF8);
                    this.writer = new StreamWriter(this.stream, Encoding.UTF8);
                    this.writer.AutoFlush = true;

                    // Optional: read server welcome line (if server sends one)
                    string? welcome = await this.ReadLineWithTimeoutAsync(ioTimeoutMs, cancellationToken);
                    if (string.IsNullOrWhiteSpace(welcome) == false)
                    {
                        message = welcome.Trim();
                    }

                    isSuccess = true;
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
            }

            NetworkResult result = new NetworkResult(isSuccess, message, errorMessage);

            return (result);
        }

        public async Task<NetworkResult> SendRequestAsync(string requestLine, int ioTimeoutMs, CancellationToken cancellationToken)
        {
            bool isSuccess = false;
            string message = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                if ((this.writer == null) || (this.reader == null))
                {
                    errorMessage = "Not connected to server.";
                }
                else
                {
                    await this.writer.WriteLineAsync(requestLine);

                    string? response = await this.ReadLineWithTimeoutAsync(ioTimeoutMs, cancellationToken);

                    if (string.IsNullOrWhiteSpace(response) == true)
                    {
                        errorMessage = "No response received from server.";
                    }
                    else
                    {
                        message = response.Trim();
                        isSuccess = true;
                    }
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
            }

            NetworkResult result = new NetworkResult(isSuccess, message, errorMessage);

            return (result);
        }

        private async Task<string?> ReadLineWithTimeoutAsync(int ioTimeoutMs, CancellationToken cancellationToken)
        {
            string? line = null;
            Task<string?>? readTask = null;
            Task? timeoutTask = null;

            if (this.reader != null)
            {
                readTask = this.reader.ReadLineAsync();
                timeoutTask = Task.Delay(ioTimeoutMs, cancellationToken);

                Task completed = await Task.WhenAny(readTask, timeoutTask);

                if (completed == readTask)
                {
                    line = await readTask;
                }
            }

            return (line);
        }

        public void Disconnect()
        {
            try
            {
                this.writer?.Close();
                this.reader?.Close();
                this.stream?.Close();
                this.tcpClient?.Close();
            }
            catch
            {
                // swallow in disconnect
            }

            this.writer = null;
            this.reader = null;
            this.stream = null;
            this.tcpClient = null;

            return;
        }

        public void Dispose()
        {
            this.Disconnect();

            return;
        }
    }

    public sealed class NetworkResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public string ErrorMessage { get; }

        public NetworkResult(bool isSuccess, string message, string errorMessage)
        {
            this.IsSuccess = isSuccess;
            this.Message = message;
            this.ErrorMessage = errorMessage;

            return;
        }
    }
}
