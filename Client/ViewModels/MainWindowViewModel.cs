/*
* FILE            : MainWindowViewModel.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   ViewModel for the MainWindow UI
*   Stores client-side UI state for the word game screen, including player name, puzzle string, guess word, feedback message, and found words list.
*   Communicates with the server using a persistent TCP connection and the server's protocol commands.
*/


using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WordGameClient.Commands;
using WordGameClient.Models;
using WordGameClient.Network;
using WordGameClient.Protocol;

namespace WordGameClient.ViewModels
{
    //
    // CLASS : MainWindowViewModel
    // DESCRIPTION : ViewModel for the main game window. Manages UI state, handles user commands (start, guess, etc.),
    //               communicates with the game server via TCP, and updates the UI accordingly.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private const string HighScoreFilePath = "highscores.txt";
        private const int IdleTimeoutSeconds = 120;
        private const int IdleWarning60 = 60;
        private const int IdleWarning30 = 30;
        private static readonly Regex NameRegex = new Regex(@"^[A-Za-z][A-Za-z0-9 _\-]{1,29}$");
        private static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$");

        private readonly ClientSettings? settings;
        private readonly TcpRequestResponseClient tcpClient;
        private readonly DispatcherTimer? progressTimer;
        private readonly DispatcherTimer? idleTimer;

        private string configurationErrorMessage;
        private string serverEndpointLabel;

        private string playerName;
        private string playerEmail;

        private string puzzleString;
        private string guessWord;

        private string feedbackMessage;
        private int timeLeftSeconds;
        private int wordsLeft;
        private int triesUsed;
        private int maxTries;
        private int currentScore;

        private bool isBusy;
        private bool isGameActive;
        private string connectionStatus;
        private int highScore;
        private int idleSecondsRemaining;
        private bool idleWarning60Shown;
        private bool idleWarning30Shown;

        public ObservableCollection<string> FoundWords { get; }
        public ObservableCollection<string> LogMessages { get; }

        public AsyncRelayCommand StartGameCommand { get; }
        public AsyncRelayCommand SubmitGuessCommand { get; }
        public AsyncRelayCommand PlayAgainYesCommand { get; }
        public AsyncRelayCommand PlayAgainNoCommand { get; }
        public AsyncRelayCommand DisconnectCommand { get; }

        //
        // PROPERTY : ConfigurationErrorMessage
        // DESCRIPTION : Gets or sets the configuration error message. Updates visibility of error display.
        // PARAMETERS : n/a
        // RETURNS : string
        //

