using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.Votes
{
    /// <summary>
    /// Used in conjunction with <seealso cref="VoteStorage"/>, for
    /// keeping track of voters and their associated votes.
    /// VoterStorage is a dictionary of voter origins and the vote
    /// each submitted.
    /// </summary>
    public class VoterStorage : Dictionary<Origin, VoteLineBlock>
    {
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public VoterStorage()
        { }

        /// <summary>
        /// Constructor that allows copying from an existing instance.
        /// </summary>
        /// <param name="copyFrom">The VoterStorage instance to copy from.</param>
        public VoterStorage(VoterStorage copyFrom)
            : base(copyFrom)
        { }

        /// <summary>
        /// Constructor that allows copying from the base dictionary class
        /// that VoterStorage is based on.
        /// </summary>
        /// <param name="copyFrom">The Dictionary instance to copy from.</param>
        public VoterStorage(Dictionary<Origin, VoteLineBlock> copyFrom)
            : base(copyFrom)
        { }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Special lookup for the origin keys stored in the VoterStorage dictionary.
        /// </summary>
        readonly HashSet<Origin> NameLookup = new HashSet<Origin>();
        #endregion Properties

        #region Reset
        public void Reset()
        {
            this.Clear();
            NameLookup.Clear();
        }
        #endregion

        #region Override Add/Remove functions
        /// <summary>
        /// Define the indexer to allow client code to use [] notation. 
        /// </summary>
        /// <param name="index">The index into the storage array.</param>
        /// <returns>Returns the element found at the given index.</returns>
        public new VoteLineBlock this[Origin index]
        {
            get
            {
                return base[index];
            }
            set
            {
                NameLookup.Add(index);
                base[index] = value;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the storage.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <param name="value">Stored value.</param>
        public new void Add(Origin key, VoteLineBlock value)
        {
            NameLookup.Add(key);
            base.Add(key, value);
        }

        /// <summary>
        /// Tries to add the specified key and value to the storage.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <param name="value">Stored value.</param>
        /// <returns>Returns true if successfully added. Returns false if the key already exists.</returns>
        public new bool TryAdd(Origin key, VoteLineBlock value)
        {
            if (NameLookup.Add(key))
            {
                return base.TryAdd(key, value);
            }

            return false;
        }

        /// <summary>
        /// Remove the specified key from storage.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <returns>Returns true if the key was found in the collection.</returns>
        public new bool Remove(Origin key)
        {
            NameLookup.Remove(key);
            return base.Remove(key);
        }

        /// <summary>
        /// Removes the specified key from storage.
        /// Returns the corresponding value as an out parameter.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns>Returns true if the key was found in the collection.</returns>
        public new bool Remove(Origin key, out VoteLineBlock value)
        {
            NameLookup.Remove(key);
            return base.Remove(key, out value);
        }
        #endregion Override Add/Remove functions

        #region Queries
        /// <summary>
        /// Check whether a given origin matches any voters stored in this lookup.
        /// </summary>
        /// <param name="origin">The origin to compare to.</param>
        /// <returns>Returns true if the origin exists in this lookup.</returns>
        public bool HasIdentity(Origin origin)
        {
            return NameLookup.Contains(origin);
        }

        /// <summary>
        /// Check whether a given plan name exists in the voters stored in this lookup.
        /// </summary>
        /// <param name="planName">The name of the plan to check for.</param>
        /// <returns>Returns true if the plan name can be found in this lookup.</returns>
        public bool HasPlan(string planName)
        {
            return NameLookup.Contains(new Origin(planName, IdentityType.Plan));
        }

        /// <summary>
        /// Check whether a given voter name exists in the voters stored in this lookup.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns true if the voter name can be found in this lookup.</returns>
        public bool HasVoter(string voterName)
        {
            return NameLookup.Contains(new Origin(voterName, IdentityType.User));
        }
        #endregion Queries
    }
}
