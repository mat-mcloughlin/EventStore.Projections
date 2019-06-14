namespace EventStore.Projections
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using ClientAPI;

    class Subscription
    {
        readonly string _subscriptionName;

        readonly ISubscriber _subscriber;

        readonly IProjectionStateRepository _projectionStateRepository;

        Checkpoint _checkpoint;

        internal Subscription(string subscriptionName, ISubscriber subscriber, IProjectionStateRepository projectionStateRepository)
        {
            Guard.AgainstNullArgument(nameof(subscriber), subscriber);
            Guard.AgainstNullArgument(nameof(projectionStateRepository), projectionStateRepository);

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("subscriptionName must not be null or whitespace");
            }
            
            _subscriptionName = subscriptionName;
            _subscriber = subscriber;
            _projectionStateRepository = projectionStateRepository;
        }

        internal void Start(StreamId streamId, CatchUpSubscriptionSettings settings, Action<PipelineContext> pipeline)
        {
            _checkpoint = Checkpoint.Start;
            
            _subscriber.Subscribe(streamId,
                () => _checkpoint,
                settings,
                e =>
                {
                    pipeline(new PipelineContext(e));
                    _checkpoint = new Checkpoint(e.OriginalPosition, e.OriginalEventNumber);
                    _projectionStateRepository.UpdateCheckpoint(_subscriptionName, _checkpoint);
                    return Task.CompletedTask;
                });
        }

        internal void Stop()
        {
            _subscriber.Stop();
        }

        internal void Reset()
        {
            _checkpoint = new Checkpoint(Position.Start, 0);
            _subscriber.Restart();
        }

        internal void Restart()
        {
            _subscriber.Restart();
        }
    }

    interface IProjectionStateRepository
    {
        Task UpdateCheckpoint(string projectionName, Checkpoint checkpoint);
    }
}