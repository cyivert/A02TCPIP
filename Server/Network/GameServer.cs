/*
* FILE : GameServer.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* TCP server with configurable IP binding and CancellationToken added for graceful shutdown. Manages active client tasks and logs important events and errors.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WordGameServer.Utils;

namespace WordGameServer.Network
{

    //
    // CLASS : GameServer
    // DESCRIPTION :
    // This class implements a TCP server that listens for incoming client connections on a specified IP address and port.
    // It manages active client tasks and ensures thread-safe operations when handling multiple clients concurrently.
    // The server uses a Logger instance to log important events and errors during its operation.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public class GameServer
    {
        private readonly IPAddress ipAddress;
        private readonly int port;
        private readonly Logger logger;
        private TcpListener? listener;
        private readonly List<Task> activeClientTasks;
        private readonly object clientTasksLock;
        private int activeConnections;

        //
        // CONSTRUCTOR : GameServer
        // DESCRIPTION :
        // Initializes a new instance of the GameServer class with the specified IP address, port, and Logger instance.
        // PARAMETERS : 
        // IPAddress ipAddress - The IP address that the server will bind to for listening to incoming client connections.
        // int port - The port number that the server will listen on for incoming client connections.
        // Logger logger - An instance of the Logger class used for logging important events and errors during the server's operation.
        // RETURNS : n/a
        //
        public GameServer(IPAddress ipAddress, int port, Logger logger)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.logger = logger;
            this.listener = null;
            this.activeClientTasks = new List<Task>();
            this.clientTasksLock = new object();
            this.activeConnections = 0;

            return;
        }

        //
        // FUNCTION TASK : StartAsync 
        // DESCRIPTION : Starts the TCP server and begins accepting client connections asynchronously. The server will continue to accept clients until the provided CancellationToken is triggered.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal the server to stop accepting new client connections and begin shutdown procedures.
        // RETURNS : 
        // Task - Represents the asynchronous operation of starting the server and accepting clients. The task completes when the server is stopped or an error occurs.
        //
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.listener = new TcpListener(this.ipAddress, this.port);

            try
            {
                this.listener.Start(GameConstants.MaxConcurrentClients);
                this.logger.LogMessage($"Server started on {this.ipAddress}:{this.port}");
                this.logger.LogMessage($"Maximum concurrent connections: {GameConstants.MaxConcurrentClients}");
                this.logger.LogMessage("Waiting for client connections...");
                this.logger.LogMessage("Press Ctrl+C to shutdown");
                this.logger.LogMessage("");

                // Register cancellation to stop the listener, which unblocks AcceptTcpClientAsync
                cancellationToken.Register(() =>
                {
                    try
                    {
                        this.listener?.Stop();
                    }
                    catch
                    {
                        this.logger.LogError("Error stopping listener during cancellation");
                    }
                });

                await this.AcceptClientsAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogMessage("Server shutdown initiated by cancellation token");
            }
            catch (SocketException sockEx)
            {
                this.logger.LogError($"Socket error: {sockEx.Message}");
                this.logger.LogError($"Error code: {sockEx.ErrorCode}");

                if (sockEx.ErrorCode == 10048) // Address already in use
                {
                    this.logger.LogError($"Port {this.port} is already in use. Please choose a different port.");
                }
                else if (sockEx.ErrorCode == 10049) // Cannot assign requested address
                {
                    this.logger.LogError($"Cannot bind to {this.ipAddress}. This IP may not be available on this machine.");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Server error: {ex.Message}");
                this.logger.LogError($"Stack trace: {ex.StackTrace}");
            }

            return;
        }

        //
        // FUNCTION TASK : AcceptClientsAsync
        // DESCRIPTION :
        // This private asynchronous method continuously accepts incoming client connections until the provided CancellationToken is triggered. For each accepted client, it increments the active connection count, logs the client's remote endpoint, and starts a new task to handle the client's communication.
        // The method also includes error handling for various exceptions that may occur during the accept process.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal the server to stop accepting new client connections and begin shutdown procedures.
        // RETURNS :
        // Task - Represents the asynchronous operation of accepting clients. The task completes when the server is stopped or an error occurs.
        //
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            Task<TcpClient>? acceptTask = null;
            TcpClient? client = null;
            DateTime lastWaitingLog = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    acceptTask = this.listener!.AcceptTcpClientAsync();

                    // Wait for a client with periodic "waiting" log every 30 seconds
                    while (!acceptTask.IsCompleted)
                    {
                        Task completedTask = await Task.WhenAny(acceptTask, Task.Delay(30000, cancellationToken));

                        if (completedTask != acceptTask)
                        {
                            // Timeout elapsed, log waiting message if no connections are active
                            if (this.activeConnections == 0)
                            {
                                this.logger.LogMessage("Waiting for client connections...");
                            }
                        }
                    }

                    client = await acceptTask;

                    Interlocked.Increment(ref this.activeConnections);

                    string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                    this.logger.LogMessage($"Client connected from {clientEndpoint}. Active connections: {this.activeConnections}");

                    // Handle client in a separate task
                    Task clientTask = Task.Run(async () => await this.HandleClientAsync(client, cancellationToken), cancellationToken);

                    // Track active client tasks for graceful shutdown
                    lock (this.clientTasksLock)
                    {
                        this.activeClientTasks.Add(clientTask);
                    }

                    // Clean up completed tasks periodically
                    this.CleanupCompletedTasks();
                }
                catch (OperationCanceledException)
                {
                    this.logger.LogMessage("Accept loop cancelled");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    this.logger.LogMessage("Listener disposed during accept");
                    break;
                }
                catch (SocketException sockEx)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.logger.LogError($"Socket error accepting client: {sockEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.logger.LogError($"Error accepting client: {ex.Message}");
                    }
                }
            }

            return;
        }

        //
        // FUNCTION TASK : HandleClientAsync
        // DESCRIPTION :
        // This private asynchronous method handles communication with a connected client.
        // It takes a TcpClient object representing the client connection and a CancellationToken for managing shutdown.
        // The method includes error handling for various exceptions that may occur during client communication and ensures that the client connection is properly closed when finished.
        // PARAMETERS : 
        // TcpClient client - The TcpClient object representing the connected client that will be handled by this method.
        // CancellationToken cancellationToken - A token used to signal the server to stop handling client communication and begin shutdown procedures.
        // RETURNS : 
        // Task - Represents the asynchronous operation of handling the client. The task completes when the client disconnects or an error occurs.
        //
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            string clientEndpoint = string.Empty;

            try
            {
                clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                ClientHandler handler = new ClientHandler(client, this.logger);
                await handler.ProcessClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogMessage($"Client {clientEndpoint} disconnected due to shutdown");
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Client handler error for {clientEndpoint}: {ex.Message}");
            }
            finally
            {
                try
                {
                    client?.Close();
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Error closing client connection: {ex.Message}");
                }

                int currentConnections = Interlocked.Decrement(ref this.activeConnections);
                this.logger.LogMessage($"Client {clientEndpoint} disconnected. Active connections: {currentConnections}");
            }

            return;
        }

        //
        // FUNCTION : CleanupCompletedTasks
        // DESCRIPTION :
        // This private method is responsible for cleaning up completed client tasks from the activeClientTasks list.
        // It locks the clientTasksLock to ensure thread-safe access to the list and removes any tasks that have completed their execution.
        // This helps to prevent memory leaks and keeps the list of active client tasks manageable.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        private void CleanupCompletedTasks()
        {
            lock (this.clientTasksLock)
            {
                this.activeClientTasks.RemoveAll(t => t.IsCompleted);
            }

            return;
        }

        public async Task StopAsync()
        {
            this.logger.LogMessage("Stopping server...");

            try
            {
                // Stop accepting new connections
                this.listener?.Stop();
                this.logger.LogMessage("Stopped accepting new connections");

                // Wait for active clients to finish (with timeout)
                Task[]? clientTasksArray = null;
                lock (this.clientTasksLock)
                {
                    clientTasksArray = this.activeClientTasks.ToArray();
                }

                if (clientTasksArray.Length > 0)
                {
                    this.logger.LogMessage($"Waiting for {clientTasksArray.Length} active client(s) to disconnect...");

                    Task allClientsTask = Task.WhenAll(clientTasksArray);
                    Task timeoutTask = Task.Delay(5000); // 5 second timeout

                    Task completedTask = await Task.WhenAny(allClientsTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        this.logger.LogWarning("Timeout waiting for clients. Forcing shutdown.");
                    }
                    else
                    {
                        this.logger.LogMessage("All clients disconnected cleanly");
                    }
                }

                this.logger.LogMessage("Server stopped successfully");
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error during server stop: {ex.Message}");
            }
            finally
            {
                this.logger.SignalShutdown();
            }

            return;
        }
    }
}
