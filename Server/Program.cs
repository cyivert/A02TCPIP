/*
* FILE : Program.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* This is the main entry point for the Word Game Server application. It initializes the server, reads configuration settings, validates game data files,
* and starts the server to listen for incoming client connections. The program also handles graceful shutdown when a termination signal is received (e.g., Ctrl+C) 
* and ensures that all resources are properly released before exiting.
*/

// NOTE: PLEASE PLEASE PLEASE check for ? as some are not initialied it and remember to remove it when
// the  variable and its functioanlity is implemented. I have added it to prevent compiler errors and to allow the code to compile successfully while still being a work in progress.
// - CY

using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using WordGameServer.Data;
using WordGameServer.Network;
using WordGameServer.Utils;

namespace WordGameServer
{
    //
    // CLASS : Program
    // DESCRIPTION : Program class serves as the main entry point for the Word Game Server application
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    class Program
    {
        // CancellationTokenSource to manage graceful shutdown of the server and its components.
        private static CancellationTokenSource? cancellationTokenSource;
        static async Task Main(string[] args)
        {
            Console.Title = "Word Game Server";
            Console.WriteLine("=================================");
            Console.WriteLine("   WORD GAME SERVER  ");
            Console.WriteLine("=================================");
            Console.WriteLine();

            // Initialize variables for server, logger, and configuration values.
            GameServer? server = null;
            Logger? logger = null;
            IPAddress? serverIP = null;
            int serverPort = 0;
            string? ipValue = string.Empty;
            string? portValue = string.Empty;

            // Initialize the cancellation token source for managing shutdown signals.
            cancellationTokenSource = new CancellationTokenSource();

            // Main execution block wrapped in a try-catch to handle any unexpected exceptions and ensure proper logging and resource cleanup.
            try
            {
                logger = new Logger();

                // Set up graceful shutdown handler
                Console.CancelKeyPress += OnCancelKeyPress;

                // Read IP from App.config
                ipValue = ConfigurationManager.AppSettings[ConfigKeys.ServerIP];
                serverIP = ParseIPAddress(ipValue, logger);

                // Read port from App.config
                portValue = ConfigurationManager.AppSettings[ConfigKeys.ServerPort];
                if (!int.TryParse(portValue, out serverPort) || serverPort <= 0 || serverPort > 65535)
                {
                    serverPort = GameConstants.DefaultServerPort;
                    logger.LogWarning($"Invalid port in config, using default {GameConstants.DefaultServerPort}");
                }

                // Validate game data files before starting server
                logger.LogMessage("Validating game data file...");
                int validFileCount = GameDataLoader.ValidateAndLoadFiles(logger);

                if (validFileCount == 0)
                {
                    logger.LogError("No valid game file found. Server cannot start.");
                    logger.LogError("Ensure the game data file (configured in App.config as 'GameDataFile') exists alongside the executable.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                logger.LogMessage($"Successfully loaded {validFileCount} valid game file(s)");
                Console.WriteLine();

                // Initialize and start the server
                server = new GameServer(serverIP, serverPort, logger);
                await server.StartAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                if (logger != null)
                {
                    logger.LogMessage("Server shutdown completed");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogError($"Fatal error: {ex.Message}");
                    logger.LogError($"Stack trace: {ex.StackTrace}");
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                Console.CancelKeyPress -= OnCancelKeyPress;

                if (server != null)
                {
                    await server.StopAsync();
                }

                logger?.Dispose();
                cancellationTokenSource?.Dispose();
            }

            return;
        }

        //
        // METHOD : ParseIPAddress
        // DESCRIPTION :
        // This method takes a string representation of an IP address and attempts to parse it into an IPAddress object.
        // It handles various special cases such as "ANY", "LOCALHOST", and "IPV6ANY", as well as validating the format of the IP address.
        // If the input is invalid or empty, it defaults to using IPAddress.Any (
        // PARAMETERS : 
        // string ipValue - The string value representing the IP address to be parsed, typically read from the application configuration.
        // Logger logger - An instance of the Logger class used to log messages and warnings during the parsing process, providing feedback on the configuration and any issues encountered.
        // RETURNS : 
        // IPAddress - The parsed IPAddress object based on the input string, or IPAddress.Any if the input is invalid or empty, allowing the server to bind to all available network interfaces.
        //
        private static IPAddress ParseIPAddress(string? ipValue, Logger logger)
        {
            IPAddress? parsedIP = null;

            // Handle empty or null
            if (string.IsNullOrWhiteSpace(ipValue))
            {
                logger.LogWarning("No IP configured, using 0.0.0.0 (all interfaces)");
                parsedIP = IPAddress.Any;
                return parsedIP;
            }

            // Handle special values
            string normalizedValue = ipValue.Trim().ToUpper();
            if (normalizedValue == "ANY" || normalizedValue == "0.0.0.0")
            {
                logger.LogMessage("Configured to listen on all network interfaces (0.0.0.0)");
                parsedIP = IPAddress.Any;
                return parsedIP;
            }

            if (normalizedValue == "LOCALHOST" || normalizedValue == "127.0.0.1")
            {
                logger.LogMessage("Configured to listen on localhost only (127.0.0.1)");
                parsedIP = IPAddress.Loopback;
                return parsedIP;
            }

            if (normalizedValue == "::" || normalizedValue == "IPV6ANY")
            {
                logger.LogMessage("Configured to listen on all IPv6 interfaces (::)");
                parsedIP = IPAddress.IPv6Any;
                return parsedIP;
            }

            // Try to parse as IP address
            if (IPAddress.TryParse(ipValue, out parsedIP))
            {
                logger.LogMessage($"Configured to listen on specific address: {parsedIP}");
                return parsedIP;
            }

            // Invalid IP - use default
            logger.LogWarning($"Invalid IP address '{ipValue}' in config, using 0.0.0.0 (all interfaces)");
            parsedIP = IPAddress.Any;

            return parsedIP;
        }

        //
        // METHOD : OnCancelKeyPress
        // DESCRIPTION : This event handler is invoked when the user presses Ctrl+C in the console.
        // It signals the server to shut down gracefully by canceling the cancellation token, allowing any ongoing operations to complete before the application exits.
        // PARAMETERS : 
        // object sender - The source of the event, typically the console.
        // ConsoleCancelEventArgs eventArgs - Contains information about the cancel key press event, including a Cancel property that
        // can be set to true to prevent the default behavior of terminating the application immediately.
        // RETURNS : n/a
        //
        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            Console.WriteLine("\nShutdown signal received. Stopping server gracefully...");
            cancellationTokenSource?.Cancel();

            return;
        }
    }
}
