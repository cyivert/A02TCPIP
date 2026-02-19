/*
* FILE : ClientHandler.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Handles communication with a single client with strict request/response pattern.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WordGameServer.Data;
using WordGameServer.Game;
using WordGameServer.Utils;

namespace WordGameServer.Network
{
    //
    // CLASS : ClientHandler
    // DESCRIPTION :
    // This class is responsible for managing the communication with a single client.
    // It processes incoming requests, manages the game state for that client, and sends appropriate responses back to the client.
    // The class ensures a strict request/response pattern and handles various commands such as starting a game, making guesses, checking progress, and quitting.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public class ClientHandler
    {
        private readonly TcpClient client;
        private readonly Logger logger;
        private NetworkStream? stream;
        private StreamReader? reader;
        private StreamWriter? writer;
        private GameSession? currentGame;
        private string playerName;
        private string playerEmail;

        //
        // CONSTRUCTOR : ClientHandler
        // DESCRIPTION :
        // This constructor initializes a new instance of the ClientHandler class with the specified TcpClient and Logger.
        // It sets up the necessary fields for managing client communication and game state.
        // PARAMETERS : 
        // TcpClient client - The TcpClient instance representing the connection to the client. This is used for reading and writing data to the client.
        // Logger logger - An instance of the Logger class used for logging messages, warnings, and errors related to client communication and game processing.
        // RETURNS : n/a
        //
        public ClientHandler(TcpClient client, Logger logger)
        {
            this.client = client;           // Store the TcpClient instance for communication with the client
            this.logger = logger;           // Store the Logger instance for logging purposes
            this.stream = null;             // Initialize the NetworkStream to null; it will be set when the client connection is established
            this.reader = null;             // Initialize the StreamReader to null; it will be set when the client connection is established
            this.writer = null;             // Initialize the StreamWriter to null; it will be set when the client connection is established
            this.currentGame = null;        // Initialize the current game session to null; it will be set when a game is started by the client
            this.playerName = "Unknown";    // Default name until START command is received
            this.playerEmail = "N/A";       // Default email until START command is received

            return;
        }

        //
        // METHOD : ProcessClientAsync
        // DESCRIPTION :
        // This asynchronous method manages the communication with the client.
        // It implements a strict request/response pattern, where it waits for a request from the client, processes it, and sends back a response before waiting for the next request.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal cancellation of the client processing. This allows for graceful shutdown of the client handler when requested.
        // RETURNS : 
        // Task - Represents the asynchronous operation of processing the client. The method completes when the client disconnects or when a quit command is received.
        //
        public async Task ProcessClientAsync(CancellationToken cancellationToken)
        {
            bool continueProcessing = true;
            string clientEndpoint = string.Empty;

            try
            {
                clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                // Initialize network stream and readers/writers
                this.stream = this.client.GetStream();
                this.reader = new StreamReader(this.stream, Encoding.UTF8);
                this.writer = new StreamWriter(this.stream, Encoding.UTF8) { AutoFlush = true }; // auto-flush after each write
                                                                                                 // (means: it will automatically send the data to the client without needing to call Flush() explicitly)

                // Send welcome message (handshake)
                await this.SendResponseAsync(GameConstants.WelcomeMessage, cancellationToken);

                // Process client requests with strict request/response pattern
                while (continueProcessing && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for request with timeout
                        Task<string?> readTask = this.reader.ReadLineAsync();
                        Task delayTask = Task.Delay(GameConstants.ClientReadTimeoutMs, cancellationToken);

                        Task completedTask = await Task.WhenAny(readTask, delayTask);

                        if (completedTask == delayTask)
                        {
                            this.logger.LogWarning($"Client {clientEndpoint} read timeout");
                            await this.SendResponseAsync($"{ProtocolCommands.Error}|Read timeout", cancellationToken);
                            continueProcessing = false;
                        }
                        else
                        {
                            string? request = await readTask;

                            if (string.IsNullOrEmpty(request))
                            {
                                this.logger.LogMessage($"Client {clientEndpoint} sent empty request - disconnecting");
                                continueProcessing = false;
                            }
                            else
                            {
                                // Process request and send response (strict pattern)
                                continueProcessing = await this.ProcessRequestAsync(request, cancellationToken);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        this.logger.LogMessage($"Client {clientEndpoint} processing cancelled");
                        continueProcessing = false;
                    }
                }
            }
            catch (IOException ioException)
            {
                this.logger.LogWarning($"Client {clientEndpoint} I/O error: {ioException.Message}");
            }
            catch (Exception exception)
            {
                this.logger.LogError($"Client {clientEndpoint} processing error: {exception.Message}");
                this.logger.LogError($"Stack trace: {exception.StackTrace}");
            }
            finally
            {
                // Log performance summary if game was active when client disconnected
                if (this.currentGame != null && this.currentGame.Status == GameStatus.Active)
                {
                    this.currentGame.Status = GameStatus.Ended;
                    this.LogPerformanceSummary("CLIENT DISCONNECTED");
                }

                try
                {
                    this.writer?.Close();
                    this.reader?.Close();
                    this.stream?.Close();
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Error cleaning up client resources: {ex.Message}");
                }
            }

            return;
        }

        //
        // METHOD : ProcessRequestAsync
        // DESCRIPTION : This asynchronously method processes a single request from the client and sends back an appropriate response based on the command received.
        // PARAMETERS : 
        // string request - The raw request string received from the client. This string is expected to follow a specific format based on the defined protocol commands.
        // CancellationToken cancellationToken - A token used to signal cancellation of the request processing. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task<bool> - RThe method returns a boolean value indicating whether to continue processing further requests (true) or to stop (false), such as when a quit command is received or when an error occurs.
        //
        private async Task<bool> ProcessRequestAsync(string request, CancellationToken cancellationToken)
        {
            bool continueProcessing = true;
            string[]? messageParts = null;
            string command = string.Empty;
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            this.logger.LogDebug($"[{clientEndpoint}] Received: {request}");

            try
            {
                messageParts = request.Split(GameConstants.MessageDelimiter);
                command = messageParts[0].ToUpper();

                switch (command)
                {
                    case ProtocolCommands.Start:
                        string startPlayerName = (messageParts.Length >= 2) ? messageParts[1].Trim() : "Unknown";
                        string startPlayerEmail = (messageParts.Length >= 3) ? messageParts[2].Trim() : "N/A";
                        this.playerName = startPlayerName;
                        this.playerEmail = startPlayerEmail;
                        await this.HandleStartGameAsync(startPlayerName, startPlayerEmail, cancellationToken);
                        break;

                    case ProtocolCommands.Guess:
                        if (messageParts.Length >= 2)
                        {
                            await this.HandleGuessAsync(messageParts[1], cancellationToken);
                        }
                        else
                        {
                            await this.SendResponseAsync($"{ProtocolCommands.Error}|Invalid GUESS format - missing word", cancellationToken);
                        }
                        break;

                    case ProtocolCommands.Quit:
                        await this.HandleQuitAsync(cancellationToken);
                        continueProcessing = false;
                        break;

                    case ProtocolCommands.EndGame:
                        await this.HandleEndGameAsync(cancellationToken);
                        break;

                    case ProtocolCommands.Progress:
                        await this.HandleProgressAsync(cancellationToken);
                        break;

                    default:
                        await this.SendResponseAsync($"{ProtocolCommands.Error}|Unknown command: {command}", cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.LogMessage($"[{clientEndpoint}] Request processing cancelled");
                continueProcessing = false;
            }
            catch (Exception exception)
            {
                this.logger.LogError($"[{clientEndpoint}] Error processing command '{command}': {exception.Message}");

                try
                {
                    await this.SendResponseAsync($"{ProtocolCommands.Error}|{exception.Message}", cancellationToken);
                }
                catch (Exception sendEx)
                {
                    this.logger.LogError($"Failed to send error response: {sendEx.Message}");
                }
            }

            return continueProcessing;
        }

        //
        // METHOD : HandleStartGameAsync
        // DESCRIPTION :
        // This asynchronous method handles the logic for starting a new game session for the client.
        // It loads a random game configuration, initializes a new GameSession instance, and sends the appropriate response back to the client with the game details.
        // If any errors occur during this process, it logs the error and sends an error response to the client.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal cancellation of the game start process. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task - Represents the asynchronous operation of starting a game. The method completes when the game has been initialized and the response has been sent to the client.
        //
        private async Task HandleStartGameAsync(string playerName, string playerEmail, CancellationToken cancellationToken)
        {
            GameData? gameData = null;
            string response = string.Empty;
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            try
            {
                gameData = GameDataLoader.LoadRandomGame();
                this.currentGame = new GameSession(gameData);

                response = $"{ProtocolCommands.GameStart}|{gameData.PuzzleString}|{gameData.WordCount}|{GameSession.GameDurationSeconds}|{GameSession.MaxGuesses}";
                await this.SendResponseAsync(response, cancellationToken);

                this.logger.LogMessage($"[{clientEndpoint}] Player '{playerName}' ({playerEmail}) started game - Puzzle: {gameData.PuzzleString}, Words: {gameData.WordCount}, Duration: {GameSession.GameDurationSeconds}s, MaxTries: {GameSession.MaxGuesses}");
            }
            catch (Exception exception)
            {
                this.logger.LogError($"[{clientEndpoint}] Failed to start game: {exception.Message}");
                await this.SendResponseAsync($"{ProtocolCommands.Error}|Failed to start game: {exception.Message}", cancellationToken);
            }

            return;
        }

        //
        // METHOD : HandleGuessAsync
        // DESCRIPTION :
        // This asynchronous method processes a guess from the client.
        // It checks if there is an active game session, validates the guess, updates the game state accordingly, and sends back a response
        // indicating whether the guess was correct, already found, not found, or if the time has expired.
        // PARAMETERS : 
        // string word - The word guessed by the client. This is expected to be a single word that the client is trying to find in the puzzle.
        // CancellationToken cancellationToken - A token used to signal cancellation of the guess processing. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task - Represents the asynchronous operation of processing the guess. The method completes when the guess has been evaluated and the response has been sent to the client.
        //
        private async Task HandleGuessAsync(string word, CancellationToken cancellationToken)
        {
            string response = string.Empty;
            GuessResult guessResult = GuessResult.InvalidGame;
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            if (this.currentGame == null)
            {
                await this.SendResponseAsync($"{ProtocolCommands.Error}|No active game - use START command first", cancellationToken);
                return;
            }

            if (this.currentGame.Status != GameStatus.Active)
            {
                await this.SendResponseAsync($"{ProtocolCommands.GameOver}|Game is not active", cancellationToken);
                return;
            }

            if (this.currentGame.IsGameOver())
            {
                this.currentGame.Status = GameStatus.Lost;
                this.LogPerformanceSummary("TIME EXPIRED");
                await this.SendResponseAsync($"{ProtocolCommands.GameOver}|TIME EXPIRED", cancellationToken);
                return;
            }

            if (this.currentGame.IsOutOfTries())
            {
                this.currentGame.Status = GameStatus.Lost;
                this.LogPerformanceSummary("OUT OF TRIES");
                await this.SendResponseAsync($"{ProtocolCommands.GameOver}|OUT OF TRIES! Score: {this.currentGame.GetScore()}", cancellationToken);
                return;
            }

            guessResult = this.currentGame.ProcessGuess(word);

            switch (guessResult)
            {
                case GuessResult.Found:
                    if (this.currentGame.IsGameComplete())
                    {
                        this.currentGame.Status = GameStatus.Won;
                        response = $"{ProtocolCommands.GameOver}|ALL WORDS FOUND! Score: {this.currentGame.GetScore()}|Guesses: {this.currentGame.GuessCount}";
                        this.logger.LogMessage($"[{clientEndpoint}] Word found: {word} - All words found! Score: {this.currentGame.GetScore()}");
                        this.LogPerformanceSummary("ALL WORDS FOUND");
                    }
                    else
                    {
                        response = $"{ProtocolCommands.WordFound}|{word.ToUpper()}|{this.currentGame.GetFoundCount()}|{this.currentGame.GetTotalWords()}";
                        this.logger.LogMessage($"[{clientEndpoint}] Word found: {word} ({this.currentGame.GetFoundCount()}/{this.currentGame.GetTotalWords()})");
                    }
                    break;

                case GuessResult.AlreadyFound:
                    response = $"{ProtocolCommands.WordAlreadyExist}|{word.ToUpper()}";
                    this.logger.LogMessage($"[{clientEndpoint}] Word already found: {word}");
                    break;

                case GuessResult.NotFound:
                    response = $"{ProtocolCommands.WordDoesNotExist}|{word.ToUpper()}";
                    this.logger.LogMessage($"[{clientEndpoint}] Word not found: {word} (Wrong guess {this.currentGame.GuessCount}/{GameSession.MaxGuesses})");
                    break;

                case GuessResult.TimeExpired:
                    this.currentGame.Status = GameStatus.Lost;
                    response = $"{ProtocolCommands.GameOver}|TIME EXPIRED! Score: {this.currentGame.GetScore()}";
                    this.LogPerformanceSummary("TIME EXPIRED");
                    break;

                default:
                    response = $"{ProtocolCommands.Error}|Invalid guess";
                    break;
            }

            // Check if out of tries after this guess
            if ((this.currentGame.Status == GameStatus.Active) && this.currentGame.IsOutOfTries())
            {
                this.currentGame.Status = GameStatus.Lost;
                response = $"{ProtocolCommands.GameOver}|OUT OF TRIES! Score: {this.currentGame.GetScore()}";
                this.LogPerformanceSummary("OUT OF TRIES");
            }

            await this.SendResponseAsync(response, cancellationToken);

            return;
        }

        //
        // METHOD : HandleProgressAsync
        // DESCRIPTION : This asynchronous method handles the client's request for a progress update on the current game session.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal cancellation of the progress update process. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task - Represents the asynchronous operation of handling the progress request. The method completes when the progress information has been gathered and the response has been sent to the client.
        //
        private async Task HandleProgressAsync(CancellationToken cancellationToken)
        {
            string response = string.Empty;
            int foundCount = 0;
            int totalWords = 0;
            int remainingSeconds = 0;
            int guessCount = 0;
            int score = 0;
            int maxGuesses = 0;

            if (this.currentGame == null)
            {
                await this.SendResponseAsync($"{ProtocolCommands.Error}|No active game", cancellationToken);
                return;
            }

            foundCount = this.currentGame.GetFoundCount();
            totalWords = this.currentGame.GetTotalWords();
            remainingSeconds = this.currentGame.GetRemainingSeconds();
            guessCount = this.currentGame.GuessCount;
            score = this.currentGame.GetScore();
            maxGuesses = GameSession.MaxGuesses;

            // Detect time expiry during progress poll and log performance summary
            if (remainingSeconds <= 0 && this.currentGame.Status == GameStatus.Active)
            {
                this.currentGame.Status = GameStatus.Lost;
                this.LogPerformanceSummary("TIME EXPIRED");
            }

            response = $"{ProtocolCommands.ProgressUpdate}|{foundCount}|{totalWords}|{remainingSeconds}|{guessCount}|{score}|{maxGuesses}";
            await this.SendResponseAsync(response, cancellationToken);

            string clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            this.logger.LogDebug($"[{clientEndpoint}] Progress: Found={foundCount}/{totalWords}, Time={remainingSeconds}s, WrongGuesses={guessCount}/{maxGuesses}, Score={score}");

            return;
        }

        //
        // METHOD : HandleEndGameAsync
        // DESCRIPTION :
        // This asynchronous method handles the client's request to end the current game session.
        // Checks if there is an active game, calculates the final score, updates the game status to ended, and sends a response back to the client with the final score.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal cancellation of the end game process. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task - Represents the asynchronous operation of handling the end game request. The method completes when the game has been ended and the response has been sent to the client.
        //
        private async Task HandleEndGameAsync(CancellationToken cancellationToken)
        {
            int finalScore = 0;
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            if (this.currentGame != null)
            {
                finalScore = this.currentGame.GetScore();
                this.LogPerformanceSummary("Game ended by client");
                this.currentGame.Status = GameStatus.Ended;
                this.currentGame = null;
                await this.SendResponseAsync($"{ProtocolCommands.GameEnded}|Score: {finalScore}", cancellationToken);
            }
            else
            {
                await this.SendResponseAsync($"{ProtocolCommands.Error}|No active game to end", cancellationToken);
            }

            return;
        }

        //
        // METHOD : HandleQuitAsync
        // DESCRIPTION :
        // This asynchronous method handles the client's request to quit the game and disconnect.
        // It sends a goodbye message to the client, logs the disconnection, and allows the client handler to clean up resources and stop processing further requests.
        // PARAMETERS : 
        // CancellationToken cancellationToken - A token used to signal cancellation of the quit process. This allows for graceful shutdown of the client handler when requested.
        // RETURNS :
        // Task - Represents the asynchronous operation of handling the quit request. The method completes when the goodbye message has been sent to the client and the disconnection has been logged.
        //
        private async Task HandleQuitAsync(CancellationToken cancellationToken)
        {
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            await this.SendResponseAsync(GameConstants.GoodbyeMessage, cancellationToken);
            this.logger.LogMessage($"[{clientEndpoint}] Client quit gracefully");

            return;
        }

        private void LogPerformanceSummary(string result)
        {
            string clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            if (this.currentGame != null)
            {
                int foundCount = this.currentGame.GetFoundCount();
                int totalWords = this.currentGame.GetTotalWords();
                int guessCount = this.currentGame.GuessCount;
                int remainingSeconds = this.currentGame.GetRemainingSeconds();
                int score = this.currentGame.GetScore();

                this.logger.LogMessage($"");
                this.logger.LogMessage($"========== PERFORMANCE SUMMARY ==========");
                this.logger.LogMessage($"  Client:         {clientEndpoint}");
                this.logger.LogMessage($"  Player Name:    {this.playerName}");
                this.logger.LogMessage($"  Player Email:   {this.playerEmail}");
                this.logger.LogMessage($"  Words Found:    {foundCount} / {totalWords}");
                this.logger.LogMessage($"  Wrong Guesses:  {guessCount} / {GameSession.MaxGuesses}");
                this.logger.LogMessage($"  Time Remaining: {remainingSeconds}s");
                this.logger.LogMessage($"  Final Score:    {score}");
                this.logger.LogMessage($"  Result:         {result}");
                this.logger.LogMessage($"=========================================");
                this.logger.LogMessage($"");
            }

            return;
        }

        //
        // METHOD : SendResponseAsync
        // DESCRIPTION :
        // This asynchronous method is responsible for sending a response message back to the client.
        // It takes care of writing the response to the client's stream and logging the sent message. If any errors occur while sending the response,
        // it logs the error and rethrows the exception to be handled by the calling method.
        // PARAMETERS : 
        // RETURNS :
        //
        private async Task SendResponseAsync(string response, CancellationToken cancellationToken)
        {
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            try
            {
                await this.writer!.WriteLineAsync(response);
                this.logger.LogDebug($"[{clientEndpoint}] Sent: {response}");
            }
            catch (Exception ex)
            {
                this.logger.LogError($"[{clientEndpoint}] Failed to send response: {ex.Message}");
                throw;
            }

            return;
        }
    }
}
