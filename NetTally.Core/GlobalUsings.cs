// Dictionary lookup of votes by each voter

// Individual dictionary element from VoteStorageF:
global using VoteStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.ComponentsF.Votes.VoteBlockType,
    NetTally.Tally.ComponentsF.Storage.VoterStorage>;

// Individual dictionary element from VoterStorageF:
global using VoterStorageEntryF = System.Collections.Generic.KeyValuePair<
    NetTally.Tally.ComponentsF.Posts.OriginType,
    NetTally.Tally.ComponentsF.Votes.VoteBlockType>;

// Enumeration of VoteStorage elements:
global using VoteStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.ComponentsF.Votes.VoteBlockType,
        NetTally.Tally.ComponentsF.Storage.VoterStorage>>;

// Enumeration of VoterStorage elements:
global using VoterStorageType = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.ComponentsF.Posts.OriginType,
        NetTally.Tally.ComponentsF.Votes.VoteBlockType>>;

// List of VoterStorage elements (ordered):
global using OrderedVoterStorageF = System.Collections.Generic.List<
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.ComponentsF.Posts.OriginType,
        NetTally.Tally.ComponentsF.Votes.VoteBlockType>>;

// Dictionary lookup of votes by each voter
global using VotesByVoterF =
    System.Collections.Generic.Dictionary<
        NetTally.Tally.ComponentsF.Posts.OriginType,
        System.Collections.Generic.List<NetTally.Tally.ComponentsF.Votes.VoteBlockType>>;

// Grouping of VoteStorage elements by task:
global using VotesGroupedByTaskF = System.Linq.IGrouping<
    NetTally.Tally.ComponentsF.Votes.VoteTaskType,
    System.Collections.Generic.KeyValuePair<
        NetTally.Tally.ComponentsF.Votes.VoteBlockType,
        NetTally.Tally.ComponentsF.Storage.VoterStorage>>;

