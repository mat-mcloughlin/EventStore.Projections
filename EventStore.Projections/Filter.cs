namespace EventStore.Projections
{
    using System;
    using System.Threading.Tasks;

    public abstract class Filter<TContext> : IFilter<TContext>
    {
        IFilter<TContext> _next;

        protected abstract Task Execute(TContext context, Func<TContext, Task> next);

        public void Register(IFilter<TContext> filter)
        {
            if (_next == null)
            {
                _next = filter;
            }
            else
            {
                _next.Register(filter);
            }
        }

        Task IFilter<TContext>.Execute(TContext context)
        {
            return Execute(context, ctx => _next == null
                ? Task.CompletedTask
                : _next.Execute(ctx));
        }
    }
}