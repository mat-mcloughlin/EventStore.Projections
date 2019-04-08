namespace EventStore.Projections.Facts.FactCollections
{
    using Docker;
    using Xunit;

    [CollectionDefinition(nameof(EventStoreCollection), DisableParallelization = true)]
    public class EventStoreCollection : ICollectionFixture<EventStoreRunningInDocker>
    {
    }
}