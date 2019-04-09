using Xunit;

namespace EventStore.Projections.Facts
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;
    using Docker;
    using FactCollections;
    using Xunit.Abstractions;

    [Collection(nameof(EventStoreCollection))]
    public class SubscriptionFactsStreamSubscribe
    {
        readonly EventStoreRunningInDocker _eventStoreRunningInDocker;

        readonly ITestOutputHelper _output;

        public SubscriptionFactsStreamSubscribe(EventStoreRunningInDocker eventStoreRunningInDocker, ITestOutputHelper output)
        {
            _eventStoreRunningInDocker = eventStoreRunningInDocker;
            _output = output;
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(15)]
        public async Task successfully_subscribes_to_a_single_stream_by_id_from_start(int eventCount)
        {
            var streamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(streamId, eventCount);

            var anotherStreamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(anotherStreamId, eventCount);

            var tcs = new TaskCompletionSource<bool>();
            var count = 0;

            var subscription = new Subscription(_eventStoreRunningInDocker.Connection, 0);
            subscription.Subscribe(streamId,
                () => Checkpoint.Start,
                CatchUpSubscriptionSettings.Default,
                (s, e) =>
                {
                    count++;
                    _output.WriteLine($"Processing Event {e.Event.EventNumber}");
                    
                    if (count >= eventCount)
                    {
                        tcs.SetResult(true);
                    }
                    
                    return Task.CompletedTask;
                });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(eventCount, count);
        }

        [Theory]
        [InlineData(5, 2)]
        [InlineData(10, 6)]
        [InlineData(15, 8)]
        public async Task successfully_subscribes_to_a_single_stream_by_id_from_checkpoint(int eventCount, long start)
        {
            var streamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(streamId, eventCount);

            var anotherStreamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.GenerateRandomEvents(anotherStreamId, eventCount);

            var tcs = new TaskCompletionSource<bool>();
            var count = 0;

            var subscription = new Subscription(_eventStoreRunningInDocker.Connection, 0);
            subscription.Subscribe(streamId,
                () => Checkpoint.EventNumber(start),
                CatchUpSubscriptionSettings.Default,
                (s, e) =>
                {
                    count++;
                    _output.WriteLine($"Processing Event {e.Event.EventNumber}");
                    
                    if (count >= eventCount - start - 1)
                    {
                        tcs.SetResult(true);
                    }

                    return Task.CompletedTask;
                });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(eventCount - start - 1, count); // -1 as event number is 0 based.
        }
    }
}