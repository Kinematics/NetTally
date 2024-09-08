namespace NetTally.Utility.Delegates;
/// <summary>
/// A predicate that adapts one type of object to work
/// with another type.
/// </summary>
/// <typeparam name="T">The type of the main object.</typeparam>
/// <typeparam name="U">The type of the adapted object.</typeparam>
/// <param name="element">The main object.</param>
/// <param name="item">The adapted item</param>
/// <returns><c>True</c> if the predicate passes, otherwise <c>false</c>.</returns>
public delegate bool AdaptPredicate<T, U>(T element, U item);
