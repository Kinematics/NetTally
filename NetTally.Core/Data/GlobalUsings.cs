global using VotesByVoter = 
    System.Collections.Generic.Dictionary<
        NetTally.Tally.Components.Origin,
        System.Collections.Generic.List<
            NetTally.Tally.Components.VoteLineBlock>>;

global using VoteStorageEntry = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.VoteLineBlock,
    NetTally.Votes.VoterStorage>;
global using VoterStorageEntry = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.Origin,
    NetTally.Tally.Components.VoteLineBlock>;

