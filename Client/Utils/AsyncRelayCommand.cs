/*
* FILE            : AsyncRelayCommand.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Async ICommand implementation for WPF bindings.
*/

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WordGameClient.Commands
{
    //
    // CLASS : AsyncRelayCommand
    // DESCRIPTION : An asynchronous implementation of ICommand that wraps an async delegate.
    //               It disables itself while the async operation is running and uses
    //               CommandManager to refresh the UI's command state.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> executeFunc;
        private readonly Func<bool>? canExecuteFunc;

        private bool isRunning;

        //
        // EVENT : CanExecuteChanged
        // DESCRIPTION : Raised when the command's ability to execute may have changed.
        //               Relays the event to CommandManager.RequerySuggested to automatically
        //               refresh command state in WPF.
        // PARAMETERS : value - event handler to add or remove.
        // RETURNS : n/a (event accessor)
        //

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        //
        // FUNCTION : AsyncRelayCommand (constructor)
        // DESCRIPTION : Initializes a new instance of the AsyncRelayCommand with the specified execute and canExecute delegates.
        // PARAMETERS : Func<Task> executeFunc - the asynchronous method to invoke;
        //              Func<bool>? canExecuteFunc - optional synchronous predicate to control command availability.
        // RETURNS : n/a
        //
        public AsyncRelayCommand(Func<Task> executeFunc, Func<bool>? canExecuteFunc)
        {
            this.executeFunc = executeFunc;
            this.canExecuteFunc = canExecuteFunc;
            this.isRunning = false;

            return;
        }

        //
        // FUNCTION : CanExecute
        // DESCRIPTION : Determines whether the command can execute in its current state.
        //               Returns false if an async operation is already running; otherwise,
        //               evaluates the optional canExecuteFunc (or true if none provided).
        // PARAMETERS : object? parameter - command parameter (ignored in this implementation)
        // RETURNS : bool - true if the command can execute; otherwise false.
        //

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

        //
        // FUNCTION : Execute
        // DESCRIPTION : Executes the asynchronous command. Sets isRunning to true,
        //               triggers a requery of command state, awaits the execute delegate,
        //               and finally resets isRunning and triggers another requery.
        //               Note: This method is "fire and forget" due to ICommand's void return,
        //               but exceptions are properly propagated via the task.
        // PARAMETERS : object? parameter - command parameter (ignored)
        // RETURNS : void
        //

        public async void Execute(object? parameter)
        {
            this.isRunning = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await this.executeFunc.Invoke();
            }
            finally
            {
                this.isRunning = false;
                CommandManager.InvalidateRequerySuggested();
            }

            return;
        }
    }
}
