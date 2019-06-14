namespace EventStore.Projections
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;

    class CurrentSubscription
    {
        internal CurrentSubscription(StreamId streamId,
            Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<ResolvedEvent, Task> eventAppeared,
            Action liveProcessingStarted,
            Action<SubscriptionDropReason, Exception> subscriptionDropped)
        {
            StreamId = streamId;
            Checkpoint = checkpoint;
            Settings = settings;
            EventAppeared = eventAppeared;
            LiveProcessingStarted = liveProcessingStarted;
            SubscriptionDropped = subscriptionDropped;
        }

        internal StreamId StreamId { get; }

        internal Func<Checkpoint> Checkpoint { get; }

        internal CatchUpSubscriptionSettings Settings { get; }

        internal Func<ResolvedEvent, Task> EventAppeared { get; }

        internal Action LiveProcessingStarted { get; }

        internal Action<SubscriptionDropReason, Exception> SubscriptionDropped { get; }
    }
}