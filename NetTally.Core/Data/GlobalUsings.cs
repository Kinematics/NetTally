// Dictionary lookup of votes by each voter
global using VotesByVoter = 
    System.Collections.Generic.Dictionary<
        NetTally.Tally.Components.Origin,
        System.Collections.Generic.List<
            NetTally.Tally.Components.VoteLineBlock>>;

// Individual dictionary element from VoteStorage:
global using VoteStorageEntry = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.VoteLineBlock,
    NetTally.Votes.VoterStorage>;

// Individual dictionary element from VoterStorage:
global using VoterStorageEntry = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.Origin,
    NetTally.Tally.Components.VoteLineBlock>;

// List of VoterStorage elements:
global using OrderedVoterStorage = System.Collections.Generic.List<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Origin, NetTally.Tally.Components.VoteLineBlock>>;

// Grouping of VoteStorage elements by task:
global using VotesGroupedByTask = System.Linq.IGrouping<
    string, System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.VoteLineBlock, NetTally.Votes.VoterStorage>>;

global using FilteredVoterStorage = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Origin, NetTally.Tally.Components.VoteLineBlock>>;
