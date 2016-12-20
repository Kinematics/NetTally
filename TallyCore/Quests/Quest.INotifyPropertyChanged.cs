using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetTally
{
    /// <summary>
    /// Implement <see cref="INotifyPropertyChanged"/> for <see cref="NetTally.Quest"/> class.
    /// </summary>
    public partial class Quest : IQuest
    {
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == "ThreadName")
            {
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
