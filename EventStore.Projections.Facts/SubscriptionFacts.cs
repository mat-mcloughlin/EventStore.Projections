namespace EventStore.Projections.Facts
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;
    using Docker;
    using FactCollections;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(nameof(EventStoreCollection))]
    public class SubscriptionFacts
    {
        readonly EventStoreRunningInDocker _eventStoreRunningInDocker;


        public SubscriptionFacts(EventStoreRunningInDocker eventStoreRunningInDocker)
        {
            _eventStoreRunningInDocker = eventStoreRunningInDocker;
        }
        
        [Fact]
        public async Task subscription_starts_subscriber()
        {
            var streamId = "test-" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.AppendRandomEvents(streamId, 5);
            var tcs = new TaskCompletionSource<bool>();
            var success = false;
            
            var subscriber = new Subscriber(_eventStoreRunningInDocker.Connection, LoggingAdaptor.Empty, RetryPolicy.None);
            var subscription = new Subscription("test", subscriber, new InMemoryProjectionStateRepository());
            
            subscription.Start(new StreamId(streamId),CatchUpSubscriptionSettings.Default, r =>
            {
                if (!r.Event.Event.EventType.StartsWith("$"))
                {
                    success = true;
                    tcs.SetResult(true);
                }
            });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));

            Assert.True(success);
        }
        
        [Fact]
        public async Task subscription_updates_checkpoint()
        {
            var streamId = "test-" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.AppendRandomEvents(streamId, 5);
            var inMemoryProjectionStateRepository = new InMemoryProjectionStateRepository();
            var tcs = new TaskCompletionSource<bool>();
            
            var subscriber = new Subscriber(_eventStoreRunningInDocker.Connection, LoggingAdaptor.Empty, new RetryPolicy(i => TimeSpan.Zero, 2));
            var subscription = new Subscription("test", subscriber, new InMemoryProjectionStateRepository());
            
            subscription.Start(new StreamId(streamId),CatchUpSubscriptionSettings.Default, r =>
            {
                if (!r.Event.Event.EventType.StartsWith("$"))
                {
                    if (r.Event.OriginalEventNumber == 3)
                    {
                        tcs.SetResult(true);
                    }
                }
            });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));

            var position = inMemoryProjectionStateRepository.Checkpoint.ToPosition();
            Assert.Equal(1, position.Value.CommitPosition);
            Assert.Equal(1, position.Value.PreparePosition);
        }
    }
}