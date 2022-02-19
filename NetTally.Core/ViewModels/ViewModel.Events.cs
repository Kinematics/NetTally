using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using NetTally.CustomEventArgs;

namespace NetTally.ViewModels
{
    /// <summary>
    /// Partial implementation of the ViewModel class.
    /// Handles raising events, including marshalling across threads.
    /// </summary>
    public partial class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The thread context that this class was originally created in, so that we
        /// can send notifications back on that same (UI) thread.
        /// </summary>
        readonly SynchronizationContext? originalSynchronizationContext = SynchronizationContext.Current;

        /// <summary>
        /// A collection of any property changed notification values that should not be
        /// treated as significant by any relay commands.
        /// </summary>
        public HashSet<string> NonCommandPropertyChangedValues { get; } = new HashSet<string>();

        #region INotifyPropertyChanged event
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);

            if (SynchronizationContext.Current == originalSynchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                originalSynchronizationContext?.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == originalSynchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                originalSynchronizationContext?.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// Allow sending property data with the event.
        /// </summary>
        /// <param name="propertyData">The data to pass along with the property name.</param>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyDataChanged<T>(T propertyData, [CallerMemberName] string propertyName = "")
        {
            PropertyDataChangedEventArgs<T> e = new PropertyDataChangedEventArgs<T>(propertyName, propertyData);

            if (SynchronizationContext.Current == originalSynchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                originalSynchronizationContext?.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to actually invoke the delegate, after synchronization is checked.
        /// </summary>
        /// <param name="param">The EventArgs parameter. If called across synchronization
        /// contexts, will be passed as an object.</param>
        private void RaisePropertyChanged(object? param)
        {
            if (param is PropertyChangedEventArgs e)
            {
                // We are in the creator thread, call the base implementation directly.
                PropertyChanged?.Invoke(this, e);
            }
        }
        #endregion

        #region Exception handling event
        /// <summary>
        /// Event for raised exceptions that need to be propagated from the view model to the view.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ExceptionRaised;

        /// <summary>
        /// Function to raise events when an exception has been raised.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="e">The exception that was raised.</param>
        protected ExceptionEventArgs OnExceptionRaised(Exception e)
        {
            ExceptionEventArgs args = new ExceptionEventArgs(e);

            if (SynchronizationContext.Current == originalSynchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaiseExceptionRaised(args);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                originalSynchronizationContext?.Send(RaiseExceptionRaised, args);
            }

            return args;
        }

        /// <summary>
        /// Function to actually invoke the delegate, after synchronization is checked.
        /// </summary>
        /// <param name="param">The parameter.</param>
        private void RaiseExceptionRaised(object? param)
        {
            if (param is ExceptionEventArgs paramArgs)
            {
                // We are in the creator thread, call the base implementation directly
                ExceptionRaised?.Invoke(this, paramArgs);
            }
        }
        #endregion

        #region Generic Property Setting
        /// <summary>
        /// Generic handling to set property values and raise the property changed event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage">A reference to the backing property being set.</param>
        /// <param name="value">The value to be stored.</param>
        /// <param name="propertyName">Name of the property being set.</param>
        /// <returns>Returns true if the value was updated, or false if no change was made.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (storage is IComparable<T> comparableStorage && comparableStorage.CompareTo(value) == 0)
            {
                return false;
            }
            else if (Object.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }
        #endregion
    }
}
