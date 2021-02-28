using NetTally.Attributes;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for various modes of displaying the tally results.
    /// </summary>
    public enum DisplayMode
    {
        [EnumDescription("Normal")]
        Normal,
        [EnumDescription("Spoiler Voters")]
        SpoilerVoters,
        [EnumDescription("Spoiler All")]
        SpoilerAll,
        [EnumDescription("Normal, No Voters")]
        NormalNoVoters,
        [EnumDescription("Compact")]
        Compact,
        [EnumDescription("Compact, No Voters")]
        CompactNoVoters
    }
}
