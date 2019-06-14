namespace EventStore.Projections
{
    using System;

    class StreamId
    {
        internal string Id { get; }

        internal static StreamId All => new StreamId("$all");

        internal StreamId(string streamId)
        {
            if (string.IsNullOrWhiteSpace(streamId))
            {
                throw new ArgumentException("streamId must not be null or whitespace");
            }

            Id = streamId;
        }

        public override bool Equals(object obj)
        {
            return obj is StreamId item && Id.Equals(item.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id;
        }
    }
}