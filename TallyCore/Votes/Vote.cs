using System;
using System.Collections.Generic;

namespace NetTally
{
    public class Vote
    {
        string _text = "";

        public HashSet<string> Voters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string Text { get { return _text; } private set { _text = value; Minimized = VoteString.MinimizeVote(value); } }
        public string Minimized { get; private set; }
        public VoteType Type { get; }

        public Vote(string text, VoteType type)
        {
            Text = text;
            Type = type;
        }

        public bool AddSupport(string voter) => Voters.Add(voter);

        public bool RemoveSupport(string voter) => Voters.Remove(voter);

        public bool HasSupport() => Voters.Count > 0;

        public void EditVote(string text)
        {
            Text = text;
        }

        public void MergeFrom(Vote vote)
        {
            foreach (var voter in vote.Voters)
            {
                Voters.Add(voter);
            }
        }
    }
}
