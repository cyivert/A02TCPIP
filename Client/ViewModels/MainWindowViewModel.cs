/*
* FILE            : MainWindowViewModel.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   ViewModel for the MainWindow UI
*   Stores client-side UI state for the word game screen, including player name, puzzle string, guess word, feedback message, and found words list.
*/

using System.Collections.ObjectModel;
using System.Windows;
using WordGameClient.Models;
using WordGameClient.Utils;

namespace WordGameClient.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly ClientSettings? settings;

        private string playerName;
        private string puzzleString;
        private string guessWord;
        private string feedbackMessage;

        private bool isGameActive;

        private readonly RelayCommand startGameCommand;
        private readonly RelayCommand submitGuessCommand;
        private readonly RelayCommand newGameCommand;

        public string ConfigurationErrorMessage { get; }

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
            get
            {
                string label = "Server: (check App.config)";

                if (this.settings != null)
                {
                    label = "Server: " + this.settings.ServerIp + ":" + this.settings.ServerPort.ToString();
                }

                return (label);
            }
        }

        public string PlayerName
        {
            get { return (this.playerName); }
            set
            {
                this.playerName = value;
                this.NotifyPropertyChanged();
                this.startGameCommand.RaiseCanExecuteChanged();

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
                this.submitGuessCommand.RaiseCanExecuteChanged();

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

        public ObservableCollection<string> FoundWords { get; }

        public RelayCommand StartGameCommand
        {
            get { return (this.startGameCommand); }
        }

        public RelayCommand SubmitGuessCommand
        {
            get { return (this.submitGuessCommand); }
        }

        public RelayCommand NewGameCommand
        {
            get { return (this.newGameCommand); }
        }

        public MainWindowViewModel(ClientSettings? settings, string configurationErrorMessage)
        {
            this.settings = settings;
            this.ConfigurationErrorMessage = configurationErrorMessage;

            this.playerName = string.Empty;
            this.puzzleString = string.Empty;
            this.guessWord = string.Empty;
            this.feedbackMessage = string.Empty;

            this.isGameActive = false;

            this.FoundWords = new ObservableCollection<string>();

            this.startGameCommand = new RelayCommand(this.StartGame, this.CanStartGame);
            this.submitGuessCommand = new RelayCommand(this.SubmitGuess, this.CanSubmitGuess);
            this.newGameCommand = new RelayCommand(this.NewGame, this.CanNewGame);

            return;
        }

        private bool CanStartGame()
        {
            bool canStart = false;

            if ((this.settings != null) &&
                (string.IsNullOrWhiteSpace(this.ConfigurationErrorMessage) == true) &&
                (this.isGameActive == false) &&
                (string.IsNullOrWhiteSpace(this.PlayerName) == false))
            {
                canStart = true;
            }

            return (canStart);
        }

        private void StartGame()
        {
            this.isGameActive = true;

            this.PuzzleString = "Waiting for server puzzle string...";
            this.FeedbackMessage = "UI ready. Next step will request: " + ProtocolCommands.GetStringFromServer;

            this.startGameCommand.RaiseCanExecuteChanged();
            this.submitGuessCommand.RaiseCanExecuteChanged();
            this.newGameCommand.RaiseCanExecuteChanged();

            return;
        }

        private bool CanSubmitGuess()
        {
            bool canSubmit = false;

            if ((this.isGameActive == true) &&
                (string.IsNullOrWhiteSpace(this.GuessWord) == false))
            {
                canSubmit = true;
            }

            return (canSubmit);
        }

        private void SubmitGuess()
        {
            string cleanedGuess = this.GuessWord.Trim();

            if (string.IsNullOrWhiteSpace(cleanedGuess) == true)
            {
                this.FeedbackMessage = "Please enter a word.";
            }
            else
            {
                this.FeedbackMessage =
                    "UI ready. Next step will send: " +
                    ProtocolCommands.WordValidation +
                    " (" + cleanedGuess + ")";
            }

            this.GuessWord = string.Empty;

            return;
        }

        private bool CanNewGame()
        {
            bool canNewGame = false;

            if (this.isGameActive == true)
            {
                canNewGame = true;
            }

            return (canNewGame);
        }

        private void NewGame()
        {
            this.isGameActive = false;

            this.PuzzleString = string.Empty;
            this.GuessWord = string.Empty;
            this.FeedbackMessage = "Game reset. Ready to start again.";

            this.FoundWords.Clear();

            this.startGameCommand.RaiseCanExecuteChanged();
            this.submitGuessCommand.RaiseCanExecuteChanged();
            this.newGameCommand.RaiseCanExecuteChanged();

            return;
        }
    }
}
