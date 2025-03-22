// Dictionary lookup of votes by each voter

// Individual dictionary element from VoteStorageF:
// List of VoterStorage elements (ordered):
global using OrderedVoterStorageF = System.Collections.Generic.List<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Posts.Origin,
        NetTally.Tally.Components.Votes.VoteBlockType>>;
// Individual dictionary element from VoterStorageF:
global using VoterStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.Posts.Origin,
    NetTally.Tally.Components.Votes.VoteBlockType>;
// Enumeration of VoterStorage elements:
global using VoterStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Posts.Origin,
        NetTally.Tally.Components.Votes.VoteBlockType>>;
// Dictionary lookup of votes by each voter
global using VotesByVoterF =
    System.Collections.Generic.Dictionary<
        NetTally.Tally.Components.Posts.Origin,
        System.Collections.Generic.List<NetTally.Tally.Components.Votes.VoteBlockType>>;
// Grouping of VoteStorage elements by task:
global using VotesGroupedByTaskF = System.Linq.IGrouping<
    NetTally.Tally.Components.Votes.VoteTaskType,
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Votes.VoteBlockType,
        NetTally.Tally.Components.Storage.VoterStorage>>;
global using VoteStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.Components.Votes.VoteBlockType,
    NetTally.Tally.Components.Storage.VoterStorage>;
// Enumeration of VoteStorage elements:
global using VoteStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.Components.Votes.VoteBlockType,
        NetTally.Tally.Components.Storage.VoterStorage>>;

global using TextFilter = NetTally.Utility.Filtering.IItemFilter<string>;
global using PostNumFilter = NetTally.Utility.Filtering.IAdaptingFilter<System.Range, long>;

