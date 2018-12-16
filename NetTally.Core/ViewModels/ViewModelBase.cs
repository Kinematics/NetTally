﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using NetTally.CustomEventArgs;

namespace NetTally.ViewModels
{
    /// <summary>
    /// Base class for view models in order to contain various syncronization methods for
    /// when properties are changed or events are raised.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The thread context that this class was originally created in, so that we
        /// can send notifications back on that same thread.
        /// </summary>
        readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;

        /// <summary>
        /// A collection of any property changed notification values that should not be
        /// treated as significant by any relay commands.
        /// </summary>
        public HashSet<string> NonCommandPropertyChangedValues { get; } = new HashSet<string>();

        #region INotifyPropertyChanged event
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);

            if (SynchronizationContext.Current == synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// Allow sending property data with the event.
        /// </summary>
        /// <param name="propertyData">The data to pass along with the property name.</param>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyDataChanged<T>(T propertyData, [CallerMemberName] string? propertyName = null)
        {
            PropertyDataChangedEventArgs<T> e = new PropertyDataChangedEventArgs<T>(propertyName, propertyData);

            if (SynchronizationContext.Current == synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to actually invoke the delegate, after synchronization is checked.
        /// </summary>
        /// <param name="param">The parameter.</param>
        private void RaisePropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly.
            PropertyChanged?.Invoke(this, (PropertyChangedEventArgs)param);
        }
        #endregion

        #region Exception handling event
        /// <summary>
        /// Event for raised exceptions that need to be propagated from the view model to the view.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionRaised;

        /// <summary>
        /// Function to raise events when an exception has been raised.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="e">The exception that was raised.</param>
        protected ExceptionEventArgs OnExceptionRaised(Exception e)
        {
            ExceptionEventArgs args = new ExceptionEventArgs(e);

            if (SynchronizationContext.Current == synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaiseExceptionRaised(args);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                synchronizationContext.Send(RaiseExceptionRaised, args);
            }

            return args;
        }

        /// <summary>
        /// Function to actually invoke the delegate, after synchronization is checked.
        /// </summary>
        /// <param name="param">The parameter.</param>
        private void RaiseExceptionRaised(object param)
        {
            // We are in the creator thread, call the base implementation directly
            ExceptionRaised?.Invoke(this, (ExceptionEventArgs)param);
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
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Object.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }
        #endregion
    }
}
