namespace EventStore.Projections
{
    using System.Threading.Tasks;

    public interface IFilter<TContext>
    {
        void Register(IFilter<TContext> filter);

        Task Execute(TContext context);
    }
}