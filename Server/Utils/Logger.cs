/*
* FILE : Logger.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Thread-safe logging utility for console output with timestamps.
*/

// REFERENCE //
/* 
 * Microsoft. (n/a). Console.ForegroundColor property. Microsoft Learn. https://learn.microsoft.com/en-us/dotnet/api/system.console.foregroundcolor
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Utils
{
    public class Logger
    {
        private readonly object consoleLock;                                // Lock object to synchronize console access and ensure thread safety when logging messages.
        private volatile bool isShuttingDown;                               // Flag to indicate whether the logger is shutting down. This prevents new log messages from being processed after shutdown has been initiated.

        //
        // FUNCTION : Logger
        // DESCRIPTION : This constructor initializes the Logger instance and sets up necessary synchronization primitives.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public Logger()
        {
            this.consoleLock = new object();
            this.isShuttingDown = false;

            return;
        }

        //
        // FUNCTION : SignalShutdown
        // DESCRIPTION :
        // Signals the logger that the application is shutting down, preventing further log messages from being processed.
        // This method should be called during application shutdown to ensure that no new log messages are written to the console after shutdown has been initiated.
        // PARAMETERS : 
        // RETURNS :
        //
        public void SignalShutdown()
        {
            this.isShuttingDown = true;

            return;
        }

        //
        // FUNCTION : LogMessage
        // DESCRIPTION : Logs a message to the console with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
        // PARAMETERS : 
        // string message - The message will be prefixed with a timestamp in the format [yyyy-MM-dd HH:mm:ss].
        // RETURNS : n/a
        //
        public void LogMessage(string message)
        {
            string timestamp = string.Empty;
            string formattedMessage = string.Empty;

            if (this.isShuttingDown)
            {
                return;
            }

            try
            {
                lock (this.consoleLock)
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    formattedMessage = $"[{timestamp}] {message}";
                    Console.WriteLine(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // FUNCTION : LogError
        // DESCRIPTION :
        // Logs an error message to the console with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
        // Error messages are displayed in red text to differentiate them from regular log messages.
        // PARAMETERS : 
        // string message - Error messages typically indicate issues or problems that have occurred within the application and may require attention or troubleshooting.
        // RETURNS :
        //
        public void LogError(string message)
        {
            string timestamp = string.Empty;
            string formattedMessage = string.Empty;
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                lock (this.consoleLock)
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    formattedMessage = $"[{timestamp}] ERROR: {message}";

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(formattedMessage);
                    Console.ForegroundColor = originalColor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // FUNCTION : LogWarning
        // DESCRIPTION : Logs a warning message to the console with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
        // PARAMETERS : 
        // string message - Warning messages typically indicate potential issues or situations that may require attention but do not necessarily indicate an error or problem.
        // RETURNS :
        //
        public void LogWarning(string message)
        {
            string timestamp = string.Empty;
            string formattedMessage = string.Empty;
            ConsoleColor originalColor = Console.ForegroundColor;

            if (this.isShuttingDown)
            {
                return;
            }

            try
            {
                lock (this.consoleLock)
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    formattedMessage = $"[{timestamp}] WARNING: {message}";

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(formattedMessage);
                    Console.ForegroundColor = originalColor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // FUNCTION : LogDebug
        // DESCRIPTION : Logs a debug message to the console with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
        // PARAMETERS : 
        // string message - Debug messages are typically used for development and troubleshooting purposes and may include detailed information about the application's state or behavior.
        // RETURNS :
        //
        public void LogDebug(string message)
        {
            string timestamp = string.Empty;
            string formattedMessage = string.Empty;
            ConsoleColor originalColor = Console.ForegroundColor;

            if (this.isShuttingDown)
            {
                return;
            }

            try
            {
                lock (this.consoleLock)
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    formattedMessage = $"[{timestamp}] DEBUG: {message}";

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(formattedMessage);
                    Console.ForegroundColor = originalColor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }
    }
}