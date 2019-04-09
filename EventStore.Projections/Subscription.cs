namespace EventStore.Projections
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;

    class Subscription
    {
        readonly IEventStoreConnection _eventStoreConnection;

        readonly RetryPolicy _retryPolicy;

        int _retryAttempts;

        CurrentSubscription _currentSubscription;

        internal Subscription(IEventStoreConnection eventStoreConnection, RetryPolicy retryPolicy)
        {
            Guard.AgainstNullArgument(nameof(eventStoreConnection), eventStoreConnection);

            _eventStoreConnection = eventStoreConnection;
            _retryPolicy = retryPolicy;
        }

        internal void Subscribe(StreamId streamId,
            Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<SubscriptionDropReason, Exception> subscriptionDropped = null)
        {
            _currentSubscription = new CurrentSubscription(streamId,
                checkpoint,
                settings,
                eventAppeared,
                liveProcessingStarted,
                subscriptionDropped);

            if (Equals(streamId, StreamId.All))
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
            Action<SubscriptionDropReason, Exception> subscriptionDropped)
        {
            _eventStoreConnection.SubscribeToAllFrom(checkpoint().ToPosition(),
                settings,
                OnEventAppeared(eventAppeared),
                OnLiveProcessingStarted(liveProcessingStarted),
                OnSubscriptionDropped(subscriptionDropped));
        }

        void SubscribeToStream(StreamId streamId,
            Func<Checkpoint> checkpoint,
            CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted,
            Action<SubscriptionDropReason, Exception> subscriptionDropped)
        {
            _eventStoreConnection.SubscribeToStreamFrom(streamId.Id,
                checkpoint().ToEventNumber(),
                settings,
                OnEventAppeared(eventAppeared),
                OnLiveProcessingStarted(liveProcessingStarted),
                OnSubscriptionDropped(subscriptionDropped));
        }

        Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(
            Action<SubscriptionDropReason, Exception> subscriptionDropped)
        {
            return (subscription, reason, exception) =>
            {
                subscription.Stop(); // Bug in event store where it keeps receiving events

                switch (reason)
                {
                    case SubscriptionDropReason.SubscribingError:
                    case SubscriptionDropReason.ServerError:
                    case SubscriptionDropReason.ConnectionClosed:
                    case SubscriptionDropReason.CatchUpError:
                    case SubscriptionDropReason.ProcessingQueueOverflow:
                    case SubscriptionDropReason.EventHandlerException:
                        TryToRestartSubscription(subscriptionDropped, reason, exception);
                        break;
                    default:
                        subscriptionDropped(reason, exception);
                        break;
                }
            };
        }

        Action<EventStoreCatchUpSubscription> OnLiveProcessingStarted(
            Action<EventStoreCatchUpSubscription> liveProcessingStarted)
        {
            return liveProcessingStarted;
        }

        Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> OnEventAppeared(
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared)
        {
            return async (s, e) =>
            {
                await eventAppeared(s, e);
                _retryAttempts = 0;
            };
            
        }

        void TryToRestartSubscription(Action<SubscriptionDropReason, Exception> subscriptionDropped,
            SubscriptionDropReason reason,
            Exception exception)
        {
            if (_retryAttempts < _retryPolicy.RetryLimit)
            {
                _retryPolicy.Wait(_retryAttempts);
                
                Resubscribe();
                
                _retryAttempts++;
            }
            else
            {
                subscriptionDropped?.Invoke(reason, exception);
            }
        }

        void Resubscribe()
        {
            Subscribe(_currentSubscription.StreamId,
                _currentSubscription.Checkpoint,
                _currentSubscription.Settings,
                _currentSubscription.EventAppeared,
                _currentSubscription.LiveProcessingStarted,
                _currentSubscription.SubscriptionDropped);
        }
    }
}