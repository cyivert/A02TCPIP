/*
* FILE            : MainWindowViewModel.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   ViewModel for the MainWindow UI
*   Stores client-side UI state for the word game screen, including player name, puzzle string, guess word, feedback message, and found words list.
*/


using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WordGameClient.Commands;
using WordGameClient.Models;
using WordGameClient.Network;
using WordGameClient.Protocol;

namespace WordGameClient.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly ClientSettings? settings;
        private readonly TcpRequestResponseClient tcpClient;

        private string configurationErrorMessage;
        private string serverEndpointLabel;

        private string playerName;
        private string sessionId;

        private string puzzleString;
        private string guessWord;

        private string feedbackMessage;
        private int timeLeftSeconds;
        private int choicesLeft;

        private bool isBusy;
        private bool isGameActive;

        public ObservableCollection<string> FoundWords { get; }
        public ObservableCollection<string> LogMessages { get; }

        public AsyncRelayCommand StartGameCommand { get; }
        public AsyncRelayCommand SubmitGuessCommand { get; }
        public AsyncRelayCommand RefreshTimeLeftCommand { get; }
        public AsyncRelayCommand RefreshChoicesLeftCommand { get; }
        public AsyncRelayCommand CheckEndGameCommand { get; }
        public AsyncRelayCommand PlayAgainYesCommand { get; }
        public AsyncRelayCommand PlayAgainNoCommand { get; }

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

        public string ServerEndpointLabel
        {
            get { return (this.serverEndpointLabel); }
            private set
            {
                this.serverEndpointLabel = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

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

        public int ChoicesLeft
        {
            get { return (this.choicesLeft); }
            private set
            {
                this.choicesLeft = value;
                this.NotifyPropertyChanged();

                return;
            }
        }

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

        public MainWindowViewModel(ClientSettings? settings, string configurationErrorMessage)
        {
            this.settings = settings;
            this.tcpClient = new TcpRequestResponseClient();

            this.configurationErrorMessage = configurationErrorMessage;

            this.playerName = string.Empty;
            this.sessionId = "0";

            this.puzzleString = string.Empty;
            this.guessWord = string.Empty;

            this.feedbackMessage = "Ready.";
            this.timeLeftSeconds = 0;
            this.choicesLeft = 0;

            this.isBusy = false;
            this.isGameActive = false;

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
            this.RefreshTimeLeftCommand = new AsyncRelayCommand(this.RefreshTimeLeftAsync, this.CanUseSession);
            this.RefreshChoicesLeftCommand = new AsyncRelayCommand(this.RefreshChoicesLeftAsync, this.CanUseSession);
            this.CheckEndGameCommand = new AsyncRelayCommand(this.CheckEndGameAsync, this.CanUseSession);

            this.PlayAgainYesCommand = new AsyncRelayCommand(this.PlayAgainYesAsync, this.CanUseSession);
            this.PlayAgainNoCommand = new AsyncRelayCommand(this.PlayAgainNoAsync, this.CanUseSession);

            this.ConfigurationErrorMessage = configurationErrorMessage;

            return;
        }

        private bool CanStartGame()
        {
            bool canStart = false;

            if ((this.settings != null) &&
                (string.IsNullOrWhiteSpace(this.ConfigurationErrorMessage) == true) &&
                (this.IsBusy == false) &&
                (string.IsNullOrWhiteSpace(this.PlayerName) == false))
            {
                canStart = true;
            }

            return (canStart);
        }

        private bool CanUseSession()
        {
            bool canUse = false;

            if ((this.settings != null) &&
                (this.IsBusy == false) &&
                (this.isGameActive == true) &&
                (string.IsNullOrWhiteSpace(this.sessionId) == false) &&
                (this.sessionId != "0"))
            {
                canUse = true;
            }

            return (canUse);
        }

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

        private void AddLog(string message)
        {
            this.LogMessages.Add(message);

            return;
        }

        private string BuildRequestLine(string command, string sessionIdValue, params string[] parts)
        {
            string line = command + ProtocolConstants.Delimiter + sessionIdValue;

            int index = 0;

            while (index < parts.Length)
            {
                line = line + ProtocolConstants.Delimiter + parts[index];
                index++;
            }

            return (line);
        }

        private bool TryParseResponse(string responseLine, out bool isOk, out string[] tokens)
        {
            bool parsed = false;

            isOk = false;
            tokens = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(responseLine) == false)
            {
                tokens = responseLine.Split(ProtocolConstants.Delimiter);

                if (tokens.Length >= 1)
                {
                    if (tokens[0] == ProtocolConstants.Ok)
                    {
                        isOk = true;
                        parsed = true;
                    }
                    else if (tokens[0] == ProtocolConstants.Err)
                    {
                        isOk = false;
                        parsed = true;
                    }
                }
            }

            return (parsed);
        }

        private async Task<NetworkResult> SendAsync(string requestLine)
        {
            NetworkResult result = new NetworkResult(false, string.Empty, "Missing settings.");

            if (this.settings != null)
            {
                result = await this.tcpClient.SendOnceAsync(this.settings, requestLine, CancellationToken.None);
            }

            return (result);
        }

        private async Task StartGameAsync()
        {
            this.IsBusy = true;

            bool started = false;
            string message = string.Empty;

            this.FoundWords.Clear();
            this.PuzzleString = string.Empty;
            this.TimeLeftSeconds = 0;
            this.ChoicesLeft = 0;
            this.sessionId = "0";
            this.isGameActive = false;

            try
            {
                // 1) check_for_new_player|0|username
                string checkReq = this.BuildRequestLine(ProtocolCommands.CheckForNewPlayer, "0", this.PlayerName.Trim());
                NetworkResult checkRes = await this.SendAsync(checkReq);

                if (checkRes.IsSuccess == false)
                {
                    message = "Check player failed: " + checkRes.ErrorMessage;
                }
                else
                {
                    bool isOk;
                    string[] tokens;

                    if (this.TryParseResponse(checkRes.ResponseLine, out isOk, out tokens) == false)
                    {
                        message = "Invalid response (check player).";
                    }
                    else if (isOk == false)
                    {
                        message = "Server error (check player).";
                        if (tokens.Length >= 2)
                        {
                            message = "Server error: " + tokens[1];
                        }
                    }
                    else
                    {
                        // Expect OK|AVAILABLE or OK|TAKEN
                        if ((tokens.Length >= 2) && (tokens[1].ToUpperInvariant() == "TAKEN"))
                        {
                            message = "Username is already taken.";
                        }
                        else
                        {
                            // 2) login_info|0|username  -> OK|sessionId
                            string loginReq = this.BuildRequestLine(ProtocolCommands.LoginInfo, "0", this.PlayerName.Trim());
                            NetworkResult loginRes = await this.SendAsync(loginReq);

                            if (loginRes.IsSuccess == false)
                            {
                                message = "Login failed: " + loginRes.ErrorMessage;
                            }
                            else
                            {
                                bool loginOk;
                                string[] loginTokens;

                                if (this.TryParseResponse(loginRes.ResponseLine, out loginOk, out loginTokens) == false)
                                {
                                    message = "Invalid response (login).";
                                }
                                else if (loginOk == false)
                                {
                                    message = "Server error (login).";
                                    if (loginTokens.Length >= 2)
                                    {
                                        message = "Server error: " + loginTokens[1];
                                    }
                                }
                                else if (loginTokens.Length < 2)
                                {
                                    message = "Login response missing sessionId.";
                                }
                                else
                                {
                                    this.sessionId = loginTokens[1];
                                    this.isGameActive = true;

                                    // 3) get_string_from_server|sessionId -> OK|puzzle|wordCount
                                    string puzzleReq = this.BuildRequestLine(ProtocolCommands.GetStringFromServer, this.sessionId);
                                    NetworkResult puzzleRes = await this.SendAsync(puzzleReq);

                                    if (puzzleRes.IsSuccess == false)
                                    {
                                        message = "Get puzzle failed: " + puzzleRes.ErrorMessage;
                                    }
                                    else
                                    {
                                        bool puzzleOk;
                                        string[] puzzleTokens;

                                        if (this.TryParseResponse(puzzleRes.ResponseLine, out puzzleOk, out puzzleTokens) == false)
                                        {
                                            message = "Invalid response (puzzle).";
                                        }
                                        else if (puzzleOk == false)
                                        {
                                            message = "Server error (puzzle).";
                                            if (puzzleTokens.Length >= 2)
                                            {
                                                message = "Server error: " + puzzleTokens[1];
                                            }
                                        }
                                        else if (puzzleTokens.Length < 2)
                                        {
                                            message = "Puzzle response missing puzzle string.";
                                        }
                                        else
                                        {
                                            this.PuzzleString = puzzleTokens[1];
                                            message = "Game started. Enter a word.";

                                            // optional: refresh stats
                                            await this.RefreshTimeLeftAsync();
                                            await this.RefreshChoicesLeftAsync();

                                            started = true;
                                        }
                                    }
                                }
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
                this.sessionId = "0";
            }

            this.FeedbackMessage = message;
            this.AddLog(message);

            return;
        }

        private async Task SubmitGuessAsync()
        {
            this.IsBusy = true;

            string message = string.Empty;
            string cleaned = this.GuessWord.Trim();

            try
            {
                if (string.IsNullOrWhiteSpace(cleaned) == true)
                {
                    message = "Please enter a word.";
                }
                else
                {
                    // word_validation|sessionId|word -> OK|FOUND/NOT_FOUND/ALREADY
                    string req = this.BuildRequestLine(ProtocolCommands.WordValidation, this.sessionId, cleaned);
                    NetworkResult res = await this.SendAsync(req);

                    if (res.IsSuccess == false)
                    {
                        message = "Validation failed: " + res.ErrorMessage;
                    }
                    else
                    {
                        bool ok;
                        string[] tokens;

                        if (this.TryParseResponse(res.ResponseLine, out ok, out tokens) == false)
                        {
                            message = "Invalid response (validation).";
                        }
                        else if (ok == false)
                        {
                            message = "Server error (validation).";
                            if (tokens.Length >= 2)
                            {
                                message = "Server error: " + tokens[1];
                            }
                        }
                        else if (tokens.Length < 2)
                        {
                            message = "Validation response missing status.";
                        }
                        else
                        {
                            string status = tokens[1].ToUpperInvariant();

                            if (status == "FOUND")
                            {
                                this.FoundWords.Add(cleaned);
                                message = "FOUND: " + cleaned;
                            }
                            else if (status == "ALREADY")
                            {
                                message = "Already submitted: " + cleaned;
                            }
                            else
                            {
                                message = "Not found: " + cleaned;
                            }

                            await this.RefreshTimeLeftAsync();
                            await this.RefreshChoicesLeftAsync();
                            await this.CheckEndGameAsync();
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

            return;
        }

        private async Task RefreshTimeLeftAsync()
        {
            string message = string.Empty;

            if (this.settings != null)
            {
                string req = this.BuildRequestLine(ProtocolCommands.TimeLeft, this.sessionId);
                NetworkResult res = await this.SendAsync(req);

                if (res.IsSuccess == true)
                {
                    bool ok;
                    string[] tokens;

                    if (this.TryParseResponse(res.ResponseLine, out ok, out tokens) == true)
                    {
                        if ((ok == true) && (tokens.Length >= 2))
                        {
                            int parsed = 0;

                            if (int.TryParse(tokens[1], out parsed) == true)
                            {
                                this.TimeLeftSeconds = parsed;
                            }
                        }
                        else if ((ok == false) && (tokens.Length >= 2))
                        {
                            message = "Time left error: " + tokens[1];
                            this.AddLog(message);
                        }
                    }
                }
            }

            return;
        }

        private async Task RefreshChoicesLeftAsync()
        {
            string message = string.Empty;

            if (this.settings != null)
            {
                string req = this.BuildRequestLine(ProtocolCommands.ChoicesLeft, this.sessionId);
                NetworkResult res = await this.SendAsync(req);

                if (res.IsSuccess == true)
                {
                    bool ok;
                    string[] tokens;

                    if (this.TryParseResponse(res.ResponseLine, out ok, out tokens) == true)
                    {
                        if ((ok == true) && (tokens.Length >= 2))
                        {
                            int parsed = 0;

                            if (int.TryParse(tokens[1], out parsed) == true)
                            {
                                this.ChoicesLeft = parsed;
                            }
                        }
                        else if ((ok == false) && (tokens.Length >= 2))
                        {
                            message = "Choices left error: " + tokens[1];
                            this.AddLog(message);
                        }
                    }
                }
            }

            return;
        }

        private async Task CheckEndGameAsync()
        {
            string message = string.Empty;

            if (this.settings != null)
            {
                string req = this.BuildRequestLine(ProtocolCommands.CheckEndGame, this.sessionId);
                NetworkResult res = await this.SendAsync(req);

                if (res.IsSuccess == true)
                {
                    bool ok;
                    string[] tokens;

                    if (this.TryParseResponse(res.ResponseLine, out ok, out tokens) == true)
                    {
                        if ((ok == true) && (tokens.Length >= 2))
                        {
                            string status = tokens[1].ToUpperInvariant();

                            if (status == "ENDED")
                            {
                                message = "Game ended. Choose Play Again YES/NO.";
                                this.FeedbackMessage = message;
                                this.AddLog(message);
                            }
                        }
                    }
                }
            }

            return;
        }

        private async Task PlayAgainYesAsync()
        {
            this.IsBusy = true;

            string message = string.Empty;

            try
            {
                // check_new_game|sessionId|YES
                string req = this.BuildRequestLine(ProtocolCommands.CheckNewGame, this.sessionId, "YES");
                NetworkResult res = await this.SendAsync(req);

                if (res.IsSuccess == false)
                {
                    message = "New game failed: " + res.ErrorMessage;
                }
                else
                {
                    message = "Requested new game.";
                }

                // Re-run start flow (same username)
                await this.StartGameAsync();
            }
            finally
            {
                this.IsBusy = false;
            }

            this.FeedbackMessage = message;
            this.AddLog(message);

            return;
        }

        private async Task PlayAgainNoAsync()
        {
            this.IsBusy = true;

            string message = string.Empty;

            try
            {
                // check_new_game|sessionId|NO
                string req = this.BuildRequestLine(ProtocolCommands.CheckNewGame, this.sessionId, "NO");
                NetworkResult res = await this.SendAsync(req);

                if (res.IsSuccess == false)
                {
                    message = "End session failed: " + res.ErrorMessage;
                }
                else
                {
                    message = "Session ended.";
                }

                this.isGameActive = false;
                this.sessionId = "0";
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
