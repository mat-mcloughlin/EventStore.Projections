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
    public class SubscriptionFactsStreamResubscribe
    {
        readonly EventStoreRunningInDocker _eventStoreRunningInDocker;

        readonly ITestOutputHelper _output;

        public SubscriptionFactsStreamResubscribe(EventStoreRunningInDocker eventStoreRunningInDocker,
            ITestOutputHelper output)
        {
            _eventStoreRunningInDocker = eventStoreRunningInDocker;
            _output = output;
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(15)]
        public async Task successfully_resubscribes_to_a_single_stream_by_id(int retryCount)
        {
            var streamId = "test" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.AppendEvent(streamId, new ErrorEvent());

            var tcs = new TaskCompletionSource<bool>();
            var count = -1;
            var calledDropped = false;

            var subscription = new Subscription(_eventStoreRunningInDocker.Connection, retryCount);
            subscription.Subscribe(streamId,
                () => Checkpoint.Start,
                CatchUpSubscriptionSettings.Default,
                (s, e) =>
                {
                    _output.WriteLine($"Processing Event {e.Event.EventNumber}");

                    count++;
                    throw new Exception();
                },
                subscriptionDropped: (r, e) =>
                {
                    calledDropped = true;
                    tcs.SetResult(true);
                });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            
            Assert.Equal(retryCount, count);
            Assert.True(calledDropped);
        }
    }

    public class ErrorEvent
    {
    }
}