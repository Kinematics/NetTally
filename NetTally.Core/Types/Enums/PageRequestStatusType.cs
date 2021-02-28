namespace NetTally.Types.Enums
{
    /// <summary>
    /// Potential status types for page loads.
    /// </summary>
    public enum PageRequestStatusType
    {
        None,
        Requested,
        LoadedFromCache,
        Loaded,
        Retry,
        Error,
        Failed,
        Cancelled,
    }
}
