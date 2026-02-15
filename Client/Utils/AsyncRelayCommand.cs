/*
* FILE            : AsyncRelayCommand.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Async ICommand for WPF. Prevents UI freeze and double-click spam.
*/

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WordGameClient.Commands
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> executeFunc;
        private readonly Func<bool>? canExecuteFunc;

        private bool isRunning;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<Task> executeFunc, Func<bool>? canExecuteFunc)
        {
            this.executeFunc = executeFunc;
            this.canExecuteFunc = canExecuteFunc;
            this.isRunning = false;

            return;
        }

        public bool CanExecute(object? parameter)
        {
            bool canExecute = false;

            if (this.isRunning == false)
            {
                canExecute = true;

                if (this.canExecuteFunc != null)
                {
                    canExecute = this.canExecuteFunc.Invoke();
                }
            }

            return (canExecute);
        }

        public async void Execute(object? parameter)
        {
            this.isRunning = true;
            this.RaiseCanExecuteChanged();

            try
            {
                await this.executeFunc.Invoke();
            }
            finally
            {
                this.isRunning = false;
                this.RaiseCanExecuteChanged();
            }

            return;
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler? handler = this.CanExecuteChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            return;
        }
    }
}
