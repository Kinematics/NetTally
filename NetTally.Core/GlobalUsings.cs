// Dictionary lookup of votes by each voter

// Individual dictionary element from VoteStorageF:
// List of VoterStorage elements (ordered):
global using OrderedVoterStorageF = System.Collections.Generic.List<
    System.Collections.Generic.KeyValuePair<
        NetTally.Models.Posts.Origin,
        NetTally.Models.Votes.VoteBlock>>;
global using PostNumFilter = NetTally.Utility.Filtering.IAdaptingFilter<System.Range, long>;
global using TextFilter = NetTally.Utility.Filtering.IItemFilter<string>;
// Individual dictionary element from VoterStorageF:
global using VoterStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Models.Posts.Origin,
    NetTally.Models.Votes.VoteBlock>;
// Enumeration of VoterStorage elements:
global using VoterStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Models.Posts.Origin,
        NetTally.Models.Votes.VoteBlock>>;
// Dictionary lookup of votes by each voter
global using VotesByVoterF =
    System.Collections.Generic.Dictionary<
        NetTally.Models.Posts.Origin,
        System.Collections.Generic.List<NetTally.Models.Votes.VoteBlock>>;
// Grouping of VoteStorage elements by task:
global using VotesGroupedByTaskF = System.Linq.IGrouping<
    NetTally.Models.Votes.VoteTask,
    System.Collections.Generic.KeyValuePair<
        NetTally.Models.Votes.VoteBlock,
        NetTally.Tally.Storage.VoterStorage>>;
global using VoteStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Models.Votes.VoteBlock,
    NetTally.Tally.Storage.VoterStorage>;
// Enumeration of VoteStorage elements:
global using VoteStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Models.Votes.VoteBlock,
        NetTally.Tally.Storage.VoterStorage>>;

