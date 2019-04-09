namespace EventStore.Projections.Facts
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;
    using Docker;
    using FactCollections;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(nameof(EventStoreCollection))]
    public class SubscriberFactsStreamResubscribe
    {
        readonly EventStoreRunningInDocker _eventStoreRunningInDocker;

        readonly ITestOutputHelper _output;

        public SubscriberFactsStreamResubscribe(EventStoreRunningInDocker eventStoreRunningInDocker,
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
            var streamId = "test-" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.AppendRandomEvents(streamId, 1);

            var tcs = new TaskCompletionSource<bool>();
            var count = -1;
            var calledDropped = false;

            var subscriber = new Subscriber(_eventStoreRunningInDocker.Connection,
                LoggingAdaptor.Empty,
                new RetryPolicy(a => TimeSpan.Zero, retryCount));
            subscriber.Subscribe(new StreamId(streamId),
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

        [Fact]
        public async Task successfully_resets_retry_to_a_single_stream_by_id()
        {
            var streamId = "test-" + Guid.NewGuid().ToString("N");
            await _eventStoreRunningInDocker.AppendRandomEvents(streamId, 30);

            var tcs = new TaskCompletionSource<bool>();
            var count = -1;
            var initialRetryCount = 0;
            var finalRetryCount = -1; // To compensate for final retry incrementing it.
            var calledDropped = false;
            var inMemoryCheckpoint = new InMemoryCheckpoint();

            var subscriber = new Subscriber(_eventStoreRunningInDocker.Connection,
                LoggingAdaptor.Empty,
                new RetryPolicy(a => TimeSpan.Zero, 5));
            subscriber.Subscribe(new StreamId(streamId),
                () => inMemoryCheckpoint.GetCheckpint,
                CatchUpSubscriptionSettings.Default,
                (s, e) =>
                {
                    _output.WriteLine($"Processing Event {e.Event.EventNumber}");

                    count++;
                    if (count < 10 && count > 5) // Throw 5 times
                    {
                        initialRetryCount++;
                        throw new Exception();
                    }

                    if (count >= 15) // Another 5 times
                    {
                        finalRetryCount++;
                        throw new Exception();
                    }

                    inMemoryCheckpoint.SetCheckpoint(e.Event.EventNumber);
                    return Task.CompletedTask;
                },
                subscriptionDropped: (r, e) =>
                {
                    calledDropped = true;
                    tcs.SetResult(true);
                });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));

            Assert.Equal(4, initialRetryCount);
            Assert.Equal(5, finalRetryCount);
            Assert.True(calledDropped);
        }
    }

    class InMemoryCheckpoint
    {
        long? _checkpoint = 0;

        internal void SetCheckpoint(long? checkpoint)
        {
            _checkpoint = checkpoint;
        }

        internal Checkpoint GetCheckpint => Checkpoint.FromEventNumber(_checkpoint);
    }
}