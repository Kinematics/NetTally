using System.ComponentModel;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for various partitioning modes to use for breaking votes up into components.
    /// </summary>
    public enum PartitionMode
    {
        [Description("No Partitioning")]
        None,
        [Description("Partition By Line")]
        ByLine,
        [Description("Partition By Line (+Task)")]
        ByLineTask, // obsolete this; partition by line should always carry in parent tasks
        [Description("Partition By Block")]
        ByBlock, // should automatically partition label plans
        [Description("Partition (Plans) By Block")]
        ByBlockAll, // only used to partition content plans
    }
}
