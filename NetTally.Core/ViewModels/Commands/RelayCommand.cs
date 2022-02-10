using System;
using System.ComponentModel;
using System.Windows.Input;

namespace NetTally.ViewModels.Commands
{
    /// <summary>
    /// An abstract container for an <seealso cref="ICommand"/> that allows you to
    /// use commands in an existing class for the Execute and
    /// CanExecute functions rather than create a new class for
    /// each <seealso cref="ICommand"/> you register.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Constructors
        readonly string name;

        /// <summary>
        /// Default constructor with no canExecute check.
        /// </summary>
        /// <param name="execute">The action to execute when requested.</param>
        public RelayCommand(INotifyPropertyChanged viewModel, string name, Action<object?> execute)
            : this(viewModel, name, execute, (arg) => true)
        {
        }

        /// <summary>
        /// Normal class constructor that takes execute and canExecute parameters.
        /// If canExecute is null, stores a lambda which always returns true.
        /// </summary>
        /// <param name="execute">The action to execute when requested.</param>
        /// <param name="canExecute">Function to check whether it's valid to execute the action.</param>
        public RelayCommand(INotifyPropertyChanged viewModel, string name, Action<object?> execute, Func<object?, bool> canExecute)
        {
            this.name = name;
            this.execute = execute;
            this.canExecute = canExecute;

            commandFilter = viewModel as ICommandFilter ?? CommandFilter.Default;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        #endregion

        #region Can Execute Changed Events
        readonly ICommandFilter commandFilter;

        /// <summary>
        /// Handles the PropertyChanged event of the ViewModel control.
        /// Any time the view model sends a property changed notification,
        /// notify any listeners to also update the CanExecute status.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
                return;

            if ((commandFilter.PropertyFilterListMode == PropertyFilterListOption.Exclude
                    && commandFilter.PropertyFilterList.Contains(e.PropertyName))
                || (commandFilter.PropertyFilterListMode == PropertyFilterListOption.IncludeOnly
                    && !commandFilter.PropertyFilterList.Contains(e.PropertyName)))
            {
                return;
            }

            OnCanExecuteChanged();
        }

        /// <summary>
        /// Event handler for notification of a possible change in CanExecute.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Called when [can execute] changed.
        /// </summary>
        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
        #endregion

        #region Can Execute        
        /// <summary>
        /// Function to check whether it's valid to execute the action.
        /// </summary>
        private readonly Func<object?, bool> canExecute;

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public bool CanExecute(object? parameter) => canExecute(parameter);
        #endregion

        #region Execution        
        /// <summary>
        /// The action to execute.
        /// </summary>
        private readonly Action<object?> execute;

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object? parameter) => execute(parameter);
        #endregion
    }
}
