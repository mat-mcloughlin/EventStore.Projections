namespace EventStore.Projections
{
    using System.Threading.Tasks;

    public class InMemoryProjectionStateRepository : IProjectionStateRepository
    {
        public Task UpdateCheckpoint(string projectionName, Checkpoint checkpoint)
        {
            Checkpoint = checkpoint;
            return Task.CompletedTask;
        }

        public Checkpoint Checkpoint { get; private set; }
    }
}