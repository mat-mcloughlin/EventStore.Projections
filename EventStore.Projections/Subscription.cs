namespace EventStore.Projections
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;

    class Subscription
    {
        readonly IEventStoreConnection _eventStoreConnection;

        internal Subscription(IEventStoreConnection eventStoreConnection)
        {
            _eventStoreConnection = eventStoreConnection;

            Guard.AgainstNullArgument(nameof(eventStoreConnection), eventStoreConnection);
        }

        internal void Subscribe(string streamId,
            Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null)
        {
            if (string.IsNullOrWhiteSpace(streamId))
            {
                SubscribeToAll(checkpoint,
                    settings,
                    eventAppeared,
                    liveProcessingStarted,
                    subscriptionDropped);
            }
            else
            {
                SubscribeToStream(streamId,
                    checkpoint,
                    settings,
                    eventAppeared,
                    liveProcessingStarted,
                    subscriptionDropped);
            }
        }

        void SubscribeToAll(Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            _eventStoreConnection.SubscribeToAllFrom(checkpoint().ToPosition(),
                settings,
                OnEventAppeared(eventAppeared),
                OnLiveProcessingStarted(liveProcessingStarted),
                OnSubscriptionDropped(subscriptionDropped));
        }

        void SubscribeToStream(string streamId,
            Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            _eventStoreConnection.SubscribeToStreamFrom(streamId,
                checkpoint().ToEventNumber(),
                settings,
                OnEventAppeared(eventAppeared),
                OnLiveProcessingStarted(liveProcessingStarted),
                OnSubscriptionDropped(subscriptionDropped));
        }

        Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            return subscriptionDropped;
        }

        Action<EventStoreCatchUpSubscription> OnLiveProcessingStarted(
            Action<EventStoreCatchUpSubscription> liveProcessingStarted)
        {
            return liveProcessingStarted;
        }

        Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> OnEventAppeared(
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared)
        {
            return eventAppeared;
        }
    }
}