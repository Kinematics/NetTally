using System.ComponentModel;

namespace NetTally.CustomEventArgs
{
    /// <summary>
    /// Custom event args class for PropertyChanged events that allows passing
    /// arbitrary data along with the name of the property that changed.
    /// Primarily designed so that the changed data can be passed with the property name.
    /// </summary>
    /// <typeparam name="T">The data type being passed.</typeparam>
    /// <seealso cref="System.ComponentModel.PropertyChangedEventArgs" />
    public class PropertyDataChangedEventArgs<T> : PropertyChangedEventArgs
    {
        public T PropertyData { get; }

        public PropertyDataChangedEventArgs(string propertyName, T propertyData) : base(propertyName)
        {
            PropertyData = propertyData;
        }
    }
}
