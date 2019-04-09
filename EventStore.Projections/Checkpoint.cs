namespace EventStore.Projections
{
    using ClientAPI;

    struct Checkpoint
    {
        readonly long? _commitPosition;
        
        readonly long? _preparePosition;
        
        Checkpoint(long? eventNumber)
        {
            _commitPosition = eventNumber;
            _preparePosition = 0;
        }

        internal Position? ToPosition()
        {
            return new Position(0L, 0L);
        }

        internal long? ToEventNumber()
        {
            return _commitPosition;
        }

        internal static Checkpoint Start => new Checkpoint(null);

        internal static Checkpoint FromEventNumber(long? eventNumber)
        {    
            return new Checkpoint(eventNumber);
        }
    }
}