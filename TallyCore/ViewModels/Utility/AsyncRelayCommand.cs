using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetTally.ViewModels
{
    /// <summary>
    /// Interface for the asynchronous portion of the AsyncRelayCommand.
    /// </summary>
    public interface IAsyncCommand
    {
        Task ExecuteAsync(object obj);
        bool CanExecute(object obj);
    }

    /// <summary>
    /// An asynchronous version of the ICommand RelayCommand.
    /// </summary>
    /// <seealso cref="NetTally.ViewModels.IAsyncCommand" />
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class AsyncRelayCommand : IAsyncCommand, ICommand
    {
        private readonly Func<object, Task> execute;
        private readonly Func<object, bool> canExecute;
        private bool isExecuting;
        public event EventHandler CanExecuteChanged;

        #region Constructors
        /// <summary>
        /// Default constructor with no canExecute check.
        /// </summary>
        /// <param name="executeAsync">The action to execute when requested.</param>
        public AsyncRelayCommand(ViewModelBase viewModel, Func<object, Task> executeAsync)
            : this(viewModel, executeAsync, null)
        {
        }

        /// <summary>
        /// Normal class constructor that takes execute and canExecute parameters.
        /// </summary>
        /// <param name="executeAsync">The action to execute when requested.</param>
        /// <param name="canExecute">Function to check whether it's valid to execute the action.</param>
        public AsyncRelayCommand(ViewModelBase viewModel, Func<object, Task> executeAsync, Func<object, bool> canExecute)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));
            if (executeAsync == null)
                throw new ArgumentNullException(nameof(executeAsync));

            this.execute = executeAsync;
            this.canExecute = canExecute;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        #endregion

        #region Execution code.        
        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// Ignore warning about async void.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns></returns>
        public async Task ExecuteAsync(object parameter)
        {
            try
            {
                isExecuting = true;
                OnCanExecuteChanged();
                await execute(parameter);
            }
            finally
            {
                isExecuting = false;
                OnCanExecuteChanged();
            }
        }
        #endregion

        #region Other stuff
        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public bool CanExecute(object parameter) => !isExecuting && (canExecute == null || canExecute(parameter));

        /// <summary>
        /// Handles the PropertyChanged event of the ViewModel control.
        /// Any time the view model sends a property changed notification,
        /// notify any listeners to also update the CanExecute status.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        /// Called when [can execute] changed.
        /// </summary>
        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
        #endregion
    }
}