        public string ConfigurationErrorMessage
        {
            get { return (this.configurationErrorMessage); }
            private set
            {
                this.configurationErrorMessage = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(ConfigurationErrorVisibility));

                return;
            }
        }

        //
        // PROPERTY : ConfigurationErrorVisibility
        // DESCRIPTION : Returns Visibility.Visible if ConfigurationErrorMessage is not empty, otherwise Collapsed.
        // PARAMETERS : n/a
        // RETURNS : Visibility
        //

        public Visibility ConfigurationErrorVisibility
        {
            get
            {
                Visibility visibility = Visibility.Collapsed;

                if (string.IsNullOrWhiteSpace(this.ConfigurationErrorMessage) == false)
                {
                    visibility = Visibility.Visible;
                }

                return (visibility);
            }
        }

        //
        // PROPERTY : ServerEndpointLabel
        // DESCRIPTION : Display label showing server IP and port.
        // PARAMETERS : n/a
        // RETURNS : string
        //

        public string ServerEndpointLabel
        {
            get { return (this.serverEndpointLabel); }
            set
            {
                this.serverEndpointLabel = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : PlayerName
        // DESCRIPTION : Gets or sets the player name input.
        // PARAMETERS : n/a
        // RETURNS : string
        //
        public string PlayerName
        {
            get { return (this.playerName); }
            set
            {
                this.playerName = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : PlayerEmail
        // DESCRIPTION : Gets or sets the player email input.
        // PARAMETERS : n/a
        // RETURNS : string
        //

        public string PlayerEmail
        {
            get { return (this.playerEmail); }
            set
            {
                this.playerEmail = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : PuzzleString
        // DESCRIPTION : Gets the current puzzle string (display only).
        // PARAMETERS : n/a
        // RETURNS : string
        //

        public string PuzzleString
        {
            get { return (this.puzzleString); }
            private set
            {
                this.puzzleString = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : GuessWord
        // DESCRIPTION : Gets or sets the current guess word input.
        // PARAMETERS : n/a
        // RETURNS : string
        //

        public string GuessWord
        {
            get { return (this.guessWord); }
            set
            {
                this.guessWord = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : FeedbackMessage
        // DESCRIPTION : Gets the feedback message displayed to the user.
        // PARAMETERS : n/a
        // RETURNS : string
        //
        public string FeedbackMessage
        {
            get { return (this.feedbackMessage); }
            private set
            {
                this.feedbackMessage = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : TimeLeftSeconds
        // DESCRIPTION : Gets the remaining game time in seconds.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int TimeLeftSeconds
        {
            get { return (this.timeLeftSeconds); }
            private set
            {
                this.timeLeftSeconds = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : WordsLeft
        // DESCRIPTION : Gets the number of words remaining to find.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int WordsLeft
        {
            get { return (this.wordsLeft); }
            private set
            {
                this.wordsLeft = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : TriesUsed
        // DESCRIPTION : Gets the number of guesses used so far.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int TriesUsed
        {
            get { return (this.triesUsed); }
            private set
            {
                this.triesUsed = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : MaxTries
        // DESCRIPTION : Gets the maximum allowed guesses.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int MaxTries
        {
            get { return (this.maxTries); }
            private set
            {
                this.maxTries = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : CurrentScore
        // DESCRIPTION : Gets the current score.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int CurrentScore
        {
            get { return (this.currentScore); }
            private set
            {
                this.currentScore = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : IsBusy
        // DESCRIPTION : True if an asynchronous operation is in progress; disables commands.
        // PARAMETERS : n/a
        // RETURNS : bool
        //
        public bool IsBusy
        {
            get { return (this.isBusy); }
            private set
            {
                this.isBusy = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : ConnectionStatus
        // DESCRIPTION : Gets or sets the connection status string.
        // PARAMETERS : n/a
        // RETURNS : string
        //
        public string ConnectionStatus
        {
            get { return (this.connectionStatus); }
            set
            {
                this.connectionStatus = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // PROPERTY : HighScore
        // DESCRIPTION : Gets the highest score achieved this session.
        // PARAMETERS : n/a
        // RETURNS : int
        //
        public int HighScore
        {
            get { return (this.highScore); }
            private set
            {
                this.highScore = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

        //
        // FUNCTION : MainWindowViewModel (constructor)
        // DESCRIPTION : Initializes the ViewModel, sets up commands, loads high score, and initializes timers.
        // PARAMETERS : ClientSettings? settings - validated settings or null if config error;
        //              string configurationErrorMessage - error message if config failed.
        // RETURNS : n/a
        //
        public MainWindowViewModel(ClientSettings? settings, string configurationErrorMessage)
        {
            this.settings = settings;
            this.tcpClient = new TcpRequestResponseClient();

            this.configurationErrorMessage = configurationErrorMessage;

            this.playerName = string.Empty;
            this.playerEmail = string.Empty;

            this.puzzleString = string.Empty;
            this.guessWord = string.Empty;

            this.feedbackMessage = "Ready.";
            this.timeLeftSeconds = 0;
            this.wordsLeft = 0;
            this.triesUsed = 0;
            this.maxTries = 0;
            this.currentScore = 0;

            this.isBusy = false;
            this.isGameActive = false;
            this.connectionStatus = "NOT CONNECTED";
            this.highScore = 0;
            this.idleSecondsRemaining = 0;
            this.idleWarning60Shown = false;
            this.idleWarning30Shown = false;

            this.FoundWords = new ObservableCollection<string>();
            this.LogMessages = new ObservableCollection<string>();

            if (this.settings != null)
            {
                this.serverEndpointLabel = "Server: " + this.settings.ServerIp + ":" + this.settings.ServerPort.ToString();
            }
            else
            {
                this.serverEndpointLabel = "Server: (check App.config)";
            }

            this.StartGameCommand = new AsyncRelayCommand(this.StartGameAsync, this.CanStartGame);
            this.SubmitGuessCommand = new AsyncRelayCommand(this.SubmitGuessAsync, this.CanSubmitGuess);
            this.PlayAgainYesCommand = new AsyncRelayCommand(this.PlayAgainYesAsync, this.CanPlayAgain);
            this.PlayAgainNoCommand = new AsyncRelayCommand(this.PlayAgainNoAsync, this.CanPlayAgain);
            this.DisconnectCommand = new AsyncRelayCommand(this.DisconnectAsync, this.CanPlayAgain);

            this.ConfigurationErrorMessage = configurationErrorMessage;

            this.progressTimer = new DispatcherTimer();
            this.progressTimer.Interval = TimeSpan.FromSeconds(1);
            this.progressTimer.Tick += this.OnProgressTimerTick;

            this.idleTimer = new DispatcherTimer();
            this.idleTimer.Interval = TimeSpan.FromSeconds(1);
            this.idleTimer.Tick += this.OnIdleTimerTick;

            return;
        }

        //
        // FUNCTION : OnProgressTimerTick
        // DESCRIPTION : Timer tick handler for periodic progress updates. Fetches current game state from server.
        // PARAMETERS : object? sender - event source; EventArgs e - event data.
        // RETURNS : void (async void)
        //
        private async void OnProgressTimerTick(object? sender, EventArgs e)
        {
            if ((this.isGameActive == false) || (this.tcpClient.IsConnected == false) || (this.isBusy == true))
            {
                if (this.tcpClient.IsConnected == false)
                {
                    this.StopProgressTimer();
                    this.isGameActive = false;
                    this.ConnectionStatus = "NOT CONNECTED";
                }
                return;
            }

            await this.RefreshProgressAsync();

            return;
        }

        //
        // FUNCTION : StartProgressTimer
        // DESCRIPTION : Starts the progress update timer.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        private void StartProgressTimer()
        {
            this.progressTimer?.Start();

            return;
        }

        //
        // FUNCTION : StopProgressTimer
        // DESCRIPTION : Stops the progress update timer.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        private void StopProgressTimer()
        {
            this.progressTimer?.Stop();

            return;
        }

        //
        // FUNCTION : ShowGameOverPopup
        // DESCRIPTION : Displays a modal popup with game over message and score, asks to play again.
        //               Updates high score if current score exceeds previous high.
        // PARAMETERS : string reason - reason for game over; int score - final score.
        // RETURNS : void
        //
        private void ShowGameOverPopup(string reason, int score)
        {
            this.StopIdleTimer();

            string message = reason + "\n\nScore: " + score;

            if (score > this.HighScore)
            {
                this.HighScore = score;
                this.SaveHighScore(score);
                message = message + "\n\nNEW HIGH SCORE!";
            }

            message = message + "\n\nWould you like to play again?";

            MessageBoxResult result = MessageBox.Show(message, "Game Over", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                this.PlayAgainYesCommand.Execute(null);
            }
            else
            {
                this.PlayAgainNoCommand.Execute(null);
            }

            return;
        }

        //
        // FUNCTION : LoadHighScore
        // DESCRIPTION : Loads the highest score from the local highscore file.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        private void LoadHighScore()
        {
            try
            {
                if (File.Exists(HighScoreFilePath))
                {
                    string[] lines = File.ReadAllLines(HighScoreFilePath);

                    if (lines.Length > 0)
                    {
                        // Format: Score|PlayerName|PlayerEmail
                        string[] parts = lines[0].Split('|');

                        if (parts.Length >= 1)
                        {
                            int parsed = 0;
                            if (int.TryParse(parts[0], out parsed))
                            {
                                this.highScore = parsed;
                            }
                        }
                    }
                }
            }
            catch
            {
                this.highScore = 0;
            }

            return;
        }

        //
        // FUNCTION : SaveHighScore
        // DESCRIPTION : Appends the new high score to the local highscore file with player info.
        // PARAMETERS : int score - the score to save.
        // RETURNS : void
        //
        private void SaveHighScore(int score)
        {
            try
            {
                string playerId = Guid.NewGuid().ToString("N").Substring(0, 8);
                string name = string.IsNullOrWhiteSpace(this.PlayerName) ? "Unknown" : this.PlayerName.Trim();
                string email = string.IsNullOrWhiteSpace(this.PlayerEmail) ? "N/A" : this.PlayerEmail.Trim();
                string line = score + "|" + name + "|" + playerId + "|" + email;

                string existingContent = string.Empty;
                if (File.Exists(HighScoreFilePath))
                {
                    existingContent = File.ReadAllText(HighScoreFilePath);
                }

                File.WriteAllText(HighScoreFilePath, line + Environment.NewLine + existingContent);

                this.AddLog("High score saved: " + score + " by " + name);
            }
            catch (Exception ex)
            {
                this.AddLog("Failed to save high score: " + ex.Message);
            }

            return;
        }

        //
        // FUNCTION : CanStartGame
        // DESCRIPTION : Determines whether the StartGame command can execute.
        // PARAMETERS : n/a
        // RETURNS : bool
        //
        private bool CanStartGame()
        {
            bool canStart = false;

            if ((this.settings != null) &&
                (string.IsNullOrWhiteSpace(this.ConfigurationErrorMessage) == true) &&
                (this.IsBusy == false))
            {
                canStart = true;
            }

            return (canStart);
        }

        //
        // FUNCTION : CanUseSession
        // DESCRIPTION : Determines whether the client can perform game actions (connected, game active, not busy).
        // PARAMETERS : n/a
        // RETURNS : bool
        //
        private bool CanUseSession()
        {
            bool canUse = false;

            if ((this.settings != null) &&
                (this.IsBusy == false) &&
                (this.isGameActive == true) &&
                (this.tcpClient.IsConnected == true))
            {
                canUse = true;
            }

            return (canUse);
        }

        //
        // FUNCTION : CanSubmitGuess
        // DESCRIPTION : Determines whether the SubmitGuess command can execute.
        // PARAMETERS : n/a
        // RETURNS : bool
        //
        private bool CanSubmitGuess()
        {
            bool canSubmit = false;

            if ((this.CanUseSession() == true) &&
                (string.IsNullOrWhiteSpace(this.GuessWord) == false))
            {
                canSubmit = true;
            }

            return (canSubmit);
        }

        //
        // FUNCTION : CanPlayAgain
        // DESCRIPTION : Determines whether the PlayAgain (Yes/No) and Disconnect commands can execute.
        // PARAMETERS : n/a
        // RETURNS : bool
        //
        private bool CanPlayAgain()
        {
            bool canPlay = false;

            if ((this.settings != null) &&
                (this.IsBusy == false))
            {
                canPlay = true;
            }

            return (canPlay);
        }

        //
        // FUNCTION : AddLog
        // DESCRIPTION : Adds a timestamped log message to the LogMessages collection.
        // PARAMETERS : string message - the message to log.
        // RETURNS : void
        //
        private void AddLog(string message)
        {
            this.LogMessages.Add(message);

            return;
        }

        //
        // FUNCTION : SendAsync
        // DESCRIPTION : Sends a request line to the server using the TCP client and returns the response.
        // PARAMETERS : string requestLine - the protocol command line to send.
        // RETURNS : Task<NetworkResult> - the server response wrapped in a NetworkResult.
        //
        private async Task<NetworkResult> SendAsync(string requestLine)
        {
            NetworkResult result = new NetworkResult(false, string.Empty, "Missing settings.");

            if (this.settings != null)
            {
                result = await this.tcpClient.SendAndReceiveAsync(this.settings, requestLine, CancellationToken.None);
            }

            return (result);
        }

        //
        // FUNCTION : StartGameAsync
        // DESCRIPTION : Initiates a new game: validates name/email, connects to server (if not connected),
        //               sends START command, processes GAMESTART response, and starts the progress timer.
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        private async Task StartGameAsync()
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(this.PlayerName))
            {
                this.FeedbackMessage = "Please enter your Player Name before starting.";
                MessageBox.Show("Player Name is required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NameRegex.IsMatch(this.PlayerName.Trim()) == false)
            {
                this.FeedbackMessage = "Player Name must start with a letter and be 2-30 characters (letters, numbers, spaces, hyphens, underscores).";
                MessageBox.Show("Player Name must start with a letter and be 2-30 characters long.\nAllowed: letters, numbers, spaces, hyphens, underscores.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.PlayerEmail))
            {
                this.FeedbackMessage = "Please enter your Player Email before starting.";
                MessageBox.Show("Player Email is required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EmailRegex.IsMatch(this.PlayerEmail.Trim()) == false)
            {
                this.FeedbackMessage = "Please enter a valid email address (e.g., player@example.com).";
                MessageBox.Show("Please enter a valid email address.\nExample: player@example.com", "Invalid Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.IsBusy = true;
            this.StopProgressTimer();
            this.StopIdleTimer();

            bool started = false;
            string message = string.Empty;

            this.FoundWords.Clear();
            this.PuzzleString = string.Empty;
            this.TimeLeftSeconds = 0;
            this.WordsLeft = 0;
            this.TriesUsed = 0;
            this.MaxTries = 0;
            this.CurrentScore = 0;
            this.isGameActive = false;

            try
            {
                if (this.settings == null)
                {
                    message = "Missing settings.";
                }
                else
                {
                    // Connect if not already connected
                    if (this.tcpClient.IsConnected == false)
                    {
                        this.tcpClient.Disconnect();
                        NetworkResult connectRes = await this.tcpClient.ConnectAsync(this.settings, CancellationToken.None);

                        if (connectRes.IsSuccess == false)
                        {
                            message = "Connection failed: " + connectRes.ErrorMessage;
                        }
                        else
                        {
                            this.ConnectionStatus = "CONNECTED";
                            this.AddLog("Connected to server.");
                        }
                    }

                    if (this.tcpClient.IsConnected == true)
                    {
                        // Send START|playerName|playerEmail -> expect GAMESTART|puzzle|wordCount|duration|maxGuesses
                        string startCommand = ProtocolCommands.Start + ProtocolConstants.Delimiter + this.PlayerName.Trim() + ProtocolConstants.Delimiter + this.PlayerEmail.Trim();
                        NetworkResult startRes = await this.SendAsync(startCommand);

                        if (startRes.IsSuccess == false)
                        {
                            message = "Start game failed: " + startRes.ErrorMessage;
                        }
                        else
                        {
                            string[] tokens = startRes.ResponseLine.Split(ProtocolConstants.Delimiter);
                            string responseType = tokens[0];

                            if (responseType == ProtocolCommands.GameStart)
                            {
                                // GAMESTART|puzzleString|wordCount|duration|maxGuesses
                                if (tokens.Length >= 4)
                                {
                                    this.PuzzleString = tokens[1];

                                    int wordCount = 0;
                                    if (int.TryParse(tokens[2], out wordCount) == true)
                                    {
                                        this.WordsLeft = wordCount;
                                    }

                                    int duration = 0;
                                    if (int.TryParse(tokens[3], out duration) == true)
                                    {
                                        this.TimeLeftSeconds = duration;
                                    }

                                    if (tokens.Length >= 5)
                                    {
                                        int maxGuesses = 0;
                                        if (int.TryParse(tokens[4], out maxGuesses) == true)
                                        {
                                            this.MaxTries = maxGuesses;
                                        }
                                    }

                                    this.isGameActive = true;
                                    started = true;
                                    message = "Game started! Find " + wordCount + " words. You have " + this.MaxTries + " tries. Hidden and reversed words also count!";
                                }
                                else
                                {
                                    message = "Invalid GAMESTART response format.";
                                }
                            }
                            else if (responseType == ProtocolCommands.Error)
                            {
                                message = "Server error: " + (tokens.Length >= 2 ? tokens[1] : "Unknown");
                            }
                            else
                            {
                                message = "Unexpected response: " + startRes.ResponseLine;
                            }
                        }
                    }
                }
            }
            finally
            {
                this.IsBusy = false;
            }

            if (started == false)
            {
                this.isGameActive = false;
                if (this.tcpClient.IsConnected == false)
                {
                    this.ConnectionStatus = "NOT CONNECTED";
                }
            }
            else
            {
                this.StartProgressTimer();
            }

            this.FeedbackMessage = message;
            this.AddLog(message);

            return;
        }

        //
        // FUNCTION : SubmitGuessAsync
        // DESCRIPTION : Sends a GUESS command with the current guess word, processes the response,
        //               updates UI (found words, words left, etc.), and checks for game over.
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        private async Task SubmitGuessAsync()
        {
            this.IsBusy = true;

            string message = string.Empty;
            string cleaned = this.GuessWord.Trim();
            bool gameEnded = false;
            string gameOverReason = string.Empty;
            int gameOverScore = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(cleaned) == true)
                {
                    message = "Please enter a word.";
                }
                else
                {
                    string req = ProtocolCommands.Guess + ProtocolConstants.Delimiter + cleaned;
                    NetworkResult res = await this.SendAsync(req);

                    if (res.IsSuccess == false)
                    {
                        message = "Guess failed: " + res.ErrorMessage;
                        this.isGameActive = false;
                        this.StopProgressTimer();
                        this.ConnectionStatus = "NOT CONNECTED";
                    }
                    else
                    {
                        string responseLine = res.ResponseLine;

                        if (responseLine.StartsWith(ProtocolCommands.WordFound))
                        {
                            string[] tokens = responseLine.Split(ProtocolConstants.Delimiter);
                            string foundWord = (tokens.Length >= 2) ? tokens[1] : cleaned;
                            this.FoundWords.Add(foundWord);
                            message = "FOUND: " + foundWord;

                            if (tokens.Length >= 4)
                            {
                                int foundCount = 0;
                                int totalWords = 0;
                                int.TryParse(tokens[2], out foundCount);
                                int.TryParse(tokens[3], out totalWords);
                                this.WordsLeft = totalWords - foundCount;
                            }
                        }
                        else if (responseLine.StartsWith(ProtocolCommands.WordAlreadyExist))
                        {
                            message = "Already submitted: " + cleaned;
                        }
                        else if (responseLine.StartsWith(ProtocolCommands.WordDoesNotExist))
                        {
                            message = "Not found: " + cleaned;
                        }
                        else if (responseLine.StartsWith(ProtocolCommands.GameOver))
                        {
                            string[] tokens = responseLine.Split(ProtocolConstants.Delimiter);
                            gameOverReason = (tokens.Length >= 2) ? tokens[1] : "Game over";
                            message = "GAME OVER: " + gameOverReason;
                            this.isGameActive = false;
                            this.StopProgressTimer();
                            gameEnded = true;

                            // If all words found, set words left to 0
                            if (gameOverReason.Contains("ALL WORDS FOUND"))
                            {
                                this.WordsLeft = 0;
                            }

                            // Parse score from server response (e.g. "ALL WORDS FOUND! Score: 894")
                            gameOverScore = this.CurrentScore;
                            foreach (string token in tokens)
                            {
                                int scoreIdx = token.IndexOf("Score:");
                                if (scoreIdx >= 0)
                                {
                                    string scoreStr = token.Substring(scoreIdx + 6).Trim();
                                    int parsedScore = 0;
                                    if (int.TryParse(scoreStr, out parsedScore))
                                    {
                                        gameOverScore = parsedScore;
                                        this.CurrentScore = parsedScore;
                                    }
                                }
                            }
                        }
                        else if (responseLine.StartsWith(ProtocolCommands.Error))
                        {
                            string[] tokens = responseLine.Split(ProtocolConstants.Delimiter);
                            message = "Server error: " + ((tokens.Length >= 2) ? tokens[1] : "Unknown");
                        }
                        else
                        {
                            message = "Unexpected response: " + responseLine;
                        }
                    }
                }
            }
            finally
            {
                this.IsBusy = false;
            }

            this.GuessWord = string.Empty;
            this.FeedbackMessage = message;
            this.AddLog(message);

            if (gameEnded == true)
            {
                this.ShowGameOverPopup(gameOverReason, gameOverScore);
            }

            return;
        }

        //
        // FUNCTION : RefreshProgressAsync
        // DESCRIPTION : Sends a PROGRESS command to the server and updates UI with current game state.
        //               Also detects game over conditions (time expired, all words found, out of tries).
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        private async Task RefreshProgressAsync()
        {
            if ((this.settings == null) || (this.tcpClient.IsConnected == false))
            {
                this.StopProgressTimer();
                this.isGameActive = false;
                this.ConnectionStatus = "NOT CONNECTED";
                return;
            }

            NetworkResult res = await this.SendAsync(ProtocolCommands.Progress);

            if (res.IsSuccess == true)
            {
                string[] tokens = res.ResponseLine.Split(ProtocolConstants.Delimiter);

                // PROGRESS|foundCount|totalWords|remainingSeconds|guessCount|score|maxGuesses
                if ((tokens.Length >= 5) && (tokens[0] == ProtocolCommands.ProgressUpdate))
                {
                    int foundCount = 0;
                    int totalWords = 0;
                    int remainingSeconds = 0;
                    int guessCount = 0;
                    int score = 0;
                    int maxGuesses = 0;

                    int.TryParse(tokens[1], out foundCount);
                    int.TryParse(tokens[2], out totalWords);
                    int.TryParse(tokens[3], out remainingSeconds);
                    int.TryParse(tokens[4], out guessCount);

                    if (tokens.Length >= 6)
                    {
                        int.TryParse(tokens[5], out score);
                    }
                    if (tokens.Length >= 7)
                    {
                        int.TryParse(tokens[6], out maxGuesses);
                    }

                    this.TimeLeftSeconds = remainingSeconds;
                    this.WordsLeft = totalWords - foundCount;
                    this.TriesUsed = guessCount;
                    this.CurrentScore = score;

                    if (maxGuesses > 0)
                    {
                        this.MaxTries = maxGuesses;
                    }

                    // Auto detect game over: time expired
                    if (remainingSeconds <= 0)
                    {
                        this.isGameActive = false;
                        this.StopProgressTimer();
                        this.FeedbackMessage = "GAME OVER: Time expired!";
                        this.AddLog("GAME OVER: Time expired!");
                        this.ShowGameOverPopup("Time expired!", score);
                    }
                    // Auto detect game over: all words found
                    else if (foundCount >= totalWords)
                    {
                        this.isGameActive = false;
                        this.StopProgressTimer();
                        this.FeedbackMessage = "GAME OVER: All words found!";
                        this.AddLog("GAME OVER: All words found!");
                        this.ShowGameOverPopup("All words found!", score);
                    }
                    // Auto detect game over: out of tries
                    else if ((maxGuesses > 0) && (guessCount >= maxGuesses))
                    {
                        this.isGameActive = false;
                        this.StopProgressTimer();
                        this.FeedbackMessage = "GAME OVER: Out of tries!";
                        this.AddLog("GAME OVER: Out of tries!");
                        this.ShowGameOverPopup("Out of tries!", score);
                    }
                }
                else if ((tokens.Length >= 1) && (tokens[0] == ProtocolCommands.Error))
                {
                    string errorMsg = (tokens.Length >= 2) ? tokens[1] : "Unknown error";
                    this.AddLog("Progress error: " + errorMsg);
                    this.StopProgressTimer();
                    this.isGameActive = false;
                }
            }
            else
            {
                this.StopProgressTimer();
                this.isGameActive = false;
                this.ConnectionStatus = "NOT CONNECTED";
            }

            return;
        }

        //
        // FUNCTION : PlayAgainYesAsync
        // DESCRIPTION : Handles user clicking "Yes" on the game over popup. Restarts the game.
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        private async Task PlayAgainYesAsync()
        {
            this.StopProgressTimer();
            this.StopIdleTimer();
            this.isGameActive = false;

            await this.StartGameAsync();

            return;
        }

        //
        // FUNCTION : PlayAgainNoAsync
        // DESCRIPTION : Handles user clicking "No" on the game over popup. Ends the game but stays connected.
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        private async Task PlayAgainNoAsync()
        {
            this.IsBusy = true;
            this.StopProgressTimer();

            string message = string.Empty;

            try
            {
                this.isGameActive = false;
                message = "Game ended. Still connected to server.";
            }
            finally
            {
                this.IsBusy = false;
            }

            this.FeedbackMessage = message;
            this.AddLog(message);

            // Start idle timer since game ended but still connected
            this.StartIdleTimer();

            return;
        }

        //
        // FUNCTION : OnIdleTimerTick
        // DESCRIPTION : Timer tick handler for idle disconnect. Decrements remaining seconds and shows warnings.
        //               Disconnects when timer reaches zero.
        // PARAMETERS : object? sender - event source; EventArgs e - event data.
        // RETURNS : void
        //
        private void OnIdleTimerTick(object? sender, EventArgs e)
        {
            if (this.tcpClient.IsConnected == false)
            {
                this.StopIdleTimer();
                this.ConnectionStatus = "NOT CONNECTED";
                return;
            }

            this.idleSecondsRemaining--;

            if ((this.idleSecondsRemaining <= IdleWarning60) && (this.idleWarning60Shown == false))
            {
                this.idleWarning60Shown = true;
                this.AddLog("WARNING: Auto-disconnect in 60 seconds due to inactivity.");
            }

            if ((this.idleSecondsRemaining <= IdleWarning30) && (this.idleWarning30Shown == false))
            {
                this.idleWarning30Shown = true;
                this.AddLog("WARNING: Auto-disconnect in 30 seconds due to inactivity.");
            }

            if (this.idleSecondsRemaining <= 0)
            {
                this.StopIdleTimer();
                this.AddLog("Auto-disconnected from server due to inactivity.");
                this.tcpClient.Disconnect();
                this.ConnectionStatus = "NOT CONNECTED";
                this.FeedbackMessage = "Disconnected due to inactivity.";
            }

            return;
        }

        //
        // FUNCTION : StartIdleTimer
        // DESCRIPTION : Starts the idle disconnect timer with initial timeout value.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        private void StartIdleTimer()
        {
            this.idleSecondsRemaining = IdleTimeoutSeconds;
            this.idleWarning60Shown = false;
            this.idleWarning30Shown = false;
            this.idleTimer?.Start();

            return;
        }

        //
        // FUNCTION : StopIdleTimer
        // DESCRIPTION : Stops the idle disconnect timer.
        // PARAMETERS : n/a
        // RETURNS : void
        //
        private void StopIdleTimer()
        {
            this.idleTimer?.Stop();

            return;
        }

        //
        // FUNCTION : DisconnectAsync
        // DESCRIPTION : Sends a QUIT command to the server, disconnects the TCP client, and updates UI.
        // PARAMETERS : n/a
        // RETURNS : Task
        //
        public async Task DisconnectAsync()
        {
            this.IsBusy = true;
            this.StopProgressTimer();
            this.StopIdleTimer();

            string message = string.Empty;

            try
            {
                if (this.tcpClient.IsConnected == true)
                {
                    NetworkResult res = await this.SendAsync(ProtocolCommands.Quit);

                    if (res.IsSuccess == true)
                    {
                        message = "Disconnected from server.";
                    }
                    else
                    {
                        message = "Disconnect error: " + res.ErrorMessage;
                    }
                }
                else
                {
                    message = "Already disconnected.";
                }

                this.tcpClient.Disconnect();
                this.isGameActive = false;
                this.ConnectionStatus = "NOT CONNECTED";
            }
            finally
            {
                this.IsBusy = false;
            }

            this.FeedbackMessage = message;
            this.AddLog(message);

            return;
        }
    }
}
