namespace EventStore.Projections
{
    class StreamId
    {
        internal string Id { get; }

        internal static StreamId All => new StreamId("$all");

        internal StreamId(string streamId)
        {
            Id = streamId;
            
            Guard.AgainstNullArgument(nameof(streamId), streamId);
        }
        
        public override bool Equals(object obj)
        {
            return obj is StreamId item && Id.Equals(item.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}