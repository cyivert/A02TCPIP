/*
* FILE : GameServer.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* TCP server with configurable IP binding and CancellationToken support.
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
        private TcpListener listener;
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
    }
}
