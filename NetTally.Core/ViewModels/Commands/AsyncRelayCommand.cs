using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetTally.ViewModels.Commands
{
    /// <summary>
    /// Interface for the asynchronous portion of the <see cref="AsyncRelayCommand"/>.
    /// </summary>
    public interface IAsyncCommand
    {
        Task ExecuteAsync(object? value);
        bool CanExecute(object? value);
    }

    /// <summary>
    /// An asynchronous version of the <seealso cref="RelayCommand" />.
    /// </summary>
    /// <seealso cref="IAsyncCommand" />
    /// <seealso cref="RelayCommand" />
    /// <seealso cref="ICommand" />
    public class AsyncRelayCommand : IAsyncCommand, ICommand
    {
        #region Constructors
        readonly string name;

        /// <summary>
        /// Default constructor with no canExecute check.
        /// </summary>
        /// <param name="executeAsync">The action to execute when requested.</param>
        public AsyncRelayCommand(INotifyPropertyChanged viewModel, string name, Func<object?, Task> executeAsync)
            : this(viewModel, name, executeAsync, (arg) => true)
        {
        }

        /// <summary>
        /// Normal class constructor that takes execute and canExecute parameters.
        /// </summary>
        /// <param name="executeAsync">The action to execute when requested.</param>
        /// <param name="canExecute">Function to check whether it's valid to execute the action.</param>
        public AsyncRelayCommand(INotifyPropertyChanged viewModel, string name, Func<object?, Task> executeAsync, Func<object?, bool> canExecute)
        {
            this.name = name;
            this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));

            commandFilter = viewModel as ICommandFilter ?? CommandFilter.Default;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        #endregion

        #region CanExecuteChanged event handling
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
        public bool CanExecute(object? parameter) => !isExecuting && canExecute(parameter);
        #endregion

        #region Execution
        /// <summary>
        /// The action to execute.
        /// </summary>
        private readonly Func<object?, Task> executeAsync;

        private bool isExecuting;

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// Ignore warning about async void.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not
        /// require data to be passed, this object can be set to null.</param>
        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns></returns>
        public async Task ExecuteAsync(object? parameter)
        {
            if (isExecuting)
                return;

            try
            {
                isExecuting = true;
                OnCanExecuteChanged();
                await executeAsync(parameter);
            }
            finally
            {
                isExecuting = false;
                OnCanExecuteChanged();
            }
        }
        #endregion
    }
}
