/*
* FILE            : RelayCommand.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   ICommand implementation for WPF bindings.
*/

using System;
using System.Windows.Input;

namespace WordGameClient.Commands
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action executeAction;
        private readonly Func<bool>? canExecuteFunc;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
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

            CommandManager.InvalidateRequerySuggested();

            return;
        }
    }
}
