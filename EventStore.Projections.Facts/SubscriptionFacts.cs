using Xunit;

namespace EventStore.Projections.Facts
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;
    using Docker;
    using FactCollections;

    [Collection(nameof(EventStoreCollection))]
    public class SubscriptionFacts
    {
        readonly EventStoreRunningInDocker _eventStoreRunningInDocker;

        public SubscriptionFacts(EventStoreRunningInDocker eventStoreRunningInDocker)
        {
            _eventStoreRunningInDocker = eventStoreRunningInDocker;
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(15)]
        public async Task successfully_subscribes_to_a_single_stream_by_id(int eventCount)
        {
            var streamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(streamId, eventCount);
            
            var anotherStreamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(anotherStreamId, eventCount);
            
            var tcs = new TaskCompletionSource<bool>();
            var count = 0;
            
            var subscription = new Subscription(_eventStoreRunningInDocker.Connection);
            subscription.Subscribe(streamId,
                () => Checkpoint.Start,
                CatchUpSubscriptionSettings.Default,
                (s, e) =>
                {
                    if (count > eventCount)
                    {
                        tcs.SetResult(true);
                    }
                    count++;

                    return Task.CompletedTask;
                });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            
            Assert.Equal(eventCount, count);
        }
    }
}