namespace EventStore.Projections.Facts.FactCollections
{
    using Docker;
    using Xunit;

    [CollectionDefinition(nameof(EventStoreAllCollection), DisableParallelization = true)]
    public class EventStoreAllCollection : ICollectionFixture<EventStoreRunningInDocker>
    {
    }
}