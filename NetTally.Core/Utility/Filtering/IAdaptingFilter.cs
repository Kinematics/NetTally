namespace NetTally.Utility.Filtering;
/// <summary>
/// A filter which can adapt a subtype to be compared to a main type.
/// </summary>
/// <typeparam name="T">The main type of item registered.</typeparam>
/// <typeparam name="U">The sub type which is checked against the main type.</typeparam>
public interface IAdaptingFilter<T, U>
{
    /// <summary>
    /// Determines whether the filter allows the item provided to pass through the filter.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns><c>True</c> if the filter allows the item, or <c>false</c> if not.</returns>
    public bool Allows(U item);

    /// <summary>
    /// Determines whether the filter blocks the item provided.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns><c>True</c> if the filter blocks the item, or <c>false</c> if not.</returns>
    public bool Blocks(U item);
}
