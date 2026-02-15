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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Utils
{

    //
    // CLASS : Logger
    // DESCRIPTION : This class provides a thread-safe logging utility for console and file output with timestamps.
    // It includes methods for logging regular messages, warnings, errors, and debug information, each with appropriate formatting and color coding to differentiate message types.
    // The logger also supports a shutdown signal to prevent new log messages from being processed after shutdown has been initiated.
    // All log messages are written to both the console and a log.txt file.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public class Logger : IDisposable
    {
        private const string LogFilePath = "log.txt";                       // Path to the log file where all log messages are written.
        private readonly object consoleLock;                                // Lock object to synchronize console access and ensure thread safety when logging messages.
        private readonly StreamWriter? fileWriter;                          // StreamWriter for writing log messages to the log file.
        private volatile bool isShuttingDown;                               // Flag to indicate whether the logger is shutting down. This prevents new log messages from being processed after shutdown has been initiated.
        private bool isDisposed;                                            // Flag to indicate whether the logger has been disposed to prevent double disposal.

        //
        // CONSTRUCTOR : Logger
        // DESCRIPTION : This constructor initializes the Logger instance, sets up necessary synchronization primitives, and opens the log file for writing.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public Logger()
        {
            this.consoleLock = new object();
            this.isShuttingDown = false;
            this.isDisposed = false;

            try
            {
                this.fileWriter = new StreamWriter(LogFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: Failed to open log file: {ex.Message}");
                this.fileWriter = null;
            }

            return;
        }

        //
        // METHOD : SignalShutdown
        // DESCRIPTION :
        // Signals the logger that the application is shutting down, preventing further log messages from being processed.
        // This method should be called during application shutdown to ensure that no new log messages are written to the console after shutdown has been initiated.
        // It also disposes of the logger resources to ensure all buffered data is flushed to disk.
        // PARAMETERS : 
        // RETURNS :
        //
        public void SignalShutdown()
        {
            this.isShuttingDown = true;
            this.Dispose();

            return;
        }

        //
        // METHOD : Dispose
        // DESCRIPTION : Releases all resources used by the Logger instance, including closing and flushing the log file writer.
        // This method ensures that all buffered log data is written to disk before the file handle is released.
        // It is safe to call this method multiple times; subsequent calls after the first will have no effect.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            try
            {
                lock (this.consoleLock)
                {
                    this.fileWriter?.Flush();
                    this.fileWriter?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: Failed to dispose log file: {ex.Message}");
            }

            this.isDisposed = true;

            return;
        }

        //
        // METHOD : WriteToFile
        // DESCRIPTION : Writes a formatted log message to the log file. This method is called internally by the logging methods to persist log messages to disk.
        // PARAMETERS :
        // string formattedMessage - The fully formatted log message including timestamp and level prefix to be written to the log file.
        // RETURNS : n/a
        //
        private void WriteToFile(string formattedMessage)
        {
            if (this.isDisposed)
            {
                return;
            }

            try
            {
                this.fileWriter?.WriteLine(formattedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: Failed to write to log file: {ex.Message}");
            }

            return;
        }

        //
        // METHOD : LogMessage
        // DESCRIPTION : Logs a message to the console and log file with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
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
                    this.WriteToFile(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // METHOD : LogError
        // DESCRIPTION :
        // Logs an error message to the console and log file with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
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
                    this.WriteToFile(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // METHOD : LogWarning
        // DESCRIPTION : Logs a warning message to the console and log file with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
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
                    this.WriteToFile(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger error: {ex.Message}");
            }

            return;
        }

        //
        // METHOD : LogDebug
        // DESCRIPTION : Logs a debug message to the console and log file with a timestamp. This method is thread-safe and will not log messages if the logger has been signaled to shut down.
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
                    this.WriteToFile(formattedMessage);
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