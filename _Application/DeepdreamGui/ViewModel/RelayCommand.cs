using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace DeepdreamGui.ViewModel
{
    /// <summary>
    /// Class for handling commands by the view
    /// Derivative by ICommand
    /// see https://msdn.microsoft.com/de-de/library/microsoft.teamfoundation.mvvm.relaycommand(v=vs.110).aspx
    /// </summary>
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        /// <summary>
        /// Currently Inactive
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Relay Command Constructor.
        /// </summary>
        /// <param name="execute">Execute method as action</param>
        /// <param name="canExecute">Method to evaluate if command can be executed</param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Returns if Command can be executed
        /// </summary>
        /// <param name="parameter">Parameter is used to evaluate canExecute</param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        /// <summary>
        /// Execute Command
        /// </summary>
        /// <param name="parameter">Command Parameter</param>
        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
}