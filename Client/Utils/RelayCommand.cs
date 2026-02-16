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
    //
    // CLASS : RelayCommand
    // DESCRIPTION : A simple implementation of ICommand that wraps synchronous delegates.
    //               It uses CommandManager to automatically refresh command state in WPF.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class RelayCommand : ICommand
    {
        private readonly Action executeAction;
        private readonly Func<bool>? canExecuteFunc;

        //
        // EVENT : CanExecuteChanged
        // DESCRIPTION : Raised when the command's ability to execute may have changed.
        //               Relays the event to CommandManager.RequerySuggested so that WPF
        //               automatically refreshes the state of any bound UI elements.
        // PARAMETERS : value - event handler to add or remove.
        // RETURNS : n/a (event accessor)
        //

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        //
        // FUNCTION : RelayCommand (constructor)
        // DESCRIPTION : Initializes a new instance of the RelayCommand with the specified execute and canExecute delegates.
        // PARAMETERS : Action executeAction - the method to invoke when the command is executed;
        //              Func<bool>? canExecuteFunc - optional predicate that controls command availability.
        // RETURNS : n/a
        //

        public RelayCommand(Action executeAction, Func<bool>? canExecuteFunc)
        {
            this.executeAction = executeAction;
            this.canExecuteFunc = canExecuteFunc;

            return;
        }

        //
        // FUNCTION : CanExecute
        // DESCRIPTION : Determines whether the command can execute in its current state.
        //               Evaluates the optional canExecuteFunc, or returns true if none is provided.
        // PARAMETERS : object? parameter - command parameter (ignored in this implementation)
        // RETURNS : bool - true if the command can execute; otherwise false.
        //

        public bool CanExecute(object? parameter)
        {
            bool canExecute = true;

            if (this.canExecuteFunc != null)
            {
                canExecute = this.canExecuteFunc.Invoke();
            }

            return (canExecute);
        }

        //
        // FUNCTION : Execute
        // DESCRIPTION : Executes the command by invoking the wrapped Action.
        //               After execution, triggers a requery of command state to update any
        //               UI elements that depend on CanExecute.
        // PARAMETERS : object? parameter - command parameter (ignored)
        // RETURNS : void
        //

        public void Execute(object? parameter)
        {
            this.executeAction.Invoke();

            CommandManager.InvalidateRequerySuggested();

            return;
        }
    }
}
