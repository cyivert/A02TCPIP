/*
* FILE            : RelayCommand.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-12
* DESCRIPTION     :
*   Basic ICommand implementation for WPF button bindings.
*/

using System;
using System.Windows.Input;

namespace WordGameClient.Utils
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action executeAction;
        private readonly Func<bool>? canExecuteFunc;

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;

                return;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;

                return;
            }
        }

        public RelayCommand(Action executeAction, Func<bool>? canExecuteFunc)
        {
            this.executeAction = executeAction;
            this.canExecuteFunc = canExecuteFunc;

            return;
        }

        public bool CanExecute(object? parameter)
        {
            bool canExecute = true;

            if (this.canExecuteFunc != null)
            {
                canExecute = this.canExecuteFunc.Invoke();
            }

            return (canExecute);
        }

        public void Execute(object? parameter)
        {
            this.executeAction.Invoke();

            return;
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();

            return;
        }
    }
}
