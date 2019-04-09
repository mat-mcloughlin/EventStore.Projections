namespace EventStore.Projections
{
    using ClientAPI;

    struct Checkpoint
    {
        readonly long? _commitPosition;
        
        Checkpoint(long? eventNumber)
        {
            _commitPosition = eventNumber;
        }

        internal Position? ToPosition()
        {
            return new Position();
        }

        internal long? ToEventNumber()
        {
            return _commitPosition;
        }

        internal static Checkpoint Start => new Checkpoint(null);

        internal static Checkpoint EventNumber(long eventNumber)
        {
            return new Checkpoint(eventNumber);
        }
    }
}