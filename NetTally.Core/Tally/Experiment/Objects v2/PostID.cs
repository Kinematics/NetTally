using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    public enum PostIDFormat
    {
        Number
    }

    class PostID
    {
        public string ID { get; }
        public int Value { get; }

        public PostID(string id, PostIDFormat format = PostIDFormat.Number)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            ID = id;

            switch (format)
            {
                case PostIDFormat.Number:
                    Value = int.Parse(id);
                    if (Value < 1)
                        throw new ArgumentOutOfRangeException(nameof(id), $"Post ID ({id}) must be a positive value.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), $"Unknown post ID format: {format}.");
            }
        }
    }
}
