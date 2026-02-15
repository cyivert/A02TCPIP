/*
* FILE : GameServer.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* The functions in this file are used to ...
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Network
{
    /// <summary>
    /// TCP server that manages word game sessions for multiple clients
    /// </summary>
    public class GameServer
    {
        private readonly int port;
        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private readonly object consoleLock;
        private int activeConnections;

        /// <summary>
        /// Initializes a new instance of the GameServer class
        /// </summary>
        /// <param name="port">Port number to listen on</param>
        public GameServer(int port)
        {
            this.port = port;
            listener = null;
            cancellationTokenSource = null;
            consoleLock = new object();
            activeConnections = 0;

            return;
        }

        /// <summary>
        /// Starts the server and begins listening for client connections
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task StartAsync()
        {
            IPAddress ipAddress = IPAddress.Any;
            listener = new TcpListener(ipAddress, port);
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                listener.Start();
                LogMessage($"Server started on port {port}");
                LogMessage("Waiting for client connections...");
                LogMessage("Press Ctrl+C to shutdown");
                LogMessage("");

                Console.CancelKeyPress += OnCancelKeyPress;

                await AcceptClientsAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                LogMessage("Server shutdown initiated");
            }
            catch (Exception ex)
            {
                LogMessage($"Server error: {ex.Message}");
            }
            finally
            {
                Console.CancelKeyPress -= OnCancelKeyPress;
            }

            return;
        }

        /// <summary>
        /// Handles console cancel event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="eventArgs">Event arguments</param>
        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            cancellationTokenSource?.Cancel();

            return;
        }

        /// <summary>
        /// Continuously accepts incoming client connections
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            Task acceptTask = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    acceptTask = listener.AcceptTcpClientAsync();
                    TcpClient client = await acceptTask;

                    Interlocked.Increment(ref activeConnections);
                    LogMessage($"Client connected. Active connections: {activeConnections}");

                    Task clientTask = HandleClientAsync(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogMessage($"Error accepting client: {ex.Message}");
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Handles communication with a single client
        /// </summary>
        /// <param name="client">TcpClient representing the connected client</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task HandleClientAsync(TcpClient client)
        {
            ClientHandler handler = null;

            try
            {
                handler = new ClientHandler(client, this);
                await handler.ProcessClientAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"Client handler error: {ex.Message}");
            }
            finally
            {
                client?.Close();
                Interlocked.Decrement(ref activeConnections);
                LogMessage($"Client disconnected. Active connections: {activeConnections}");
            }

            return;
        }

        /// <summary>
        /// Logs a message to the console in a thread-safe manner
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogMessage(string message)
        {
            lock (consoleLock)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{timestamp}] {message}");
            }

            return;
        }

        /// <summary>
        /// Stops the server and releases resources
        /// </summary>
        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            listener?.Stop();
            LogMessage("Server stopped");

            return;
        }
    }
}
