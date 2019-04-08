namespace EventStore.Projections
{
    using ClientAPI;

    struct Checkpoint
    {
        internal Position? ToPosition()
        {
            return new Position();
        }

        internal long? ToEventNumber()
        {
            return null;
        }

        public static Checkpoint Start => new Checkpoint();
    }
}