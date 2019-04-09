namespace EventStore.Projections
{
    using System;
    using System.Threading.Tasks;
    using ClientAPI;

    class Subscriber
    {
        readonly IEventStoreConnection _eventStoreConnection;

        readonly LoggingAdaptor _loggingAdaptor;

        readonly RetryPolicy _retryPolicy;

        CurrentSubscription _currentSubscription;

        int _retryAttempts;

        internal Subscriber(IEventStoreConnection eventStoreConnection,
            LoggingAdaptor loggingAdaptor,
            RetryPolicy retryPolicy)
        {
            Guard.AgainstNullArgument(nameof(eventStoreConnection), eventStoreConnection);
            Guard.AgainstNullArgument(nameof(loggingAdaptor), loggingAdaptor);

            _eventStoreConnection = eventStoreConnection;
            _loggingAdaptor = loggingAdaptor;
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
            _loggingAdaptor.Information("Subscribing to all stream starting at checkpoint {0}",
                checkpoint().ToString());

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
            _loggingAdaptor.Information("Subscribing to {0} stream starting at checkpoint {1}",
                streamId.ToString(),
                checkpoint().ToString());

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
                        DropSubscription(subscriptionDropped, reason, exception);
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
                _loggingAdaptor.Warning(
                    "Subscription dropped, reason: {0} {1}, attempting to restart in {2}s {3}/{4}",
                    reason,
                    exception.Message,
                    _retryPolicy.CurrentWaitTime(_retryAttempts),
                    _retryAttempts + 1,
                    _retryPolicy.RetryLimit);

                _retryPolicy.Wait(_retryAttempts);

                Resubscribe();

                _retryAttempts++;
            }
            else
            {
                DropSubscription(subscriptionDropped, reason, exception);
            }
        }

        void DropSubscription(Action<SubscriptionDropReason, Exception> subscriptionDropped,
            SubscriptionDropReason reason,
            Exception exception)
        {
            _loggingAdaptor.Error("Subscription dropped, reason: {0} {1}",
                exception,
                reason,
                exception.Message);
            
            subscriptionDropped?.Invoke(reason, exception);
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