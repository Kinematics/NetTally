namespace NetTally.Models.Mapping;

/// <summary>
/// Class containing mapping function for subclasses of <see cref="Origin"/> objects.
/// </summary>
public static class OriginMapping
{
    extension(Origin origin)
    {
        /// <summary>
        /// Map function that defines how to implement a function that can apply to different
        /// subclasses of <see cref="Origin"/>.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="origin">The <see cref="Origin"/> that this extension method applies to.</param>
        /// <param name="userMap">What to do if the <see cref="Origin"/> is a <see cref="UserOrigin"/></param>
        /// <param name="planMap">What to do if the <see cref="Origin"/> is a <see cref="PlanOrigin"/></param>
        /// <returns>The result of whichever function got applied.</returns>
        /// <exception cref="InvalidOperationException">Will trigger if another subclass is
        /// ever created, but this function hasn't been updated.</exception>
        //public T Map<T>(Func<UserOrigin, T> userMap, Func<PlanOrigin, T> planMap) =>
        //    origin switch
        //    {
        //        UserOrigin userOrigin => userMap(userOrigin),
        //        PlanOrigin planOrigin => planMap(planOrigin),
        //        _ => throw new InvalidOperationException("Unknown Origin type.")
        //    };

        public T Map<T>(
            Func<NoOrigin, T> noOriginFunc,
            Func<UserOrigin, T> userOriginFunc,
            Func<PlanOrigin, T> planOriginFunc) =>
            origin switch
            {
                NoOrigin noOrigin => noOriginFunc(noOrigin),
                UserOrigin userOrigin => userOriginFunc(userOrigin),
                PlanOrigin planOrigin => planOriginFunc(planOrigin),
                _ => throw new NotImplementedException($"Unknown Origin type: {origin.GetType()}")
            };
    }
}

