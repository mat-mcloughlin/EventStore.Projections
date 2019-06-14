namespace EventStore.Projections
{
    using System;
    using ClientAPI;

    public struct Checkpoint
    {
        readonly long? _commitPosition;

        readonly long? _preparePosition;

        internal Checkpoint(Position? position, long eventNumber)
        {
            if (position == null)
            {
                _commitPosition = eventNumber;
                _preparePosition = eventNumber;
            }
            else
            {
                _commitPosition = position.Value.CommitPosition;
                _preparePosition = position.Value.PreparePosition;
            }
        }

        internal Position? ToPosition()
        {
            return new Position(0L, 0L);
        }

        internal long? ToEventNumber()
        {
            return _commitPosition;
        }

        internal static Checkpoint Start => new Checkpoint();

        internal static Checkpoint FromEventNumber(long eventNumber)
        {    
            return new Checkpoint(null, eventNumber);
        }

        public override string ToString()
        {
            return $"{_commitPosition}/{_preparePosition}";
        }
    }
}