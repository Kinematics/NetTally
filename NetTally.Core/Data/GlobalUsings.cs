global using VotesByVoter = 
    System.Collections.Generic.Dictionary<
        NetTally.Tally.Components.Origin,
        System.Collections.Generic.List<
            NetTally.Tally.Components.VoteLineBlock>>;
global using VoteStorageEntry = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.VoteLineBlock,
    NetTally.Votes.VoterStorage>;
