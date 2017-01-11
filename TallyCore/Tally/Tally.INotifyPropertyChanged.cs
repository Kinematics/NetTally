using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Implement <see cref="INotifyPropertyChanged"/> for <see cref="Tally"/> class.
    /// </summary>
    public partial class Tally : INotifyPropertyChanged
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
