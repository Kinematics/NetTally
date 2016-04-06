using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The thread context that this class was originally created in, so that we
        /// can send notifications back on that same thread.
        /// </summary>
        SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// Make sure we're in the proper synchronization context before sending.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);

            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        /// <summary>
        /// Function to actually invoke the delegate, after synchronization is checked.
        /// </summary>
        /// <param name="param">The parameter.</param>
        private void RaisePropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            PropertyChanged?.Invoke(this, (PropertyChangedEventArgs)param);
        }

        /// <summary>
        /// Generic handling to set property values and raise the property changed event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage">The storage.</param>
        /// <param name="value">The value.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Object.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }
    }
}
