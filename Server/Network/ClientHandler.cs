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
            this.client = client;
            this.logger = logger;
            this.stream = null;
            this.reader = null;
            this.writer = null;
            this.currentGame = null;

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
                this.writer = new StreamWriter(this.stream, Encoding.UTF8) { AutoFlush = true };

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
        // DESCRIPTION : This asynchronous method processes a single request from the client and sends back an appropriate response based on the command received.
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
                        await this.HandleStartGameAsync(cancellationToken);
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
        private async Task HandleStartGameAsync(CancellationToken cancellationToken)
        {
            GameData? gameData = null;
            string response = string.Empty;
            string clientEndpoint = string.Empty;

            clientEndpoint = this.client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            try
            {
                gameData = GameDataLoader.LoadRandomGame();
                this.currentGame = new GameSession(gameData);

                response = $"{ProtocolCommands.GameStart}|{gameData.PuzzleString}|{gameData.WordCount}|{GameSession.GameDurationSeconds}";
                await this.SendResponseAsync(response, cancellationToken);

                this.logger.LogMessage($"[{clientEndpoint}] Game started - Puzzle: {gameData.PuzzleString}");
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
                await this.SendResponseAsync($"{ProtocolCommands.Error}|Cannot guess - game is not active", cancellationToken);
                return;
            }

            if (this.currentGame.IsGameOver())
            {
                this.currentGame.Status = GameStatus.Lost;
                await this.SendResponseAsync($"{ProtocolCommands.GameOver}|TIME EXPIRED", cancellationToken);
                return;
            }

            guessResult = this.currentGame.ProcessGuess(word);

            switch (guessResult)
            {
                case GuessResult.Found:
                    response = $"{ProtocolCommands.WordFound}|{word.ToUpper()}|{this.currentGame.GetFoundCount()}|{this.currentGame.GetTotalWords()}";
                    this.logger.LogMessage($"[{clientEndpoint}] Word found: {word}");

                    if (this.currentGame.IsGameComplete())
                    {
                        this.currentGame.Status = GameStatus.Won;
                        await this.SendResponseAsync(response, cancellationToken);
                        await this.SendResponseAsync($"{ProtocolCommands.GameOver}|ALL WORDS FOUND! Score: {this.currentGame.GetScore()}|Guesses: {this.currentGame.GuessCount}", cancellationToken);
                        return;
                    }
                    break;

                case GuessResult.AlreadyFound:
                    response = $"{ProtocolCommands.WordAlreadyExist}|{word.ToUpper()}";
                    break;

                case GuessResult.NotFound:
                    response = $"{ProtocolCommands.WordDoesNotExist}|{word.ToUpper()}";
                    break;

                case GuessResult.TimeExpired:
                    this.currentGame.Status = GameStatus.Lost;
                    response = $"{ProtocolCommands.GameOver}|TIME EXPIRED";
                    break;

                default:
                    response = $"{ProtocolCommands.Error}|Invalid guess";
                    break;
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

            if (this.currentGame == null)
            {
                await this.SendResponseAsync($"{ProtocolCommands.Error}|No active game", cancellationToken);
                return;
            }

            foundCount = this.currentGame.GetFoundCount();
            totalWords = this.currentGame.GetTotalWords();
            remainingSeconds = this.currentGame.GetRemainingSeconds();
            guessCount = this.currentGame.GuessCount;

            response = $"{ProtocolCommands.ProgressUpdate}|{foundCount}|{totalWords}|{remainingSeconds}|{guessCount}";
            await this.SendResponseAsync(response, cancellationToken);

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
                this.currentGame.Status = GameStatus.Ended;
                this.currentGame = null;
                await this.SendResponseAsync($"{ProtocolCommands.GameEnded}|Score: {finalScore}", cancellationToken);
                this.logger.LogMessage($"[{clientEndpoint}] Game ended by client");
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
