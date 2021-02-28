using NetTally.Attributes;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for various partitioning modes to use for breaking votes up into components.
    /// </summary>
    public enum PartitionMode
    {
        [EnumDescription("No Partitioning")]
        None,
        [EnumDescription("Partition By Line")]
        ByLine,
        [EnumDescription("Partition By Line (+Task)")]
        ByLineTask, // obsolete this; partition by line should always carry in parent tasks
        [EnumDescription("Partition By Block")]
        ByBlock, // should automatically partition label plans
        [EnumDescription("Partition (Plans) By Block")]
        ByBlockAll, // only used to partition content plans
    }
}
