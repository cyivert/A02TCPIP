/*
* FILE            : TcpRequestResponseClient.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Async TCP client that performs a strict disconnected request/response:
*   connect -> send 1 line -> read 1 line -> close.
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
    public sealed class TcpRequestResponseClient
    {
        public TcpRequestResponseClient()
        {
            return;
        }

        public async Task<NetworkResult> SendOnceAsync(ClientSettings settings, string requestLine, CancellationToken cancellationToken)
        {
            bool isSuccess = false;
            string responseLine = string.Empty;
            string errorMessage = string.Empty;

            TcpClient? tcpClient = null;
            NetworkStream? stream = null;
            StreamReader? reader = null;
            StreamWriter? writer = null;

            Task? connectTask = null;
            Task? connectTimeoutTask = null;

            try
            {
                tcpClient = new TcpClient();

                connectTask = tcpClient.ConnectAsync(settings.ServerIp, settings.ServerPort);
                connectTimeoutTask = Task.Delay(settings.ConnectTimeoutMs, cancellationToken);

                Task completedConnect = await Task.WhenAny(connectTask, connectTimeoutTask);

                if (completedConnect == connectTimeoutTask)
                {
                    errorMessage = "Connect timed out.";
                }
                else
                {
                    stream = tcpClient.GetStream();

                    reader = new StreamReader(stream, Encoding.UTF8);
                    writer = new StreamWriter(stream, Encoding.UTF8);
                    writer.AutoFlush = true;

                    await writer.WriteLineAsync(requestLine);

                    Task<string?> readTask = reader.ReadLineAsync();
                    Task ioTimeoutTask = Task.Delay(settings.IoTimeoutMs, cancellationToken);

                    Task completedRead = await Task.WhenAny(readTask, ioTimeoutTask);

                    if (completedRead == ioTimeoutTask)
                    {
                        errorMessage = "Read timed out.";
                    }
                    else
                    {
                        string? line = await readTask;

                        if (string.IsNullOrWhiteSpace(line) == true)
                        {
                            errorMessage = "No response received from server.";
                        }
                        else
                        {
                            responseLine = line.Trim();
                            isSuccess = true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }

                if (reader != null)
                {
                    reader.Dispose();
                }

                if (stream != null)
                {
                    stream.Dispose();
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
            }

            NetworkResult result = new NetworkResult(isSuccess, responseLine, errorMessage);

            return (result);
        }
    }
}
