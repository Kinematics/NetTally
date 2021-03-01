using System.ComponentModel;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for various modes of displaying the tally results.
    /// </summary>
    public enum DisplayMode
    {
        [Description("Normal")]
        Normal,
        [Description("Spoiler Voters")]
        SpoilerVoters,
        [Description("Spoiler All")]
        SpoilerAll,
        [Description("Normal, No Voters")]
        NormalNoVoters,
        [Description("Compact")]
        Compact,
        [Description("Compact, No Voters")]
        CompactNoVoters
    }
}
